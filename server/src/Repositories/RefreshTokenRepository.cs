using Dapper;
using server.src.Data.Interfaces;
using server.src.Models;
using server.src.Repositories.Interfaces;

namespace server.src.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RefreshToken?> GetByToken(string token, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r.id, r.token, r.expires_at, r.is_revoked, r.user_id, r.created_at,
                   u.id, u.username, u.email, u.password, u.created_at, u.updated_at
            FROM refresh_tokens r
            INNER JOIN users u ON u.id = r.user_id
            WHERE r.token = @Token
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<RefreshToken, User, RefreshToken>(
            new CommandDefinition(sql, new { Token = token }, cancellationToken: cancellationToken),
            (refreshToken, user) =>
            {
                refreshToken.User = user;
                return refreshToken;
            },
            splitOn: "id");

        return result.SingleOrDefault();
    }

    public async Task<RefreshToken> Create(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO refresh_tokens (token, expires_at, is_revoked, user_id)
            OUTPUT INSERTED.id
            VALUES (@Token, @ExpiresAt, @IsRevoked, @UserId)
            """;

        await using var connection = _connectionFactory.CreateConnection();
        refreshToken.Id = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken));
        return refreshToken;
    }

    public async Task Update(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE refresh_tokens
            SET is_revoked = @IsRevoked
            WHERE id = @Id
            """;

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, refreshToken, cancellationToken: cancellationToken));
    }
}
