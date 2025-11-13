using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

public static class JwtService
{
    public static string GenerateJwt(string userId)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? throw new Exception("JWT_SECRET not set");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "apstud-backend",
            audience: "apstud-frontend",
            claims: new[]
            {
                new Claim("uid", userId)
            },
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal ValidateJwt(string token)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET")
                     ?? throw new Exception("JWT_SECRET not set");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var tokenHandler = new JwtSecurityTokenHandler();

        return tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            IssuerSigningKey = key,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }
}
