package main

import (
	"log"
	"net/http"
	"os"
	"time"

	"equipment-idle-server/internal/save"
	"equipment-idle-server/internal/ws"
)

func main() {
	store := save.NewStore("saves")
	stopFlush := store.StartPeriodicFlush(30 * time.Second)
	defer stopFlush()

	hub := ws.NewHub(store)

	http.HandleFunc("/ws", hub.ServeWS)
	addr := os.Getenv("FZ2_ADDR")
	if addr == "" {
		addr = "127.0.0.1:8080"
	}
	log.Printf("equipment-idle-server listening on ws://%s/ws", addr)
	if err := http.ListenAndServe(addr, nil); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
