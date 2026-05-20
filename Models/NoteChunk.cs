using Pgvector;

namespace VecNotes.Models;

public class NoteChunk
{
    public int Id { get; set; }
    public int SequencePlace { get; set; }
    public string? NoteText { get; set; }
    public Vector? VectorizedText { get; set; }

    public int NoteId { get; set; }
    public Note Note { get; set; } = null!;
}
