using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VecNotes.Data;
using VecNotes.Models;
using VecNotes.Services;

namespace VecNotes.Endpoints;

public static class NoteEndpoints
{
    public static void MapNoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/notes");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapGet("/search", Search);
        group.MapGet("/{id:int}/audio", GetAudio);
        group.MapGet("/{id:int}/transcript", GetTranscript);
        group.MapPost("/{id:int}/transcribe", Transcribe);
        group.MapPost("/{id:int}/confirm", Confirm);
        group.MapPut("/{id:int}/transcript", UpdateTranscript);
        group.MapDelete("/{id:int}", Delete);
    }

    static async Task<IResult> GetAll(AppDbContext db)
    {
        var notes = await db.Notes
            .Include(n => n.NoteChunks.OrderBy(c => c.SequencePlace))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Results.Ok(notes.Select(ToResponse));
    }

    static async Task<IResult> GetAudio(int id, AppDbContext db)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null || note.AudioFilePath is null || !File.Exists(note.AudioFilePath))
            return Results.NotFound();

        var contentType = Path.GetExtension(note.AudioFilePath).ToLowerInvariant() switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".webm" => "audio/webm",
            _ => "application/octet-stream"
        };

        var absolutePath = Path.GetFullPath(note.AudioFilePath);
        return Results.Stream(File.OpenRead(absolutePath), contentType, enableRangeProcessing: true);
    }

    static async Task<IResult> GetById(int id, AppDbContext db)
    {
        var note = await db.Notes
            .Include(n => n.NoteChunks.OrderBy(c => c.SequencePlace))
            .FirstOrDefaultAsync(n => n.Id == id);

        return note is null ? Results.NotFound() : Results.Ok(ToResponse(note));
    }

    static async Task<IResult> Search(string q, AppDbContext db, IEmbeddingService embedder, int limit = 5)
    {
        var queryVector = await embedder.EmbedAsync(q);

        var results = await db.NoteChunks
            .Where(c => c.VectorizedText != null)
            .OrderBy(c => c.VectorizedText!.CosineDistance(queryVector))
            .Take(limit)
            .Select(c => new
            {
                c.Id,
                c.NoteId,
                c.SequencePlace,
                c.NoteText,
                Distance = c.VectorizedText!.CosineDistance(queryVector)
            })
            .ToListAsync();

        return Results.Ok(results.Select(r => new SearchResult(
            r.NoteId, r.Id, r.SequencePlace, r.NoteText, (float)(1 - r.Distance)
        )));
    }

    static async Task<IResult> Transcribe(int id, AppDbContext db, ITranscriptionService transcriber)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null) return Results.NotFound();
        if (note.AudioFilePath is null || !File.Exists(note.AudioFilePath))
            return Results.BadRequest("No audio file associated with this note.");

        note.RawTranscript = await transcriber.TranscribeAsync(note.AudioFilePath);
        note.Status = NoteStatus.Transcribed;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { note.Id, Status = note.Status.ToString(), note.RawTranscript });
    }

    static async Task<IResult> GetTranscript(int id, AppDbContext db)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null) return Results.NotFound();
        return Results.Ok(new { note.Id, Status = note.Status.ToString(), Transcript = note.RawTranscript });
    }

    static async Task<IResult> UpdateTranscript(int id, UpdateTranscriptRequest req, AppDbContext db)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null) return Results.NotFound();

        note.RawTranscript = req.Transcript;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new { note.Id, Status = note.Status.ToString(), note.RawTranscript });
    }

    static async Task<IResult> Confirm(int id, AppDbContext db, IEmbeddingService embedder)
    {
        var note = await db.Notes
            .Include(n => n.NoteChunks)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (note is null) return Results.NotFound();
        if (note.Status != NoteStatus.Transcribed)
            return Results.BadRequest($"Note must be in Transcribed state to confirm (current: {note.Status}).");
        if (string.IsNullOrWhiteSpace(note.RawTranscript))
            return Results.BadRequest("No transcript to confirm.");

        var chunks = TextChunker.Chunk(note.RawTranscript).ToList();
        var vectors = await Task.WhenAll(chunks.Select(c => embedder.EmbedAsync(c)));
        var noteChunks = chunks.Select((text, i) => new NoteChunk
        {
            SequencePlace = i + 1,
            NoteText = text,
            VectorizedText = vectors[i]
        }).ToList();

        note.NoteChunks = noteChunks;
        note.Status = NoteStatus.Confirmed;
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ToResponse(note));
    }

    static async Task<IResult> Delete(int id, AppDbContext db)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null) return Results.NotFound();

        db.Notes.Remove(note);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    static NoteResponse ToResponse(Note note) => new(
        note.Id,
        note.Status.ToString(),
        note.RawTranscript,
        note.CreatedAt,
        note.UpdatedAt,
        note.NoteChunks.Select(c => new ChunkResponse(c.Id, c.SequencePlace, c.NoteText)).ToList()
    );
}

record ChunkResponse(int Id, int SequencePlace, string? NoteText);
record NoteResponse(int Id, string Status, string? RawTranscript, DateTime CreatedAt, DateTime UpdatedAt, IEnumerable<ChunkResponse> Chunks);
record SearchResult(int NoteId, int ChunkId, int SequencePlace, string? NoteText, float Score);
record UpdateTranscriptRequest(string Transcript);
