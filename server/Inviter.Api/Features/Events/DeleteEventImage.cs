using Inviter.Api.Data;
using Inviter.Api.Infrastructure.Images;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Features.Events;

public static class DeleteEventImage
{
    public static async Task<IResult> Handle(
        string adminToken,
        AppDbContext db,
        EventImageStorage storage,
        CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(x => x.AdminToken == adminToken, ct);
        if (ev is null) return Results.NotFound();

        var fileName = ev.ImageFileName;
        if (!string.IsNullOrEmpty(fileName))
        {
            ev.ImageFileName = null;
            await db.SaveChangesAsync(ct);
            storage.Delete(fileName);
        }

        return Results.NoContent();
    }
}
