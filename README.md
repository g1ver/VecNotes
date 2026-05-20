# VecNotes

Voice-note app with semantic search. Record audio, transcribe it with whisper, and search your notes using vector embeddings stored in PostgreSQL.

## Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| PostgreSQL | 16+ | Database |
| pgvector | — | Vector extension for PostgreSQL |
| Ollama | — | Local embedding model |
| whisper-cli | — | Speech-to-text transcription |
| ffmpeg | — | Audio format conversion |
| .NET SDK | 10 | Backend |
| Node.js | 22+ | Frontend |

## Setup

### 1. PostgreSQL + pgvector

**macOS**
```bash
brew install postgresql@17 pgvector
brew services start postgresql@17
```

**Arch Linux**
```bash
sudo pacman -S postgresql
sudo systemctl enable --now postgresql
yay -S pgvector   # or: paru -S pgvector
```

### 2. Ollama

**macOS**
```bash
brew install ollama
brew services start ollama
```

**Arch Linux**
```bash
yay -S ollama
sudo systemctl enable --now ollama
```

Then pull the embedding model:
```bash
ollama pull nomic-embed-text
```

### 3. whisper-cli + model

**macOS**
```bash
brew install whisper-cpp ffmpeg
```

**Arch Linux**
```bash
yay -S whisper-cpp ffmpeg
```

Download the model (both platforms):
```bash
mkdir -p ~/whisper-models
curl -L -o ~/whisper-models/ggml-base.en-q8_0.bin \
  https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en-q8_0.bin
```

### 4. .NET SDK 10

**macOS**
```bash
brew install dotnet
```

**Arch Linux**
```bash
sudo pacman -S dotnet-sdk
```

### 5. Clone and configure

```bash
git clone <repo-url>
cd VecNotes
cp appsettings.example.json appsettings.json
```

Edit `appsettings.json` and set the following:

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string — update host, database, username, and password to match your setup |
| `Ollama:BaseUrl` | URL of your Ollama server (default: `http://localhost:11434`) |
| `Ollama:Model` | Embedding model name — must match what you pulled (e.g. `nomic-embed-text`) |
| `WhisperCpp:BinaryPath` | Absolute path to the `whisper-cli` binary |
| `WhisperCpp:ModelPath` | Absolute path to the downloaded `.bin` model file |
| `AudioStorage:Path` | Directory where uploaded audio files are stored (default: `./audio-uploads`) |

### 6. Apply database migrations

```bash
dotnet ef database update
```

### 7. Frontend

```bash
cd web
cp .env.example .env
npm install
```

`.env` defaults to `PUBLIC_API_URL=http://localhost:5000` which matches the backend.

## Running

**Backend** (http://localhost:5000)
```bash
dotnet run
```

**Frontend** (http://localhost:5173)
```bash
cd web
npm run dev
```

## Seeding sample data

```bash
python scripts/seed.py scripts/tts-audio/

# or a single file
python scripts/seed.py path/to/recording.wav --base-url http://localhost:5000
```

The seed script runs each audio file through the full upload → transcribe → confirm pipeline.
