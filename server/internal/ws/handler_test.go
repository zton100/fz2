package ws

import (
	"encoding/json"
	"strings"
	"sync"
	"testing"
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/protocol"
	"equipment-idle-server/internal/reincarnation"
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

func TestHandleLogin_NewAccount_GrantsStarterLoadout(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 16)}

	reqData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	env := protocol.Envelope{T: protocol.TypeLogin, ID: "r1", Data: reqData}
	raw, _ := json.Marshal(env)

	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		if len(p.Equipped) != len(data.AllSlots()) {
			t.Fatalf("equipped count = %d, want %d", len(p.Equipped), len(data.AllSlots()))
		}
		if p.Materials[data.MatBase] < data.ComposeCost {
			t.Fatalf("base materials = %d, want at least %d", p.Materials[data.MatBase], data.ComposeCost)
		}
		if power := reincarnation.ComputePlayerPower(p); power < data.MonsterAt(1).Power {
			t.Fatalf("starter power = %.1f, want at least floor 1 monster %.1f", power, data.MonsterAt(1).Power)
		}
	})

	var pushedBag protocol.BagData
	if !readEnvelope(sess.Send, protocol.TypeBag, &pushedBag) {
		t.Fatal("no bag message sent")
	}
	if len(pushedBag.Equipped) != len(data.AllSlots()) {
		t.Fatalf("pushed equipped count = %d, want %d", len(pushedBag.Equipped), len(data.AllSlots()))
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

func TestHandleReincarn_GrantsStarterLoadout(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 32)}

	loginData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	loginEnv := protocol.Envelope{T: protocol.TypeLogin, ID: "login", Data: loginData}
	loginRaw, _ := json.Marshal(loginEnv)
	hub.handleMessage(sess, loginRaw)

	store.WithPlayer("hero", func(p *model.Player) {
		p.Floor = reincarnation.ReincarnateFloorReq
	})

	reincarnEnv := protocol.Envelope{T: protocol.TypeReincarn, ID: "reincarn"}
	reincarnRaw, _ := json.Marshal(reincarnEnv)
	hub.handleMessage(sess, reincarnRaw)

	store.WithPlayer("hero", func(p *model.Player) {
		if p.Floor != 1 {
			t.Fatalf("floor = %d, want 1", p.Floor)
		}
		if len(p.Equipped) != len(data.AllSlots()) {
			t.Fatalf("equipped count = %d, want %d", len(p.Equipped), len(data.AllSlots()))
		}
		if power := reincarnation.ComputePlayerPower(p); power < data.MonsterAt(1).Power {
			t.Fatalf("starter power after reincarn = %.1f, want at least floor 1 monster %.1f", power, data.MonsterAt(1).Power)
		}
	})
}

func TestHandleLockEquipment_PersistsAndPushesBag(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 16)}
	store.LoadOrCreate("hero")
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:    "eq_lock_me",
			BaseID: "base_weapon",
			Name:   "Lock Me",
			Slot:   data.SlotWeapon,
		})
	})

	reqData, _ := json.Marshal(protocol.LockEquipmentRequest{UID: "eq_lock_me", Locked: true})
	env := protocol.Envelope{T: protocol.TypeLockEquipment, ID: "lock", Data: reqData}
	raw, _ := json.Marshal(env)
	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		if !p.Locked["eq_lock_me"] {
			t.Fatal("equipment was not locked in player state")
		}
	})
	var pushedBag protocol.BagData
	if !readEnvelope(sess.Send, protocol.TypeBag, &pushedBag) {
		t.Fatal("no bag message sent")
	}
	if len(pushedBag.Items) != 1 || !pushedBag.Items[0].Locked {
		t.Fatalf("pushed bag locked state = %+v, want locked item", pushedBag.Items)
	}
}

func TestHandleDecompose_LockedEquipmentRejected(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 16)}
	store.LoadOrCreate("hero")
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:    "eq_locked",
			BaseID: "base_weapon",
			Name:   "Locked Sword",
			Slot:   data.SlotWeapon,
		})
		p.Locked["eq_locked"] = true
	})

	reqData, _ := json.Marshal(protocol.DecomposeRequest{UID: "eq_locked"})
	env := protocol.Envelope{T: protocol.TypeDecompose, ID: "decompose", Data: reqData}
	raw, _ := json.Marshal(env)
	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		if len(p.EquipBag) != 1 {
			t.Fatalf("locked item was decomposed, bag len = %d", len(p.EquipBag))
		}
	})
	var result protocol.CraftResult
	if !readEnvelope(sess.Send, protocol.TypeCraftResult, &result) {
		t.Fatal("no craft result sent")
	}
	if result.OK {
		t.Fatal("locked decompose should fail")
	}
}

func TestHandleDecompose_UpgradedEquipmentReportsRefund(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 16)}
	store.LoadOrCreate("hero")
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:     "eq_upgraded",
			BaseID:  "base_weapon",
			Name:    "Upgraded Sword",
			Slot:    data.SlotWeapon,
			Rarity:  data.RarityRare,
			Upgrade: 3,
		})
	})

	reqData, _ := json.Marshal(protocol.DecomposeRequest{UID: "eq_upgraded"})
	env := protocol.Envelope{T: protocol.TypeDecompose, ID: "decompose", Data: reqData}
	raw, _ := json.Marshal(env)
	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		if len(p.EquipBag) != 0 {
			t.Fatalf("upgraded item was not decomposed, bag len = %d", len(p.EquipBag))
		}
		if p.Materials[data.MatBase] <= data.DecomposeBaseYield[data.RarityRare] {
			t.Fatalf("base materials = %d, want more than rarity yield after refund", p.Materials[data.MatBase])
		}
	})
	var result protocol.CraftResult
	if !readEnvelope(sess.Send, protocol.TypeCraftResult, &result) {
		t.Fatal("no craft result sent")
	}
	if !result.OK {
		t.Fatal("upgraded decompose should succeed")
	}
	if !strings.Contains(result.Msg, "返还强化材料") {
		t.Fatalf("craft result msg = %q, want refund hint", result.Msg)
	}
}

func TestHub_ConcurrentPlayerMutations_DoNotDeadlock(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Send: make(chan []byte, 256), stopCh: make(chan struct{})}
	defer close(sess.stopCh)

	loginData, _ := json.Marshal(protocol.LoginRequest{Account: "hero"})
	loginEnv := protocol.Envelope{T: protocol.TypeLogin, ID: "login", Data: loginData}
	loginRaw, _ := json.Marshal(loginEnv)
	hub.handleMessage(sess, loginRaw)

	var weaponUID string
	store.WithPlayer("hero", func(p *model.Player) {
		p.Materials[data.MatBase] = 1000
		weapon := &model.Equipment{
			UID:       "eq_concurrent_weapon",
			BaseID:    "base_weapon",
			Name:      "Concurrent Sword",
			Slot:      data.SlotWeapon,
			Rarity:    data.RarityRare,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 500},
		}
		p.EquipBag = append(p.EquipBag, weapon)
		weaponUID = weapon.UID
	})

	equipData, _ := json.Marshal(protocol.EquipRequest{UID: weaponUID})
	equipEnv := protocol.Envelope{T: protocol.TypeEquip, ID: "equip", Data: equipData}
	equipRaw, _ := json.Marshal(equipEnv)
	composeData, _ := json.Marshal(protocol.ComposeRequest{Slot: int(data.SlotWeapon)})
	composeEnv := protocol.Envelope{T: protocol.TypeCompose, ID: "compose", Data: composeData}
	composeRaw, _ := json.Marshal(composeEnv)

	done := make(chan struct{})
	go func() {
		defer close(done)
		var wg sync.WaitGroup
		for i := 0; i < 20; i++ {
			wg.Add(3)
			go func() {
				defer wg.Done()
				hub.handleMessage(sess, equipRaw)
			}()
			go func() {
				defer wg.Done()
				hub.handleMessage(sess, composeRaw)
			}()
			go func() {
				defer wg.Done()
				store.WithPlayer("hero", func(*model.Player) {
					sess.runner.Tick()
				})
				store.MarkDirty("hero")
			}()
		}
		wg.Wait()
	}()

	select {
	case <-done:
	case <-time.After(2 * time.Second):
		t.Fatal("concurrent mutations did not complete; possible lock inversion or deadlock")
	}
}

func readEnvelope(ch <-chan []byte, typ string, out any) bool {
	for {
		select {
		case raw := <-ch:
			var env protocol.Envelope
			if err := json.Unmarshal(raw, &env); err != nil {
				continue
			}
			if env.T != typ {
				continue
			}
			return json.Unmarshal(env.Data, out) == nil
		default:
			return false
		}
	}
}
