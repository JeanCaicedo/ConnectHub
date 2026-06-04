using ConnectHub.API.Data;
using ConnectHub.API.DTOs;
using ConnectHub.API.Helpers;
using ConnectHub.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly JwtHelper _jwt;

    public AuthController(ApplicationDbContext db, JwtHelper jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        // Verificar duplicados
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Ese email ya está registrado" });

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest(new { message = "Ese username ya está en uso" });

        // Crear usuario con hash de password
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(new AuthResponseDto
        {
            Token = _jwt.GenerateToken(user),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        });
    }
}
