using System.Security.Claims;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Domain.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
