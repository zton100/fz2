package ws

import (
	"encoding/json"
	"testing"

	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/save"
)

func TestHandleLogin_NewAccount_SendsSync(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 8)}

	reqData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	env := protocol.Envelope{T: protocol.TypeLogin, ID: "r1", Data: reqData}
	raw, _ := json.Marshal(env)

	hub.handleMessage(sess, raw)

	select {
	case resp := <-sess.Send:
		var got protocol.Envelope
		if err := json.Unmarshal(resp, &got); err != nil {
			t.Fatalf("unmarshal: %v", err)
		}
		if got.T != protocol.TypeSync {
			t.Fatalf("type = %q, want sync", got.T)
		}
		var sd protocol.SyncData
		json.Unmarshal(got.Data, &sd)
		if sd.Account != "hero" || sd.Floor != 1 {
			t.Fatalf("sync data = %+v, want account=hero floor=1", sd)
		}
	default:
		t.Fatal("no sync message sent")
	}
}

func TestHandleLogin_ExistingAccount_RetainsFloor(t *testing.T) {
	store := save.NewMemoryStore()
	p := store.LoadOrCreate("hero")
	p.Floor = 7

	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 8)}

	reqData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	env := protocol.Envelope{T: protocol.TypeLogin, ID: "r2", Data: reqData}
	raw, _ := json.Marshal(env)

	hub.handleMessage(sess, raw)

	resp := <-sess.Send
	var got protocol.Envelope
	json.Unmarshal(resp, &got)
	var sd protocol.SyncData
	json.Unmarshal(got.Data, &sd)
	if sd.Floor != 7 {
		t.Fatalf("floor = %d, want 7 (existing player retained)", sd.Floor)
	}
	if sess.Account != "hero" {
		t.Fatalf("session account = %q, want hero", sess.Account)
	}
}
