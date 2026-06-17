using System.ComponentModel.DataAnnotations;

namespace ConnectHub.API.DTOs;

public class RegisterDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreatePostDto
{
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}

public class PostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    // Datos de interacción
    public int LikesCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
