package main

import (
	"encoding/json"
	"fmt"
	"log"
	"os"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/protocol"

	"github.com/gorilla/websocket"
)

const (
	defaultServerURL = "ws://127.0.0.1:8080/ws"
	timeout          = 35 * time.Second
)

type verifyState struct {
	account           string
	floor             int
	power             float64
	baseMaterials     int
	lootCount         int
	initialEquipped   int
	postReincEquipped int
	transferPhase     string
	sourceUID         string
	targetUID         string
	sourceSlot        int
	transferDone      bool
	reincarnSent      bool
	reincarnSynced    bool
	bag               []protocol.EquipmentDTO
	equipped          []protocol.EquipmentDTO
	messageCounts     map[string]int
	minionProgress    map[int]int
	sawMinion         bool
	sawGuardian       bool
	sawBoss           bool
}

const (
	phaseWaitingFloor        = ""
	phaseUnequipSent         = "unequip_sent"
	phaseUpgradeSent         = "upgrade_sent"
	phaseEquipSourceSent     = "equip_source_sent"
	phaseEquipSourceWaitBag  = "equip_source_wait_bag"
	phaseTransferWaitResult  = "transfer_wait_result"
	phaseTransferConfirmed   = "transfer_confirmed"
	phaseDecomposeSourceSent = "decompose_source_sent"
	phaseDone                = "done"
)

func main() {
	serverURL := os.Getenv("FZ2_WS_URL")
	if serverURL == "" {
		serverURL = defaultServerURL
	}
	account := fmt.Sprintf("verifyhero_%d", time.Now().UnixNano())
	st := verifyState{
		account:        account,
		messageCounts:  map[string]int{},
		minionProgress: map[int]int{},
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
		if st.transferDone && st.reincarnSynced && st.postReincEquipped == len(data.AllSlots()) && st.sawMinion && st.sawGuardian && st.sawBoss {
			printSummary(st)
			fmt.Println("VERIFY_OK: login, starter loadout, battle loot, transfer upgrade, decompose old gear, floor 10, and reincarnation flow passed")
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
		st.bag = bd.Items
		st.equipped = bd.Equipped
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
	case protocol.TypeCombat:
		var cd protocol.CombatData
		if err := json.Unmarshal(env.Data, &cd); err != nil {
			return err
		}
		if cd.MinionsKilled < 0 || cd.MinionsKilled > cd.MinionsTotal {
			return fmt.Errorf("VERIFY_FAIL: invalid minion progress %+v", cd)
		}
		if cd.PlayerStartHP <= 0 || cd.EnemyStartHP <= 0 || len(cd.Events) == 0 {
			return fmt.Errorf("VERIFY_FAIL: combat missing authoritative hp or events %+v", cd)
		}
		if cd.EnemyFamily == "" || cd.EnemyElement == "" || len(cd.EnemyResistances) == 0 {
			return fmt.Errorf("VERIFY_FAIL: combat missing monster family or resistances %+v", cd)
		}
		firstEvent := cd.Events[0]
		if firstEvent.Actor == "" || firstEvent.Kind == "" || firstEvent.Damage <= 0 {
			return fmt.Errorf("VERIFY_FAIL: invalid combat event %+v", firstEvent)
		}
		switch cd.EnemyKind {
		case string(dungeon.EncounterMinion):
			st.sawMinion = true
			if cd.MinionsKilled > st.minionProgress[cd.Floor] {
				st.minionProgress[cd.Floor] = cd.MinionsKilled
			}
		case string(dungeon.EncounterGuardian), string(dungeon.EncounterBoss):
			if st.minionProgress[cd.Floor] < cd.MinionsTotal {
				return fmt.Errorf("VERIFY_FAIL: guardian arrived before minions cleared: %+v", cd)
			}
			if cd.EnemyKind == string(dungeon.EncounterBoss) {
				st.sawBoss = true
			} else {
				st.sawGuardian = true
			}
		}
	case protocol.TypeLoot:
		st.lootCount++
	case protocol.TypeFloor:
		var fd protocol.FloorData
		if err := json.Unmarshal(env.Data, &fd); err != nil {
			return err
		}
		st.floor = fd.Floor
	case protocol.TypeMaterials:
		var md protocol.MaterialsData
		if err := json.Unmarshal(env.Data, &md); err != nil {
			return err
		}
		for _, mat := range md.Materials {
			if mat.Key == string(data.MatBase) {
				st.baseMaterials = mat.Val
				break
			}
		}
	case protocol.TypeCraftResult:
		var cr protocol.CraftResult
		if err := json.Unmarshal(env.Data, &cr); err != nil {
			return err
		}
		if err := handleCraftResult(st, cr); err != nil {
			return err
		}
	}
	if err := advanceTransferFlow(conn, st); err != nil {
		return err
	}
	if st.transferDone && st.floor >= 10 && !st.reincarnSent {
		st.reincarnSent = true
		return writeEnvelope(conn, protocol.Envelope{T: protocol.TypeReincarn, ID: "reincarn"})
	}
	return nil
}

func handleCraftResult(st *verifyState, cr protocol.CraftResult) error {
	switch st.transferPhase {
	case phaseUpgradeSent:
		if cr.UID != st.sourceUID {
			return nil
		}
		if !cr.OK || cr.Upgrade <= 0 {
			return fmt.Errorf("VERIFY_FAIL: source upgrade failed during transfer flow: %+v", cr)
		}
		st.transferPhase = phaseEquipSourceSent
	case phaseTransferWaitResult:
		if cr.UID != st.targetUID {
			return nil
		}
		if !cr.OK || cr.Upgrade <= 0 {
			return fmt.Errorf("VERIFY_FAIL: transfer upgrade failed: %+v", cr)
		}
		st.transferPhase = phaseTransferConfirmed
	case phaseDecomposeSourceSent:
		if !cr.OK {
			return fmt.Errorf("VERIFY_FAIL: decompose old source failed: %+v", cr)
		}
		st.transferDone = true
		st.transferPhase = phaseDone
	}
	return nil
}

func advanceTransferFlow(conn *websocket.Conn, st *verifyState) error {
	switch st.transferPhase {
	case phaseWaitingFloor:
		if st.floor < 10 || st.baseMaterials < data.UpgradeCostTable[1] || len(st.equipped) == 0 {
			return nil
		}
		source, target := findTransferPair(st.equipped, st.bag)
		if source == nil || target == nil {
			return nil
		}
		st.sourceUID = source.UID
		st.sourceSlot = source.Slot
		st.targetUID = target.UID
		st.transferPhase = phaseUnequipSent
		return writeEnvelope(conn, protocol.Envelope{
			T:    protocol.TypeUnequip,
			ID:   "transfer_unequip",
			Data: mustMarshal(protocol.UnequipRequest{Slot: source.Slot}),
		})
	case phaseUnequipSent:
		if findEquipment(st.bag, st.sourceUID) == nil {
			return nil
		}
		st.transferPhase = phaseUpgradeSent
		return writeEnvelope(conn, protocol.Envelope{
			T:    protocol.TypeUpgrade,
			ID:   "transfer_upgrade_source",
			Data: mustMarshal(protocol.UpgradeRequest{UID: st.sourceUID}),
		})
	case phaseEquipSourceSent:
		if findEquipment(st.bag, st.sourceUID) == nil {
			return nil
		}
		st.transferPhase = phaseEquipSourceWaitBag
		return writeEnvelope(conn, protocol.Envelope{
			T:    protocol.TypeEquip,
			ID:   "transfer_equip_source",
			Data: mustMarshal(protocol.EquipRequest{UID: st.sourceUID}),
		})
	case phaseEquipSourceWaitBag:
		source := findEquipment(st.equipped, st.sourceUID)
		if source == nil || source.Upgrade <= 0 || findEquipment(st.bag, st.targetUID) == nil {
			return nil
		}
		return sendTransferRequest(conn, st)
	case phaseTransferConfirmed:
		target := findEquipment(st.equipped, st.targetUID)
		source := findEquipment(st.bag, st.sourceUID)
		if target == nil || source == nil || target.Upgrade <= source.Upgrade {
			return nil
		}
		st.transferPhase = phaseDecomposeSourceSent
		return writeEnvelope(conn, protocol.Envelope{
			T:    protocol.TypeDecompose,
			ID:   "transfer_decompose_old",
			Data: mustMarshal(protocol.DecomposeRequest{UID: st.sourceUID}),
		})
	}
	return nil
}

func sendTransferRequest(conn *websocket.Conn, st *verifyState) error {
	st.transferPhase = phaseTransferWaitResult
	return writeEnvelope(conn, protocol.Envelope{
		T:  protocol.TypeTransferUpg,
		ID: "transfer_apply",
		Data: mustMarshal(protocol.TransferUpgradeRequest{
			SourceUID: st.sourceUID,
			TargetUID: st.targetUID,
		}),
	})
}

func findTransferPair(equipped []protocol.EquipmentDTO, bag []protocol.EquipmentDTO) (*protocol.EquipmentDTO, *protocol.EquipmentDTO) {
	for i := range bag {
		target := &bag[i]
		for j := range equipped {
			source := &equipped[j]
			if source.Slot == target.Slot && source.UID != target.UID {
				return source, target
			}
		}
	}
	return nil, nil
}

func findEquipment(items []protocol.EquipmentDTO, uid string) *protocol.EquipmentDTO {
	for i := range items {
		if items[i].UID == uid {
			return &items[i]
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
	fmt.Printf("[summary] account=%s floor=%d power=%.1f loot=%d initial_equipped=%d transfer_done=%v transfer_phase=%s source=%s target=%s post_reinc_equipped=%d reincarn_sent=%v reincarn_synced=%v minion=%v guardian=%v boss=%v\n",
		st.account, st.floor, st.power, st.lootCount, st.initialEquipped, st.transferDone, st.transferPhase, st.sourceUID, st.targetUID, st.postReincEquipped, st.reincarnSent, st.reincarnSynced, st.sawMinion, st.sawGuardian, st.sawBoss)
	fmt.Println("--- message type counts ---")
	for t, c := range st.messageCounts {
		fmt.Printf("%s: %d\n", t, c)
	}
}
