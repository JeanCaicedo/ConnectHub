using System.Security.Claims;
using ConnectHub.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ConnectHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CommentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // DELETE /api/comments/5 -> solo el dueño del comentario
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        // Include(Replies): necesitamos las respuestas cargadas para poder borrarlas,
        // porque la auto-referencia está configurada como Restrict (no cascada).
        var comment = await _db.Comments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment is null) return NotFound(new { message = "Comentario no encontrado" });
        if (comment.UserId != userId) return Forbid();

        // Si es un comentario raíz con respuestas, se va el hilo entero:
        // borramos las respuestas antes que al padre para no violar la FK.
        if (comment.Replies.Count > 0)
            _db.Comments.RemoveRange(comment.Replies);

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private int GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.Parse(sub!);
    }
}
