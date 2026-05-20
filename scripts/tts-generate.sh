#!/bin/bash
# Generate audio files from tts-scripts/ using macOS say.
# Output goes to tts-audio/ as .m4a files.
#
# Usage: ./scripts/tts-generate.sh
# Change VOICE to any of: Eddy, Flo, Reed, Rocko, Sandy, Shelley (all (English (US)) neural voices)

set -euo pipefail

VOICE="Reed (English (US))"
SCRIPTS_DIR="$(dirname "$0")/tts-scripts"
OUTPUT_DIR="$(dirname "$0")/tts-audio"

mkdir -p "$OUTPUT_DIR"

for txt in "$SCRIPTS_DIR"/*.txt; do
  base=$(basename "$txt" .txt)
  aiff="$OUTPUT_DIR/$base.aiff"
  wav="$OUTPUT_DIR/$base.wav"
  echo "→ $base"
  say -v "$VOICE" -f "$txt" -o "$aiff"
  afconvert -f WAVE -d LEI16 "$aiff" "$wav"
  rm "$aiff"
done

echo "Done. Files written to $OUTPUT_DIR/"
