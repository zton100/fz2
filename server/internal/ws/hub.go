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
	CheckOrigin: func(r *http.Request) bool { return true }, // 本地开发，放开跨域
}

// Session 一个已连接的客户端会话。
type Session struct {
	Conn    *websocket.Conn
	Account string // 登录后填充
	Send    chan []byte
	runner  *dungeon.Runner // 登录后创建的战斗推进器
	stopCh  chan struct{}   // 战斗循环停止信号
}

// Hub 管理所有连接与会话。
type Hub struct {
	mu       sync.Mutex
	sessions map[*Session]struct{}
	store    *save.MemoryStore
	gen      *loot.Generator
	rng      *rand.Rand
}

// NewHub 创建 Hub。
func NewHub(store *save.MemoryStore) *Hub {
	return &Hub{
		sessions: make(map[*Session]struct{}),
		store:    store,
		gen:      loot.NewGenerator(rand.New(rand.NewSource(time.Now().UnixNano()))),
		rng:      rand.New(rand.NewSource(time.Now().UnixNano() + 1)),
	}
}

// ServeWS 处理 WebSocket 升级请求。
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
	h.mu.Unlock()
	close(sess.Send)
	if sess.stopCh != nil {
		select {
		case <-sess.stopCh: // 已关闭
		default:
			close(sess.stopCh)
		}
	}
}

// readPump 读取客户端消息并路由到 handler。
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

// writePump 把 Send 通道的消息写给客户端。
func (h *Hub) writePump(sess *Session) {
	for msg := range sess.Send {
		if err := sess.Conn.WriteMessage(websocket.TextMessage, msg); err != nil {
			break
		}
	}
}
