package ws

import (
	"encoding/json"
	"log"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/inventory"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/upgrade"
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
	case protocol.TypeEquip:
		h.handleEquip(sess, env)
	case protocol.TypeUnequip:
		h.handleUnequip(sess, env)
	case protocol.TypeDecompose:
		h.handleDecompose(sess, env)
	case protocol.TypeCompose:
		h.handleCompose(sess, env)
	case protocol.TypeReforge:
		h.handleReforge(sess, env)
	case protocol.TypeUpgrade:
		h.handleUpgrade(sess, env)
	default:
		log.Printf("unknown message type: %s", env.T)
	}
}

// handleLogin 处理登录：加载/创建玩家，回发全量同步与背包战力，启动战斗循环。
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

	// 推送当前背包与战力
	h.pushBag(sess, player)
	h.pushPower(sess, player)
	h.pushMaterials(sess, player)

	// 启动战斗循环
	drop := loot.NewDropTable(h.gen)
	runner := dungeon.NewRunner(player, combat.ComputePower, drop)
	runner.LootCallback = func(eq *model.Equipment) {
		h.pushLoot(sess, eq)
		h.pushBag(sess, player) // 掉落后背包变化，同步推送
	}
	runner.FloorCallback = func(newFloor int) {
		h.pushFloor(sess, newFloor)
	}
	sess.runner = runner
	go h.battleLoop(sess, runner)
}

// handleEquip 处理穿戴请求。
func (h *Hub) handleEquip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.EquipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("equip parse error: %v", err)
		return
	}
	if err := inventory.Equip(player, req.UID); err != nil {
		log.Printf("equip error: %v", err)
		return
	}
	h.pushBag(sess, player)
	h.pushPower(sess, player)
}

// handleUnequip 处理卸下请求。
func (h *Hub) handleUnequip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.UnequipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("unequip parse error: %v", err)
		return
	}
	if err := inventory.Unequip(player, data.Slot(req.Slot)); err != nil {
		log.Printf("unequip error: %v", err)
		return
	}
	h.pushBag(sess, player)
	h.pushPower(sess, player)
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
	ld := protocol.LootData{
		UID:     eq.UID,
		BaseID:  eq.BaseID,
		Name:    eq.Name,
		Slot:    int(eq.Slot),
		Rarity:  int(eq.Rarity),
		Upgrade: eq.Upgrade,
		Affixes: toAffixDTOs(eq.Affixes),
	}
	dataBytes, _ := json.Marshal(ld)
	env := protocol.Envelope{T: protocol.TypeLoot, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushFloor 把层数推进推送给客户端。
func (h *Hub) pushFloor(sess *Session, newFloor int) {
	fd := protocol.FloorData{Floor: newFloor}
	dataBytes, _ := json.Marshal(fd)
	env := protocol.Envelope{T: protocol.TypeFloor, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushBag 推送背包全量。
func (h *Hub) pushBag(sess *Session, player *model.Player) {
	items := make([]protocol.EquipmentDTO, len(player.EquipBag))
	for i, eq := range player.EquipBag {
		items[i] = toEquipmentDTO(eq)
	}
	bd := protocol.BagData{Items: items}
	dataBytes, _ := json.Marshal(bd)
	env := protocol.Envelope{T: protocol.TypeBag, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushPower 推送当前战力。
func (h *Hub) pushPower(sess *Session, player *model.Player) {
	stats := combat.AggregateStats(player.EquippedList())
	power := combat.ComputePower(stats)
	pd := protocol.PowerData{Power: power}
	dataBytes, _ := json.Marshal(pd)
	env := protocol.Envelope{T: protocol.TypePower, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// toEquipmentDTO 把装备实例转为传输对象。
func toEquipmentDTO(eq *model.Equipment) protocol.EquipmentDTO {
	return protocol.EquipmentDTO{
		UID:     eq.UID,
		BaseID:  eq.BaseID,
		Name:    eq.Name,
		Slot:    int(eq.Slot),
		Rarity:  int(eq.Rarity),
		Upgrade: eq.Upgrade,
		Affixes: toAffixDTOs(eq.Affixes),
	}
}

// toAffixDTOs 把词缀实例列表转为传输对象列表。
func toAffixDTOs(affixes []model.AffixInstance) []protocol.AffixDTO {
	out := make([]protocol.AffixDTO, len(affixes))
	for i, a := range affixes {
		out[i] = protocol.AffixDTO{
			Type:  string(a.Type),
			Tier:  a.Tier,
			Value: a.Value,
		}
	}
	return out
}

// findBagIndex 在背包按 UID 找索引。
func findBagIndex(p *model.Player, uid string) int {
	for i, eq := range p.EquipBag {
		if eq.UID == uid {
			return i
		}
	}
	return -1
}

// handleDecompose 分解请求：从背包找装备→分解→推送材料+背包。
func (h *Hub) handleDecompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.DecomposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	player.EquipBag = append(player.EquipBag[:idx], player.EquipBag[idx+1:]...)
	crafting.Decompose(player, eq)
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "decomposed", "", 0)
}

// handleCompose 合成请求。
func (h *Hub) handleCompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.ComposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	eq, err := crafting.Compose(player, h.gen, data.Slot(req.Slot))
	if err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "composed", eq.UID, 0)
}

// handleReforge 重铸请求：从背包找装备→重铸。
func (h *Hub) handleReforge(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.ReforgeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	if err := crafting.Reforge(player, h.gen, eq); err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushCraftResult(sess, true, "reforged", eq.UID, eq.Upgrade)
}

// handleUpgrade 强化请求：从背包找装备→强化。
func (h *Hub) handleUpgrade(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.UpgradeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	idx := findBagIndex(player, req.UID)
	if idx < 0 {
		h.pushCraftResult(sess, false, "equipment not in bag", "", 0)
		return
	}
	eq := player.EquipBag[idx]
	result, err := upgrade.Upgrade(player, h.rng, eq)
	if err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushMaterials(sess, player)
	h.pushBag(sess, player)
	h.pushPower(sess, player)
	msg := "upgraded"
	if !result.Success {
		msg = "upgrade failed (no degrade)"
	}
	h.pushCraftResult(sess, result.Success, msg, eq.UID, eq.Upgrade)
}

// pushMaterials 推送材料库存。
func (h *Hub) pushMaterials(sess *Session, player *model.Player) {
	mats := map[string]int{}
	for mt, n := range player.Materials {
		mats[string(mt)] = n
	}
	md := protocol.MaterialsData{Materials: mats}
	dataBytes, _ := json.Marshal(md)
	env := protocol.Envelope{T: protocol.TypeMaterials, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushCraftResult 推送养成操作结果。
func (h *Hub) pushCraftResult(sess *Session, ok bool, msg, uid string, upg int) {
	cr := protocol.CraftResult{OK: ok, Msg: msg, UID: uid, Upgrade: upg}
	dataBytes, _ := json.Marshal(cr)
	env := protocol.Envelope{T: protocol.TypeCraftResult, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}
