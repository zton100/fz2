package ws

import (
	"encoding/json"
	"log"

	"equipment-idle-server/internal/protocol"
)

// handleMessage 解析信封并路由到对应处理函数。
func (h *Hub) handleMessage(sess *Session, raw []byte) {
	var env protocol.Envelope
	if err := json.Unmarshal(raw, &env); err != nil {
		log.Printf("malformed message: %v", err)
		return
	}
	switch env.T {
	case protocol.TypeLogin:
		h.handleLogin(sess, env)
	default:
		log.Printf("unknown message type: %s", env.T)
	}
}

// handleLogin 处理登录：加载/创建玩家，回发全量同步。
func (h *Hub) handleLogin(sess *Session, env protocol.Envelope) {
	var req protocol.LoginRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("login parse error: %v", err)
		return
	}
	player := h.store.LoadOrCreate(req.Account)
	sess.Account = req.Account

	sync := protocol.SyncData{
		Account:   player.Account,
		Floor:     player.Floor,
		Souls:     player.Souls,
		Inventory: player.Inventory,
	}
	syncData, _ := json.Marshal(sync)
	resp := protocol.Envelope{T: protocol.TypeSync, ID: env.ID, Data: syncData}
	out, _ := json.Marshal(resp)
	sess.Send <- out
}
