#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SERVER_LOG="${TMPDIR:-/tmp}/fz2-verify-server.log"
FZ2_ADDR="${FZ2_ADDR:-127.0.0.1:8080}"
FZ2_WS_URL="${FZ2_WS_URL:-ws://${FZ2_ADDR}/ws}"

cleanup() {
  if [[ -n "${SERVER_PID:-}" ]]; then
    kill "$SERVER_PID" >/dev/null 2>&1 || true
    wait "$SERVER_PID" >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

cd "$ROOT/server"
FZ2_ADDR="$FZ2_ADDR" go run ./cmd/server >"$SERVER_LOG" 2>&1 &
SERVER_PID=$!

for _ in {1..40}; do
  if nc -z 127.0.0.1 "${FZ2_ADDR##*:}" >/dev/null 2>&1; then
    break
  fi
  sleep 0.25
done

if ! nc -z 127.0.0.1 "${FZ2_ADDR##*:}" >/dev/null 2>&1; then
  echo "Server did not start. Log:"
  tail -80 "$SERVER_LOG" || true
  exit 1
fi

FZ2_WS_URL="$FZ2_WS_URL" go run ./cmd/verifyclient
