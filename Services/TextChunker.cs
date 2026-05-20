using System.Text.RegularExpressions;

namespace VecNotes.Services;

public static partial class TextChunker
{
    private const int SentencesPerChunk = 3;

    public static IEnumerable<string> Chunk(string text)
    {
        var sentences = SentenceBoundary()
            .Split(text)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        for (int i = 0; i < sentences.Count; i += SentencesPerChunk)
            yield return string.Join(" ", sentences.Skip(i).Take(SentencesPerChunk));
    }

    // splits after .!? keeping punctuation attached to the preceding sentence
    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceBoundary();
}
