package main

import (
	"log"
	"net/http"

	"equipment-idle-server/internal/save"
	"equipment-idle-server/internal/ws"
)

func main() {
	store := save.NewStore("saves")
	hub := ws.NewHub(store)

	http.HandleFunc("/ws", hub.ServeWS)
	addr := ":8080"
	log.Printf("equipment-idle-server listening on ws://localhost%s/ws", addr)
	if err := http.ListenAndServe(addr, nil); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
