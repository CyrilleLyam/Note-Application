using System.Data.Common;
using Dapper;
using server.src.Data.Interfaces;
using server.src.Dtos;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class NoteRepository : INoteRepository
{
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = "n.title",
        ["created_at"] = "n.created_at",
        ["updated_at"] = "COALESCE(n.updated_at, n.created_at)",
        ["deleted_at"] = "n.deleted_at"
    };

    private readonly IDbConnectionFactory _connectionFactory;

    public NoteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IEnumerable<Note> Items, int TotalCount)> GetAll(int userId, NoteQueryDto queryDto, CancellationToken cancellationToken)
    {
        var conditions = new List<string>
        {
            "n.user_id = @UserId",
            queryDto.Trashed ? "n.deleted_at IS NOT NULL" : "n.deleted_at IS NULL"
        };
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId);

        if (!string.IsNullOrWhiteSpace(queryDto.Search))
        {
            conditions.Add(@"(n.title LIKE @Search ESCAPE '\' OR n.content LIKE @Search ESCAPE '\')");
            parameters.Add("Search", $"%{EscapeLike(queryDto.Search.Trim())}%");
        }

        if (!string.IsNullOrWhiteSpace(queryDto.Tag))
        {
            conditions.Add("""
                EXISTS (
                    SELECT 1 FROM note_tags nt
                    INNER JOIN tags t ON t.id = nt.tag_id
                    WHERE nt.note_id = n.id AND t.name = @Tag
                )
                """);
            parameters.Add("Tag", queryDto.Tag.Trim());
        }

        if (queryDto.CreatedFrom.HasValue)
        {
            conditions.Add("n.created_at >= @CreatedFrom");
            parameters.Add("CreatedFrom", queryDto.CreatedFrom.Value);
        }

        if (queryDto.CreatedTo.HasValue)
        {
            conditions.Add("n.created_at <= @CreatedTo");
            parameters.Add("CreatedTo", queryDto.CreatedTo.Value);
        }

        var sortColumn = SortColumns.GetValueOrDefault(queryDto.SortBy ?? string.Empty, "n.created_at");
        var sortDirection = string.Equals(queryDto.SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var pinnedFirst = queryDto.Trashed ? string.Empty : "n.is_pinned DESC, ";
        var where = string.Join(" AND ", conditions);

        parameters.Add("Offset", (queryDto.Page - 1) * queryDto.PageSize);
        parameters.Add("PageSize", queryDto.PageSize);

        var sql = $"""
            SELECT COUNT(*) FROM notes n WHERE {where};

            SELECT n.id, n.user_id, n.title, n.content, n.is_pinned,
                   n.created_at, n.updated_at, n.deleted_at, n.row_version
            FROM notes n
            WHERE {where}
            ORDER BY {pinnedFirst}{sortColumn} {sortDirection}, n.id {sortDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        int totalCount;
        List<Note> items;
        await using (var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)))
        {
            totalCount = await multi.ReadSingleAsync<int>();
            items = (await multi.ReadAsync<Note>()).ToList();
        }

        await AttachTags(connection, null, items, cancellationToken);

        return (items, totalCount);
    }

    public async Task<Note?> GetById(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id, title, content, is_pinned,
                   created_at, updated_at, deleted_at, row_version
            FROM notes
            WHERE id = @Id AND user_id = @UserId
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var note = await connection.QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        if (note != null)
        {
            await AttachTags(connection, null, [note], cancellationToken);
        }

        return note;
    }

    public async Task<bool> ExistsActive(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM notes WHERE id = @Id AND user_id = @UserId AND deleted_at IS NULL
            ) THEN 1 ELSE 0 END AS BIT)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<Note> Create(Note note, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO notes (user_id, title, content)
            OUTPUT INSERTED.id, INSERTED.user_id, INSERTED.title, INSERTED.content, INSERTED.is_pinned,
                   INSERTED.created_at, INSERTED.updated_at, INSERTED.deleted_at, INSERTED.row_version
            VALUES (@UserId, @Title, @Content)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var created = await connection.QuerySingleAsync<Note>(
            new CommandDefinition(sql, note, transaction, cancellationToken: cancellationToken));
        await ReplaceTags(connection, transaction, note.UserId, created.Id, note.Tags, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        created.Tags = note.Tags.Order(StringComparer.OrdinalIgnoreCase).ToList();
        return created;
    }

    public async Task<Note?> Update(Note note, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET title = @Title, content = @Content, updated_at = SYSUTCDATETIME()
            OUTPUT INSERTED.id, INSERTED.user_id, INSERTED.title, INSERTED.content, INSERTED.is_pinned,
                   INSERTED.created_at, INSERTED.updated_at, INSERTED.deleted_at, INSERTED.row_version
            WHERE id = @Id AND user_id = @UserId AND deleted_at IS NULL AND row_version = @RowVersion
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var updated = await connection.QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(sql, note, transaction, cancellationToken: cancellationToken));

        if (updated == null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await ReplaceTags(connection, transaction, note.UserId, updated.Id, note.Tags, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        updated.Tags = note.Tags.Order(StringComparer.OrdinalIgnoreCase).ToList();
        return updated;
    }

    public async Task<Note?> SetPinned(int id, int userId, bool isPinned, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET is_pinned = @IsPinned
            OUTPUT INSERTED.id, INSERTED.user_id, INSERTED.title, INSERTED.content, INSERTED.is_pinned,
                   INSERTED.created_at, INSERTED.updated_at, INSERTED.deleted_at, INSERTED.row_version
            WHERE id = @Id AND user_id = @UserId AND deleted_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var note = await connection.QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(sql, new { Id = id, UserId = userId, IsPinned = isPinned }, cancellationToken: cancellationToken));

        if (note != null)
        {
            await AttachTags(connection, null, [note], cancellationToken);
        }

        return note;
    }

    public async Task<bool> MoveToTrash(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET deleted_at = SYSUTCDATETIME()
            WHERE id = @Id AND user_id = @UserId AND deleted_at IS NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<Note?> Restore(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET deleted_at = NULL
            OUTPUT INSERTED.id, INSERTED.user_id, INSERTED.title, INSERTED.content, INSERTED.is_pinned,
                   INSERTED.created_at, INSERTED.updated_at, INSERTED.deleted_at, INSERTED.row_version
            WHERE id = @Id AND user_id = @UserId AND deleted_at IS NOT NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var note = await connection.QuerySingleOrDefaultAsync<Note>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        if (note != null)
        {
            await AttachTags(connection, null, [note], cancellationToken);
        }

        return note;
    }

    public async Task<bool> DeletePermanently(int id, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM notes
            WHERE id = @Id AND user_id = @UserId AND deleted_at IS NOT NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, transaction, cancellationToken: cancellationToken));
        await RemoveOrphanTags(connection, transaction, userId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<int> EmptyTrash(int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM notes
            WHERE user_id = @UserId AND deleted_at IS NOT NULL
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, transaction, cancellationToken: cancellationToken));
        await RemoveOrphanTags(connection, transaction, userId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return affected;
    }

    private static async Task AttachTags(DbConnection connection, DbTransaction? transaction, IReadOnlyCollection<Note> notes, CancellationToken cancellationToken)
    {
        if (notes.Count == 0)
        {
            return;
        }

        const string sql = """
            SELECT nt.note_id, t.name
            FROM note_tags nt
            INNER JOIN tags t ON t.id = nt.tag_id
            WHERE nt.note_id IN @NoteIds
            """;

        var rows = await connection.QueryAsync<NoteTagRow>(
            new CommandDefinition(sql, new { NoteIds = notes.Select(note => note.Id).ToArray() }, transaction, cancellationToken: cancellationToken));
        var tagsByNote = rows.ToLookup(row => row.NoteId, row => row.Name);

        foreach (var note in notes)
        {
            note.Tags = tagsByNote[note.Id].Order(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    private static async Task ReplaceTags(DbConnection connection, DbTransaction transaction, int userId, int noteId, IReadOnlyCollection<string> tags, CancellationToken cancellationToken)
    {
        const string unlinkSql = """
            DELETE FROM note_tags
            WHERE note_id = @NoteId
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(unlinkSql, new { NoteId = noteId }, transaction, cancellationToken: cancellationToken));

        if (tags.Count > 0)
        {
            const string createTagSql = """
                INSERT INTO tags (user_id, name)
                SELECT @UserId, @Name
                WHERE NOT EXISTS (
                    SELECT 1 FROM tags WITH (UPDLOCK, HOLDLOCK)
                    WHERE user_id = @UserId AND name = @Name
                )
                """;

            const string linkSql = """
                INSERT INTO note_tags (note_id, tag_id)
                SELECT @NoteId, id
                FROM tags
                WHERE user_id = @UserId AND name IN @Names
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(createTagSql, tags.Select(name => new { UserId = userId, Name = name }), transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(
                new CommandDefinition(linkSql, new { NoteId = noteId, UserId = userId, Names = tags }, transaction, cancellationToken: cancellationToken));
        }

        await RemoveOrphanTags(connection, transaction, userId, cancellationToken);
    }

    private static async Task RemoveOrphanTags(DbConnection connection, DbTransaction transaction, int userId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM tags
            WHERE user_id = @UserId
              AND NOT EXISTS (SELECT 1 FROM note_tags nt WHERE nt.tag_id = tags.id)
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, transaction, cancellationToken: cancellationToken));
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");
    }

    private sealed class NoteTagRow
    {
        public int NoteId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
