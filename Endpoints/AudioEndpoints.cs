using VecNotes.Data;
using VecNotes.Models;

namespace VecNotes.Endpoints;

public static class AudioEndpoints
{
    public static void MapAudioEndpoints(this WebApplication app)
    {
        app.MapPost("/audio", Upload).DisableAntiforgery();
    }

    static async Task<IResult> Upload(IFormFile audio, AppDbContext db, IConfiguration config)
    {
        var storagePath = config["AudioStorage:Path"] ?? "./audio-uploads";
        Directory.CreateDirectory(storagePath);

        var ext = Path.GetExtension(audio.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(storagePath, fileName);

        await using (var stream = File.Create(filePath))
            await audio.CopyToAsync(stream);

        var now = DateTime.UtcNow;
        var note = new Note
        {
            AudioFilePath = filePath,
            Status = NoteStatus.Uploaded,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync();

        return Results.Created($"/notes/{note.Id}", new
        {
            note.Id,
            note.AudioFilePath,
            Status = note.Status.ToString(),
            note.CreatedAt
        });
    }
}
