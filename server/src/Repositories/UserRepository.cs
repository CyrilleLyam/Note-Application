using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetById(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, username, email, password, created_at, updated_at
            FROM users
            WHERE id = @Id
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmail(string email, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, username, email, password, created_at, updated_at
            FROM users
            WHERE email = @Email
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByUsername(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, username, email, password, created_at, updated_at
            FROM users
            WHERE username = @Username
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Username = username }, cancellationToken: cancellationToken));
    }

    public async Task<User> Create(User user, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO users (username, email, password)
            OUTPUT INSERTED.id, INSERTED.username, INSERTED.email,
                   INSERTED.password, INSERTED.created_at, INSERTED.updated_at
            VALUES (@Username, @Email, @Password)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<User>(
            new CommandDefinition(sql, user, cancellationToken: cancellationToken));
    }
}
