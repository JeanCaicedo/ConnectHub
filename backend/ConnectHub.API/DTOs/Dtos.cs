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
    public int CommentsCount { get; set; }
}

public class CreateCommentDto
{
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    // Si viene null -> comentario raíz.
    // Si viene con valor -> es una respuesta a ese comentario.
    public int? ParentCommentId { get; set; }
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Autor
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    public int? ParentCommentId { get; set; }

    // Respuestas anidadas (solo se llenan en comentarios raíz).
    public List<CommentDto> Replies { get; set; } = new();
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public int PostsCount { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    // ¿El usuario autenticado ya sigue a este perfil?
    public bool IsFollowedByCurrentUser { get; set; }
}

public class UpdateProfileDto
{
    [MaxLength(500)]
    public string? Bio { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;   // "Like" | "Comment" | "Follow"
    public int FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public string? FromUserAvatarUrl { get; set; }
    public int? PostId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Punto (fecha, valor) para gráficos de línea/barras.
public class DailyCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class TopPostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
}

public class EngagementDto
{
    public int PostsCount { get; set; }
    public int LikesReceived { get; set; }
    public int CommentsReceived { get; set; }
    public int FollowersCount { get; set; }
    // Interacciones promedio por post: (likes + comentarios) / posts.
    public double EngagementRate { get; set; }
}

public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
}

public class SearchResultDto
{
    public List<PostDto> Posts { get; set; } = new();
    public List<UserSummaryDto> Users { get; set; } = new();
}

// Envoltorio genérico de paginación (offset-based) para listas.
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public bool HasMore { get; set; }
}
