#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/server"

export FZ2_ADDR="${FZ2_ADDR:-127.0.0.1:8080}"
echo "Starting fz2 server at ws://${FZ2_ADDR}/ws"
exec go run ./cmd/server
