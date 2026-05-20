namespace VecNotes.Models;

public class Note
{
    public int Id { get; set; }
    public string? AudioFilePath { get; set; }
    public NoteStatus Status { get; set; }
    public string? RawTranscript { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<NoteChunk> NoteChunks { get; set; } = [];
}
