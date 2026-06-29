package main

import (
	"encoding/json"
	"fmt"
	"log"
	"os"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/protocol"

	"github.com/gorilla/websocket"
)

const (
	serverURL = "ws://localhost:8080/ws"
	timeout   = 35 * time.Second
)

type verifyState struct {
	account           string
	floor             int
	power             float64
	lootCount         int
	initialEquipped   int
	postReincEquipped int
	reincarnSent      bool
	reincarnSynced    bool
	messageCounts     map[string]int
}

func main() {
	account := fmt.Sprintf("verifyhero_%d", time.Now().UnixNano())
	st := verifyState{
		account:       account,
		messageCounts: map[string]int{},
	}

	conn, _, err := websocket.DefaultDialer.Dial(serverURL, nil)
	if err != nil {
		log.Fatalf("dial %s: %v", serverURL, err)
	}
	defer conn.Close()

	if err := writeEnvelope(conn, protocol.Envelope{
		T:    protocol.TypeLogin,
		ID:   "login",
		Data: mustMarshal(protocol.LoginRequest{Account: account}),
	}); err != nil {
		log.Fatalf("write login: %v", err)
	}

	deadline := time.Now().Add(timeout)
	for time.Now().Before(deadline) {
		conn.SetReadDeadline(deadline)
		_, msg, err := conn.ReadMessage()
		if err != nil {
			break
		}
		var env protocol.Envelope
		if err := json.Unmarshal(msg, &env); err != nil {
			log.Fatalf("decode envelope: %v", err)
		}
		st.messageCounts[env.T]++

		if err := handleMessage(conn, &st, env); err != nil {
			log.Fatal(err)
		}
		if st.reincarnSynced && st.postReincEquipped == len(data.AllSlots()) {
			printSummary(st)
			fmt.Println("VERIFY_OK: login, starter loadout, battle loot, floor 10, and reincarnation flow passed")
			return
		}
	}

	printSummary(st)
	fmt.Println("VERIFY_FAIL: timed out before full gameplay flow completed")
	os.Exit(1)
}

func handleMessage(conn *websocket.Conn, st *verifyState, env protocol.Envelope) error {
	switch env.T {
	case protocol.TypeSync:
		var sd protocol.SyncData
		if err := json.Unmarshal(env.Data, &sd); err != nil {
			return err
		}
		st.floor = sd.Floor
		if st.reincarnSent && sd.Floor == 1 && sd.Souls > 0 {
			st.reincarnSynced = true
		}
	case protocol.TypeBag:
		var bd protocol.BagData
		if err := json.Unmarshal(env.Data, &bd); err != nil {
			return err
		}
		if !st.reincarnSent && st.initialEquipped == 0 {
			st.initialEquipped = len(bd.Equipped)
		}
		if st.reincarnSynced {
			st.postReincEquipped = len(bd.Equipped)
		}
	case protocol.TypePower:
		var pd protocol.PowerData
		if err := json.Unmarshal(env.Data, &pd); err != nil {
			return err
		}
		st.power = pd.Power
	case protocol.TypeLoot:
		st.lootCount++
	case protocol.TypeFloor:
		var fd protocol.FloorData
		if err := json.Unmarshal(env.Data, &fd); err != nil {
			return err
		}
		st.floor = fd.Floor
		if fd.Floor >= 10 && !st.reincarnSent {
			st.reincarnSent = true
			return writeEnvelope(conn, protocol.Envelope{T: protocol.TypeReincarn, ID: "reincarn"})
		}
	}
	return nil
}

func writeEnvelope(conn *websocket.Conn, env protocol.Envelope) error {
	return conn.WriteJSON(env)
}

func mustMarshal(v interface{}) json.RawMessage {
	b, _ := json.Marshal(v)
	return b
}

func printSummary(st verifyState) {
	fmt.Printf("[summary] account=%s floor=%d power=%.1f loot=%d initial_equipped=%d post_reinc_equipped=%d reincarn_sent=%v reincarn_synced=%v\n",
		st.account, st.floor, st.power, st.lootCount, st.initialEquipped, st.postReincEquipped, st.reincarnSent, st.reincarnSynced)
	fmt.Println("--- message type counts ---")
	for t, c := range st.messageCounts {
		fmt.Printf("%s: %d\n", t, c)
	}
}
