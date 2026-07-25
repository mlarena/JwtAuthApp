using System.Security.Claims;
using JwtAuthApp.Models;

namespace JwtAuthApp.Services
{
    public interface IAuthService
    {
        string GenerateJwtToken(User user);
        (string hash, string salt) HashPassword(string password);
        bool VerifyPassword(string password, string hash, string salt);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
