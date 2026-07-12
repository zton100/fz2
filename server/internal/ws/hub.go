package ws

import (
	"log"
	"math/rand"
	"net/http"
	"sync"
	"time"

	"github.com/gorilla/websocket"

	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/save"
)

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

type Session struct {
	Conn    *websocket.Conn
	Account string
	Send    chan []byte
	runner  *dungeon.Runner
	stopCh  chan struct{}
	rng     *rand.Rand
	gen     *loot.Generator
	drop    *loot.DropTable
}

type Hub struct {
	mu              sync.Mutex
	sessions        map[*Session]struct{}
	accountSessions map[string]*Session
	store           *save.Store
	battleInterval  time.Duration
}

func NewHub(store *save.Store) *Hub {
	return NewHubWithBattleInterval(store, 2*time.Second)
}

func NewHubWithBattleInterval(store *save.Store, battleInterval time.Duration) *Hub {
	if battleInterval <= 0 {
		battleInterval = 2 * time.Second
	}
	return &Hub{
		sessions:        make(map[*Session]struct{}),
		accountSessions: make(map[string]*Session),
		store:           store,
		battleInterval:  battleInterval,
	}
}

func (h *Hub) ServeWS(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("upgrade error: %v", err)
		return
	}
	sess := &Session{Conn: conn, Send: make(chan []byte, 64), stopCh: make(chan struct{})}
	h.register(sess)
	go h.writePump(sess)
	h.readPump(sess)
}

func (h *Hub) register(sess *Session) {
	h.mu.Lock()
	h.sessions[sess] = struct{}{}
	h.mu.Unlock()
}

func (h *Hub) unregister(sess *Session) {
	h.mu.Lock()
	delete(h.sessions, sess)
	if sess.Account != "" && h.accountSessions[sess.Account] == sess {
		delete(h.accountSessions, sess.Account)
	}
	h.mu.Unlock()
	close(sess.Send)
	if sess.stopCh != nil {
		select {
		case <-sess.stopCh:
		default:
			close(sess.stopCh)
		}
	}
}

func (h *Hub) readPump(sess *Session) {
	defer func() {
		h.unregister(sess)
		sess.Conn.Close()
	}()
	for {
		_, msg, err := sess.Conn.ReadMessage()
		if err != nil {
			break
		}
		h.handleMessage(sess, msg)
	}
}

func (h *Hub) writePump(sess *Session) {
	for msg := range sess.Send {
		if err := sess.Conn.WriteMessage(websocket.TextMessage, msg); err != nil {
			break
		}
	}
}
