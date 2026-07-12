package main

import (
	"log"
	"net/http"
	"os"
	"time"

	"equipment-idle-server/internal/save"
	"equipment-idle-server/internal/ws"
)

const defaultBattleInterval = 2 * time.Second

func parseBattleInterval(raw string) time.Duration {
	interval, err := time.ParseDuration(raw)
	if err != nil || interval <= 0 {
		return defaultBattleInterval
	}
	return interval
}

func main() {
	store := save.NewStore("saves")
	stopFlush := store.StartPeriodicFlush(30 * time.Second)
	defer stopFlush()

	battleInterval := parseBattleInterval(os.Getenv("FZ2_BATTLE_INTERVAL"))
	hub := ws.NewHubWithBattleInterval(store, battleInterval)

	http.HandleFunc("/ws", hub.ServeWS)
	addr := os.Getenv("FZ2_ADDR")
	if addr == "" {
		addr = "127.0.0.1:8080"
	}
	log.Printf("equipment-idle-server listening on ws://%s/ws (battle interval %s)", addr, battleInterval)
	if err := http.ListenAndServe(addr, nil); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
