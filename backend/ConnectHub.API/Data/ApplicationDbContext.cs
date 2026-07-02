using ConnectHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Username y Email únicos
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Relación User -> Posts (1 a muchos)
        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Un usuario no puede likear el mismo post dos veces
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.PostId, l.UserId })
            .IsUnique();

        // Like -> Post: al borrar un post se borran sus likes (cascada)
        modelBuilder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Like -> User: Restrict para evitar "multiple cascade paths"
        // (el like ya se borra vía el post; no hace falta un segundo camino User -> Like)
        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> Post: al borrar un post se borran sus comentarios (cascada).
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment -> User: Restrict. El comentario ya se borra vía el post;
        // un segundo camino User -> Comment crearía "multiple cascade paths".
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> Comment (auto-referencia): Restrict.
        // Si fuese Cascade, borrar un post tendría dos rutas hacia una respuesta
        // (Post -> Comment directo, y Post -> Comment raíz -> Comment respuesta),
        // que es justo el "multiple cascade paths" que SQL Server prohíbe.
        // Con Restrict, borramos las respuestas a mano en el controller.
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Follow: clave compuesta (FollowerId, FollowedId). Sin esto, EF no sabría
        // cuál es la PK de una entidad sin propiedad "Id".
        modelBuilder.Entity<Follow>()
            .HasKey(f => new { f.FollowerId, f.FollowedId });

        // Follow -> Follower (el que sigue). Restrict: si fuese Cascade, borrar un User
        // tendría dos caminos hacia la misma fila Follow (vía Follower y vía Followed).
        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Follow -> Followed (el seguido). También Restrict, por el mismo motivo.
        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Followed)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowedId)
            .OnDelete(DeleteBehavior.Restrict);

        // Guardamos el enum como texto ("Like"/"Comment"/"Follow") en vez de 0/1/2:
        // más legible al inspeccionar la BD y robusto si se reordena el enum.
        modelBuilder.Entity<Notification>()
            .Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Notification -> User (destinatario) y -> FromUser (autor): ambos Restrict.
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.FromUser)
            .WithMany()
            .HasForeignKey(n => n.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification -> Post: al borrar el post, la notificación queda con PostId nulo
        // (no la borramos, pero pierde la referencia). PostId es nullable para permitirlo.
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Post)
            .WithMany()
            .HasForeignKey(n => n.PostId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
