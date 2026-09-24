#!/usr/bin/env bash
# Append user prompts to AI_PROMPTS.md (beforeSubmitPrompt hook).
# Parses stdin JSON with Python (jq is optional / may be missing).

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/AI_PROMPTS.md"

# Always allow the prompt to proceed
emit_continue() {
  printf '%s\n' '{"continue": true}'
}

INPUT="$(cat || true)"

if [[ -z "${INPUT//[[:space:]]/}" ]]; then
  emit_continue
  exit 0
fi

PROMPT="$(
  printf '%s' "$INPUT" | python -c '
import json, sys
try:
    data = json.load(sys.stdin)
except Exception:
    sys.exit(0)
prompt = data.get("prompt")
if prompt is None:
    sys.exit(0)
if not isinstance(prompt, str):
    prompt = str(prompt)
sys.stdout.write(prompt)
' 2>/dev/null || true
)"

if [[ -z "$PROMPT" ]]; then
  emit_continue
  exit 0
fi

# Skip short prompts
if [[ ${#PROMPT} -lt 15 ]]; then
  emit_continue
  exit 0
fi

# Skip prompts that look like they contain secrets
UPPER="$(printf '%s' "$PROMPT" | tr '[:lower:]' '[:upper:]')"
if [[ "$UPPER" == *"API_KEY"* ]] || [[ "$UPPER" == *"SECRET"* ]] || [[ "$UPPER" == *"TOKEN"* ]] || [[ "$UPPER" == *"PASSWORD"* ]]; then
  emit_continue
  exit 0
fi

TS="$(date +%H:%M)"

{
  printf '\n## %s – Cursor\n\n' "$TS"
  printf '%s\n' "$PROMPT"
} >> "$OUT"

emit_continue
exit 0
