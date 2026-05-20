using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using VecNotes.Data;
using VecNotes.Endpoints;
using VecNotes.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(http =>
    http.BaseAddress = new Uri(builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434"));

builder.Services.AddScoped<ITranscriptionService, WhisperCppTranscriptionService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();
app.MapNoteEndpoints();
app.MapAudioEndpoints();

app.Run();

