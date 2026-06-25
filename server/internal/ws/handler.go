package ws

import (
	"encoding/json"
	"log"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/protocol"
)

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

// handleLogin 处理登录：加载/创建玩家，回发全量同步，启动战斗循环。
func (h *Hub) handleLogin(sess *Session, env protocol.Envelope) {
	var req protocol.LoginRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("login parse error: %v", err)
		return
	}
	player := h.store.LoadOrCreate(req.Account)
	sess.Account = req.Account

	// 回发全量同步
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

	// 启动战斗循环
	drop := loot.NewDropTable(h.gen)
	runner := dungeon.NewRunner(player, combat.ComputePower, drop)
	runner.LootCallback = func(eq *model.Equipment) {
		h.pushLoot(sess, eq)
	}
	runner.FloorCallback = func(newFloor int) {
		h.pushFloor(sess, newFloor)
	}
	sess.runner = runner
	go h.battleLoop(sess, runner)
}

// battleLoop 定时战斗循环，每 2 秒一次 tick。
func (h *Hub) battleLoop(sess *Session, runner *dungeon.Runner) {
	ticker := time.NewTicker(2 * time.Second)
	defer ticker.Stop()
	for {
		select {
		case <-sess.stopCh:
			return
		case <-ticker.C:
			runner.Tick()
		}
	}
}

// pushLoot 把掉落装备推送给客户端。
func (h *Hub) pushLoot(sess *Session, eq *model.Equipment) {
	affixes := make([]protocol.AffixDTO, len(eq.Affixes))
	for i, a := range eq.Affixes {
		affixes[i] = protocol.AffixDTO{
			Type:  string(a.Type),
			Tier:  a.Tier,
			Value: a.Value,
		}
	}
	ld := protocol.LootData{
		UID:     eq.UID,
		BaseID:  eq.BaseID,
		Name:    eq.Name,
		Slot:    int(eq.Slot),
		Rarity:  int(eq.Rarity),
		Upgrade: eq.Upgrade,
		Affixes: affixes,
	}
	data, _ := json.Marshal(ld)
	env := protocol.Envelope{T: protocol.TypeLoot, Data: data}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default: // 发送缓冲满则丢弃，避免阻塞战斗循环
	}
}

// pushFloor 把层数推进推送给客户端。
func (h *Hub) pushFloor(sess *Session, newFloor int) {
	fd := protocol.FloorData{Floor: newFloor}
	data, _ := json.Marshal(fd)
	env := protocol.Envelope{T: protocol.TypeFloor, Data: data}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}
