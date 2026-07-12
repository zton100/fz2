#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_DIR="$ROOT/client/Assets/Art/UIReferences"
OUTPUT_DIR="$ROOT/client/Assets/Resources/UI/Equipment"
CHROMA_HELPER="${CODEX_HOME:-$HOME/.codex}/skills/.system/imagegen/scripts/remove_chroma_key.py"

command -v magick >/dev/null
test -f "$CHROMA_HELPER"
mkdir -p "$OUTPUT_DIR"

process_sheet() {
  local source="$1"
  shift
  local ids=("$@")
  local width height cell_width index x raw keyed

  width="$(magick identify -format '%w' "$source")"
  height="$(magick identify -format '%h' "$source")"
  cell_width=$((width / ${#ids[@]}))

  for index in "${!ids[@]}"; do
    x=$((index * cell_width))
    raw="$(mktemp -t fz2-icon-raw).png"
    keyed="$(mktemp -t fz2-icon-keyed).png"
    trap 'rm -f "$raw" "$keyed"' RETURN

    magick "$source" -crop "${cell_width}x${height}+${x}+0" +repage "$raw"
    python3 "$CHROMA_HELPER" \
      --input "$raw" \
      --out "$keyed" \
      --auto-key border \
      --soft-matte \
      --transparent-threshold 12 \
      --opaque-threshold 220 \
      --despill
    magick "$keyed" \
      -trim +repage \
      -resize '220x220>' \
      -gravity center \
      -background none \
      -extent 256x256 \
      "$OUTPUT_DIR/${ids[$index]}.png"

    rm -f "$raw" "$keyed"
    trap - RETURN
  done
}

process_sheet "$SOURCE_DIR/equipment-helmets-v1-source.png" \
  helmet_leather_hood helmet_iron_guard helmet_barbarian helmet_arcane_crown helmet_oath_mask
process_sheet "$SOURCE_DIR/equipment-armors-v1-source.png" \
  armor_chainmail armor_ranger_leather armor_arcane_robe armor_bulwark_plate armor_ember_garb
process_sheet "$SOURCE_DIR/equipment-gloves-v1-source.png" \
  gloves_leather gloves_gladiator gloves_eagle_eye gloves_runic gloves_gale
process_sheet "$SOURCE_DIR/equipment-boots-v1-source.png" \
  boots_expedition boots_iron_tread boots_shadowstep boots_guard_greaves boots_wildwalker
process_sheet "$SOURCE_DIR/equipment-rings-primary-v1-source.png" \
  ring1_iron ring1_flame ring1_frost ring1_eagle ring1_treasure
process_sheet "$SOURCE_DIR/equipment-rings-secondary-v1-source.png" \
  ring2_thunder ring2_might ring2_ruin ring2_focus ring2_precision
process_sheet "$SOURCE_DIR/equipment-necklaces-v1-source.png" \
  neck_plain_amulet neck_life_pendant neck_blood_charm neck_bulwark neck_abundance
process_sheet "$SOURCE_DIR/legendary-equipment-a-v1-source.png" \
  legendary_ember_cleaver legendary_frost_wake legendary_star_crown legendary_last_bulwark
process_sheet "$SOURCE_DIR/legendary-equipment-b-v1-source.png" \
  legendary_ash_mantle legendary_gale_grasp legendary_night_stride legendary_coinbound
process_sheet "$SOURCE_DIR/legendary-equipment-c-v1-source.png" \
  legendary_sun_brand legendary_tempest_eye legendary_blood_covenant legendary_harvest_seal

derive_artifact_icon() {
  local source_id="$1"
  local artifact_id="$2"
  magick "$OUTPUT_DIR/${source_id}.png" \
    -fill '#ef4444' -colorize 12 \
    \( -size 256x256 xc:none -fill none -stroke '#ef4444' -strokewidth 7 -draw 'roundrectangle 10,10 246,246 28,28' \) \
    -compose SrcOver -composite \
    \( -size 256x256 xc:none -fill none -stroke '#fbbf24' -strokewidth 2 -draw 'roundrectangle 20,20 236,236 22,22' \) \
    -compose SrcOver -composite \
    "$OUTPUT_DIR/${artifact_id}.png"
}

derive_artifact_icon weapon_hunter_sabre artifact_echo_blade
derive_artifact_icon armor_bulwark_plate artifact_aegis_heart
derive_artifact_icon ring2_ruin artifact_cull_signet
derive_artifact_icon neck_blood_charm artifact_blood_well

# Generated subjects occasionally cross a cell boundary; keep deterministic
# cleanup here so rebuilding the icon set does not restore neighboring debris.
magick "$OUTPUT_DIR/armor_arcane_robe.png" -channel A -fx 'i>229?0:u' \
  "$OUTPUT_DIR/armor_arcane_robe.png"
magick "$OUTPUT_DIR/legendary_gale_grasp.png" -channel A -fx 'i<51?0:u' \
  "$OUTPUT_DIR/legendary_gale_grasp.png"
magick "$OUTPUT_DIR/legendary_star_crown.png" -channel A -fx 'i>229?0:u' \
  "$OUTPUT_DIR/legendary_star_crown.png"
