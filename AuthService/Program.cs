using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5003");
var jwt = builder.Configuration.GetSection("Jwt");
var app = builder.Build();

app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    // Demo credentials for the class activity only. Replace with your lesson's identity API.
    if (request.Username != "admin" || request.Password != "Pass123!")
        return Results.Unauthorized();

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[] { new Claim(ClaimTypes.Name, request.Username), new Claim(ClaimTypes.Role, "Admin") };
    var token = new JwtSecurityToken(
        issuer: jwt["Issuer"],
        audience: jwt["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: credentials);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

app.Run();

record LoginRequest(string Username, string Password);
