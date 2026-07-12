#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASES="$ROOT/server/internal/data/equipment_bases.json"
LEGENDARIES="$ROOT/server/internal/data/legendary_equipment.json"
ICONS="$ROOT/client/Assets/Resources/UI/Equipment"
missing=()

while IFS= read -r icon_id; do
	icon="$ICONS/$icon_id.png"
	if [[ ! -s "$icon" ]]; then
		missing+=("$icon_id")
	fi
done < <(jq -r '.bases[].id' "$BASES"; jq -r '.legendaries[].id' "$LEGENDARIES")

if ((${#missing[@]} > 0)); then
  printf 'Missing equipment icons: %s\n' "${missing[*]}" >&2
  exit 1
fi

expected="$(( $(jq '.bases | length' "$BASES") + $(jq '.legendaries | length' "$LEGENDARIES") ))"
actual="$(find "$ICONS" -maxdepth 1 -type f -name '*.png' | wc -l | tr -d ' ')"
if [[ "$actual" != "$expected" ]]; then
  echo "Equipment icon count mismatch: got $actual, expected $expected" >&2
  exit 1
fi

echo "Equipment icon coverage OK: $actual/$expected (bases + legendaries)"
