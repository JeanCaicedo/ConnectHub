namespace ConnectHub.API.Models;

// Relacion N-N auto-referencial: un usuario sigue a otro usuario.
// No tiene Id propio; la clave primaria es la combinacion (FollowerId, FollowedId),
// lo que garantiza que no puedas seguir dos veces a la misma persona.
public class Follow
{
    // Quien sigue
    public int FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    // A quien sigue
    public int FollowedId { get; set; }
    public User Followed { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
