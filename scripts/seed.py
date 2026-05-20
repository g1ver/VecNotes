#!/usr/bin/env python3
"""
Seed vec-notes by running audio files through the full pipeline:
  POST /audio → POST /notes/{id}/transcribe → POST /notes/{id}/confirm

Usage:
  python seed.py ./audio-files/
  python seed.py recording.mp3
  python seed.py ./audio-files/ --base-url http://localhost:5111
"""

import argparse
import json
import mimetypes
import os
import sys
import time
import urllib.error
import urllib.request
import uuid
from datetime import datetime
from pathlib import Path

AUDIO_EXTENSIONS = {".mp3", ".wav", ".flac", ".ogg", ".m4a"}


def post_json(url: str) -> dict:
    req = urllib.request.Request(url, method="POST")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read())


def upload_audio(base_url: str, file_path: Path) -> dict:
    boundary = uuid.uuid4().hex
    mime_type = mimetypes.guess_type(str(file_path))[0] or "audio/mpeg"

    with open(file_path, "rb") as f:
        file_data = f.read()

    body = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="audio"; filename="{file_path.name}"\r\n'
        f"Content-Type: {mime_type}\r\n\r\n"
    ).encode() + file_data + f"\r\n--{boundary}--\r\n".encode()

    req = urllib.request.Request(
        f"{base_url}/audio",
        data=body,
        method="POST",
        headers={"Content-Type": f"multipart/form-data; boundary={boundary}"},
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read())


def log(msg: str) -> None:
    ts = datetime.now().strftime("%H:%M:%S")
    line = f"[{ts}] {msg}"
    print(line, flush=True)


def process_file(base_url: str, file_path: Path, index: int, total: int) -> bool:
    log(f"[{index}/{total}] {file_path.name}")

    try:
        t0 = time.time()
        upload = upload_audio(base_url, file_path)
        note_id = upload["id"]
        log(f"  [{index}/{total}] uploaded → note_id={note_id} ({time.time()-t0:.1f}s)")

        t1 = time.time()
        log(f"  [{index}/{total}] transcribing...")
        transcribe = post_json(f"{base_url}/notes/{note_id}/transcribe")
        transcript = transcribe.get("rawTranscript", "") or ""
        log(f"  [{index}/{total}] transcribed → {len(transcript)} chars ({time.time()-t1:.1f}s)")

        t2 = time.time()
        log(f"  [{index}/{total}] confirming (embedding chunks)...")
        confirm = post_json(f"{base_url}/notes/{note_id}/confirm")
        chunk_count = len(confirm.get("chunks", []))
        log(f"  [{index}/{total}] confirmed → {chunk_count} chunks ({time.time()-t2:.1f}s)")

        return True

    except urllib.error.HTTPError as e:
        body = e.read().decode(errors="replace")
        log(f"  [{index}/{total}] ERROR {e.code}: {body[:200]}")
        return False
    except Exception as e:
        log(f"  [{index}/{total}] ERROR: {e}")
        return False


def collect_files(target: str) -> list[Path]:
    p = Path(target)
    if p.is_file():
        return [p]
    return sorted(f for f in p.iterdir() if f.suffix.lower() in AUDIO_EXTENSIONS)


def main():
    parser = argparse.ArgumentParser(description="Seed vec-notes from audio files")
    parser.add_argument("target", help="Audio file or directory of audio files")
    parser.add_argument("--base-url", default="http://localhost:5111")
    args = parser.parse_args()

    files = collect_files(args.target)
    if not files:
        print("No audio files found.", file=sys.stderr)
        sys.exit(1)

    total = len(files)
    log(f"Starting seed: {total} file(s) → {args.base_url}")
    ok = 0
    for i, f in enumerate(files, 1):
        if process_file(args.base_url, f, i, total):
            ok += 1
    log(f"Done. {ok}/{total} succeeded.")


if __name__ == "__main__":
    main()
