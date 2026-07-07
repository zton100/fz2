package ws

import (
	"encoding/json"
	"fmt"
	"log"
	"math/rand"
	"time"

	"equipment-idle-server/internal/crafting"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/inventory"
	"equipment-idle-server/internal/locale"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/offline"
	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/reincarnation"
	"equipment-idle-server/internal/starter"
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
	case protocol.TypeTransferUpg:
		h.handleTransferUpgrade(sess, env)
	case protocol.TypeLockEquipment:
		h.handleLockEquipment(sess, env)
	case protocol.TypeReincarn:
		h.handleReincarn(sess, env)
	case protocol.TypeTalentUp:
		h.handleTalentUp(sess, env)
	default:
		log.Printf("unknown message type: %s", env.T)
	}
}

func (h *Hub) handleLogin(sess *Session, env protocol.Envelope) {
	var req protocol.LoginRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		log.Printf("login parse error: %v", err)
		return
	}
	sess.Account = req.Account

	h.mu.Lock()
	if old, ok := h.accountSessions[sess.Account]; ok && old != sess {
		log.Printf("kicking duplicate session for account %s", sess.Account)
		old.Conn.Close()
	}
	h.accountSessions[sess.Account] = sess
	h.mu.Unlock()

	sess.rng = rand.New(rand.NewSource(time.Now().UnixNano()))
	sess.gen = loot.NewGenerator(sess.rng)
	sess.drop = loot.NewDropTable(sess.gen)
	var player *model.Player
	if err := h.store.WithPlayerSave(req.Account, func(p *model.Player) {
		player = p
		if len(player.Equipped) == 0 && len(player.EquipBag) == 0 && len(player.Materials) == 0 {
			starter.GrantLoadout(player, sess.gen)
			log.Printf("bootstrap: %s %s", locale.Current().MsgBootstrap, sess.Account)
		}

		now := time.Now()
		if !player.LastOnline.IsZero() {
			offlineDuration := now.Sub(player.LastOnline)
			if offlineDuration > 0 {
				drop := loot.NewDropTable(sess.gen)
				result := offline.Calc(player, nil, drop, offlineDuration,
					reincarnation.OfflineGainBonus(player),
					reincarnation.DropBonus(player),
					reincarnation.QualityFloor(player))
				if result.TicksSimulated > 0 {
					h.pushOfflineResult(sess, result)
				}
			}
		}
		player.LastOnline = now

		sync := protocol.SyncData{
			Account: player.Account, Floor: player.Floor, Souls: player.Souls, Inventory: player.Inventory,
		}
		syncData, _ := json.Marshal(sync)
		resp := protocol.Envelope{T: protocol.TypeSync, ID: env.ID, Data: syncData}
		out, _ := json.Marshal(resp)
		sess.Send <- out

		h.pushBag(sess, player)
		h.pushPower(sess, player)
		h.pushMaterials(sess, player)
		h.pushTalents(sess, player)
	}); err != nil {
		log.Printf("login save error for %s: %v", req.Account, err)
	}

	runner := dungeon.NewRunner(player, nil, sess.drop)
	runner.LootCallback = func(eq *model.Equipment) {
		h.pushLoot(sess, eq)
		h.pushBag(sess, player)
	}
	runner.FloorCallback = func(newFloor int) {
		h.pushFloor(sess, newFloor)
		h.pushTalents(sess, player)
	}
	runner.BossRewardCallback = func(floor int, amount int) {
		h.pushMaterials(sess, player)
		h.pushCraftResult(sess, true, fmt.Sprintf("Boss 首通奖励：基础材料 +%d", amount), "", 0)
	}
	sess.runner = runner
	go h.battleLoop(sess, runner)
}

func (h *Hub) handleEquip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.EquipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		if err := inventory.Equip(player, req.UID); err != nil {
			return
		}
		h.pushBag(sess, player)
		h.pushPower(sess, player)
	}); err != nil {
		log.Printf("equip save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleUnequip(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.UnequipRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		if err := inventory.Unequip(player, data.Slot(req.Slot)); err != nil {
			return
		}
		h.pushBag(sess, player)
		h.pushPower(sess, player)
	}); err != nil {
		log.Printf("unequip save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) battleLoop(sess *Session, runner *dungeon.Runner) {
	ticker := time.NewTicker(2 * time.Second)
	defer ticker.Stop()
	for {
		select {
		case <-sess.stopCh:
			if sess.Account != "" {
				if err := h.store.WithPlayerSave(sess.Account, func(p *model.Player) {
					p.LastOnline = time.Now()
				}); err != nil {
					log.Printf("disconnect save error for %s: %v", sess.Account, err)
				}
			}
			return
		case <-ticker.C:
			h.store.WithPlayer(sess.Account, func(*model.Player) {
				runner.Tick()
			})
			h.store.MarkDirty(sess.Account)
		}
	}
}

func (h *Hub) pushLoot(sess *Session, eq *model.Equipment) {
	ld := protocol.LootData{
		UID: eq.UID, BaseID: eq.BaseID, Name: eq.Name,
		Slot: int(eq.Slot), Rarity: int(eq.Rarity), Upgrade: eq.Upgrade,
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

func (h *Hub) pushBag(sess *Session, player *model.Player) {
	items := make([]protocol.EquipmentDTO, len(player.EquipBag))
	for i, eq := range player.EquipBag {
		items[i] = toEquipmentDTO(eq, player.Locked)
	}
	equipped := make([]protocol.EquipmentDTO, 0, len(player.Equipped))
	for _, slot := range data.AllSlots() {
		if eq := player.Equipped[slot]; eq != nil {
			equipped = append(equipped, toEquipmentDTO(eq, player.Locked))
		}
	}
	bd := protocol.BagData{Items: items, Equipped: equipped}
	dataBytes, _ := json.Marshal(bd)
	env := protocol.Envelope{T: protocol.TypeBag, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

func (h *Hub) pushPower(sess *Session, player *model.Player) {
	power := reincarnation.ComputePlayerPower(player)
	pd := protocol.PowerData{Power: power}
	dataBytes, _ := json.Marshal(pd)
	env := protocol.Envelope{T: protocol.TypePower, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

func toEquipmentDTO(eq *model.Equipment, locked map[string]bool) protocol.EquipmentDTO {
	return protocol.EquipmentDTO{
		UID: eq.UID, BaseID: eq.BaseID, Name: eq.Name,
		Slot: int(eq.Slot), Rarity: int(eq.Rarity), Upgrade: eq.Upgrade,
		Locked:  locked != nil && locked[eq.UID],
		Affixes: toAffixDTOs(eq.Affixes),
	}
}

func toAffixDTOs(affixes []model.AffixInstance) []protocol.AffixDTO {
	out := make([]protocol.AffixDTO, len(affixes))
	for i, a := range affixes {
		out[i] = protocol.AffixDTO{Type: string(a.Type), Tier: a.Tier, Value: a.Value}
	}
	return out
}

func findBagIndex(p *model.Player, uid string) int {
	for i, eq := range p.EquipBag {
		if eq.UID == uid {
			return i
		}
	}
	return -1
}

func findEquipment(p *model.Player, uid string) (*model.Equipment, bool) {
	for _, eq := range p.EquipBag {
		if eq != nil && eq.UID == uid {
			return eq, false
		}
	}
	for _, eq := range p.Equipped {
		if eq != nil && eq.UID == uid {
			return eq, true
		}
	}
	return nil, false
}

func (h *Hub) handleDecompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.DecomposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		idx := findBagIndex(player, req.UID)
		if idx < 0 {
			h.pushCraftResult(sess, false, locale.Current().MsgNotInBag, "", 0)
			return
		}
		eq := player.EquipBag[idx]
		if player.Locked != nil && player.Locked[eq.UID] {
			h.pushCraftResult(sess, false, "装备已锁定，不能分解", eq.UID, eq.Upgrade)
			return
		}
		player.EquipBag = append(player.EquipBag[:idx], player.EquipBag[idx+1:]...)
		delete(player.Locked, eq.UID)
		yield, _ := crafting.Decompose(player, eq)
		h.pushMaterials(sess, player)
		h.pushBag(sess, player)
		msg := locale.Current().MsgDecomposed
		if refund := crafting.UpgradeRefund(eq); refund > 0 {
			msg = fmt.Sprintf("%s，返还强化材料 +%d", msg, refund)
		} else if baseYield := yield[data.MatBase]; baseYield > 0 {
			msg = fmt.Sprintf("%s，基础材料 +%d", msg, baseYield)
		}
		h.pushCraftResult(sess, true, msg, "", 0)
	}); err != nil {
		log.Printf("decompose save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleTransferUpgrade(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.TransferUpgradeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		player.EnsureRuntimeDefaults()
		targetIdx := findBagIndex(player, req.TargetUID)
		if targetIdx < 0 {
			h.pushCraftResult(sess, false, locale.Current().MsgNotInBag, req.TargetUID, 0)
			return
		}
		source, sourceEquipped := findEquipment(player, req.SourceUID)
		target := player.EquipBag[targetIdx]
		if source == nil {
			h.pushCraftResult(sess, false, "来源装备不存在", req.SourceUID, 0)
			return
		}
		if player.Locked[source.UID] || player.Locked[target.UID] {
			h.pushCraftResult(sess, false, "锁定装备不能继承强化", target.UID, target.Upgrade)
			return
		}
		beforeTargetUpgrade := target.Upgrade
		if err := upgrade.TransferUpgrade(source, target); err != nil {
			h.pushCraftResult(sess, false, err.Error(), target.UID, target.Upgrade)
			return
		}
		if sourceEquipped {
			if err := inventory.Equip(player, target.UID); err != nil {
				h.pushCraftResult(sess, false, err.Error(), target.UID, target.Upgrade)
				return
			}
		}
		h.pushBag(sess, player)
		h.pushPower(sess, player)
		msg := fmt.Sprintf("继承强化成功：%s +%d，旧装回到 +%d", target.Name, target.Upgrade, beforeTargetUpgrade)
		h.pushCraftResult(sess, true, msg, target.UID, target.Upgrade)
	}); err != nil {
		log.Printf("transfer upgrade save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleLockEquipment(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.LockEquipmentRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		player.EnsureRuntimeDefaults()
		if findBagIndex(player, req.UID) < 0 {
			h.pushCraftResult(sess, false, locale.Current().MsgNotInBag, req.UID, 0)
			return
		}
		if req.Locked {
			player.Locked[req.UID] = true
			h.pushCraftResult(sess, true, "装备已锁定", req.UID, 0)
		} else {
			delete(player.Locked, req.UID)
			h.pushCraftResult(sess, true, "装备已解锁", req.UID, 0)
		}
		h.pushBag(sess, player)
	}); err != nil {
		log.Printf("lock equipment save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleCompose(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.ComposeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		eq, err := crafting.Compose(player, sess.gen, data.Slot(req.Slot))
		if err != nil {
			h.pushCraftResult(sess, false, err.Error(), "", 0)
			return
		}
		h.pushMaterials(sess, player)
		h.pushBag(sess, player)
		h.pushCraftResult(sess, true, locale.Current().MsgComposed, eq.UID, 0)
	}); err != nil {
		log.Printf("compose save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleReforge(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.ReforgeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		idx := findBagIndex(player, req.UID)
		if idx < 0 {
			h.pushCraftResult(sess, false, locale.Current().MsgNotInBag, "", 0)
			return
		}
		eq := player.EquipBag[idx]
		if err := crafting.Reforge(player, sess.gen, eq); err != nil {
			h.pushCraftResult(sess, false, err.Error(), "", 0)
			return
		}
		h.pushMaterials(sess, player)
		h.pushBag(sess, player)
		h.pushCraftResult(sess, true, locale.Current().MsgReforged, eq.UID, eq.Upgrade)
	}); err != nil {
		log.Printf("reforge save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleUpgrade(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.UpgradeRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		idx := findBagIndex(player, req.UID)
		if idx < 0 {
			h.pushCraftResult(sess, false, locale.Current().MsgNotInBag, "", 0)
			return
		}
		eq := player.EquipBag[idx]
		result, err := upgrade.Upgrade(player, sess.rng, eq)
		if err != nil {
			h.pushCraftResult(sess, false, err.Error(), "", 0)
			return
		}
		h.pushMaterials(sess, player)
		h.pushBag(sess, player)
		h.pushPower(sess, player)
		msg := locale.Current().MsgUpgraded
		if !result.Success {
			msg = locale.Current().MsgUpgradeFailed
		}
		h.pushCraftResult(sess, result.Success, msg, eq.UID, eq.Upgrade)
	}); err != nil {
		log.Printf("upgrade save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) pushMaterials(sess *Session, player *model.Player) {
	var mats []protocol.MaterialKV
	for mt, n := range player.Materials {
		mats = append(mats, protocol.MaterialKV{Key: string(mt), Val: n})
	}
	if mats == nil {
		mats = []protocol.MaterialKV{}
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

func (h *Hub) pushOfflineResult(sess *Session, result offline.OfflineResult) {
	od := protocol.OfflineResultData{
		DurationSeconds: int(result.Duration.Seconds()),
		TicksSimulated:  result.TicksSimulated,
		LootCount:       result.LootCount,
		FloorsAdvanced:  result.FloorsAdvanced,
	}
	dataBytes, _ := json.Marshal(od)
	env := protocol.Envelope{T: protocol.TypeOfflineResult, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

func (h *Hub) handleReincarn(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		if err := reincarnation.Reincarnate(player); err != nil {
			h.pushCraftResult(sess, false, err.Error(), "", 0)
			return
		}
		starter.GrantLoadout(player, sess.gen)
		h.pushSync(sess, player, env.ID)
		h.pushBag(sess, player)
		h.pushPower(sess, player)
		h.pushMaterials(sess, player)
		h.pushTalents(sess, player)
		h.pushCraftResult(sess, true, locale.Current().MsgReincarnated, "", 0)
	}); err != nil {
		log.Printf("reincarn save error for %s: %v", sess.Account, err)
	}
}

func (h *Hub) handleTalentUp(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	var req protocol.TalentUpRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := h.store.WithPlayerSave(sess.Account, func(player *model.Player) {
		if err := reincarnation.UpgradeTalent(player, req.Name); err != nil {
			h.pushCraftResult(sess, false, err.Error(), "", 0)
			return
		}
		h.pushTalents(sess, player)
		h.pushCraftResult(sess, true, fmt.Sprintf(locale.Current().MsgTalentUpgraded, talentDisplayName(req.Name)), "", 0)
	}); err != nil {
		log.Printf("talent save error for %s: %v", sess.Account, err)
	}
}

func talentDisplayName(name string) string {
	switch name {
	case "damage":
		return "伤害"
	case "quality":
		return "品质"
	case "drop":
		return "掉落"
	case "offline_gain":
		return "离线"
	default:
		return name
	}
}

func (h *Hub) pushTalents(sess *Session, player *model.Player) {
	var talents []protocol.TalentKV
	for name, level := range player.Talents {
		talents = append(talents, protocol.TalentKV{Name: name, Level: level})
	}
	if talents == nil {
		talents = []protocol.TalentKV{}
	}
	td := protocol.TalentsData{
		Souls: player.Souls, MaxFloor: player.MaxFloor,
		CanReincarn: reincarnation.CanReincarnate(player),
		Talents:     talents,
	}
	dataBytes, _ := json.Marshal(td)
	env := protocol.Envelope{T: protocol.TypeTalents, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

func (h *Hub) pushSync(sess *Session, player *model.Player, id string) {
	sync := protocol.SyncData{
		Account: player.Account, Floor: player.Floor,
		Souls: player.Souls, Inventory: player.Inventory,
	}
	syncData, _ := json.Marshal(sync)
	resp := protocol.Envelope{T: protocol.TypeSync, ID: id, Data: syncData}
	out, _ := json.Marshal(resp)
	select {
	case sess.Send <- out:
	default:
	}
}
