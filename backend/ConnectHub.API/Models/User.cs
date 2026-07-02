using System.ComponentModel.DataAnnotations;

namespace ConnectHub.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(300)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navegación: un usuario tiene muchos posts
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    // Navegación: un usuario puede dar muchos likes
    public ICollection<Like> Likes { get; set; } = new List<Like>();

    // Navegación: un usuario puede escribir muchos comentarios
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
