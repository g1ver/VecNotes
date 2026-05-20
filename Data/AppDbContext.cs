using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VecNotes.Models;

namespace VecNotes.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteChunk> NoteChunks => Set<NoteChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<NoteChunk>()
            .Property(c => c.VectorizedText)
            .HasColumnType("vector(768)");
    }
}
