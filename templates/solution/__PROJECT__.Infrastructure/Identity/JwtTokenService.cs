using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using __PROJECT__.Application.Interfaces;
using __PROJECT__.Domain.Entities;

namespace __PROJECT__.Infrastructure.Identity;

public class JwtTokenService : IJWTService
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _key;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        var secretBytes = Encoding.UTF8.GetByteCount(secret);
        if (secretBytes < 64)
        {
            throw new InvalidOperationException(
                $"Jwt:Secret must be at least 64 bytes (512 bits) for HS512; configured value is {secretBytes} bytes. "
                + "Generate a new secret with at least 64 random bytes.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string GenerateAccessToken(um_user_app user, IEnumerable<string> roles, string sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.user_id),
            new(ClaimTypes.Email, user.email ?? string.Empty),
            new(ClaimTypes.Name, user.full_name ?? user.user_id),
            new("session_id", sessionId),
        };

        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetInt("Jwt:AccessTokenExpirationMinutes", 15)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha512Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(tokenDescriptor));
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    public string? GetUserIdFromToken(string token)
        => ValidateToken(token)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private int GetInt(string key, int defaultValue)
        => int.TryParse(_configuration[key], out var value) ? value : defaultValue;
}
