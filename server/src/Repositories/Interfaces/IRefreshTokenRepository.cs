using server.src.Models;

namespace server.src.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByToken(string token, CancellationToken cancellationToken);
    Task<RefreshToken> Create(RefreshToken refreshToken, CancellationToken cancellationToken);
    Task Update(RefreshToken refreshToken, CancellationToken cancellationToken);
}
