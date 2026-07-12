package ws

import (
	"encoding/json"
	"strings"
	"sync"
	"testing"
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
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
		if sd.EquipmentDataVersion != data.EquipmentDataVersion() {
			t.Fatalf("equipment data version = %d, want %d", sd.EquipmentDataVersion, data.EquipmentDataVersion())
		}
		if sd.LegendaryDataVersion != data.LegendaryDataVersion() {
			t.Fatalf("legendary data version = %d, want %d", sd.LegendaryDataVersion, data.LegendaryDataVersion())
		}
		if sd.ArtifactDataVersion != data.ArtifactDataVersion() {
			t.Fatalf("artifact data version = %d, want %d", sd.ArtifactDataVersion, data.ArtifactDataVersion())
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

func TestPushBagIncludesAuthoritativeProjectedPower(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 8)}
	store.WithPlayer("hero", func(p *model.Player) {
		p.Equipped[data.SlotWeapon] = &model.Equipment{
			UID: "current", Slot: data.SlotWeapon,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 10},
		}
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID: "upgrade", Slot: data.SlotWeapon,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 20},
		})
		hub.pushBag(sess, p)
	})

	var bag protocol.BagData
	if !readEnvelope(sess.Send, protocol.TypeBag, &bag) {
		t.Fatal("no bag message sent")
	}
	if len(bag.Items) != 1 || len(bag.Equipped) != 1 {
		t.Fatalf("bag = %+v, want one item and one equipped item", bag)
	}
	if bag.Items[0].PowerScore <= bag.Equipped[0].PowerScore {
		t.Fatalf("upgrade power score = %.1f, want above current %.1f", bag.Items[0].PowerScore, bag.Equipped[0].PowerScore)
	}
}

func TestPushBagIncludesLegendaryIdentityAndDescription(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 8)}
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:         "legendary",
			LegendaryID: "legendary_gale_grasp",
			Slot:        data.SlotGloves,
			Rarity:      data.RarityLegendary,
			BaseStats:   map[data.AffixType]float64{},
		})
		hub.pushBag(sess, p)
	})

	var bag protocol.BagData
	if !readEnvelope(sess.Send, protocol.TypeBag, &bag) || len(bag.Items) != 1 {
		t.Fatal("no legendary bag item sent")
	}
	item := bag.Items[0]
	if item.LegendaryID != "legendary_gale_grasp" || item.LegendaryDescription == "" {
		t.Fatalf("legendary DTO = %+v, want identity and description", item)
	}
}

func TestPushBagIncludesArtifactIdentityAndTrigger(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 8)}
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:        "artifact",
			ArtifactID: "artifact_echo_blade",
			Slot:       data.SlotWeapon,
			Rarity:     data.RarityArtifact,
			BaseStats:  map[data.AffixType]float64{},
		})
		hub.pushBag(sess, p)
	})

	var bag protocol.BagData
	if !readEnvelope(sess.Send, protocol.TypeBag, &bag) || len(bag.Items) != 1 {
		t.Fatal("no artifact bag item sent")
	}
	item := bag.Items[0]
	if item.ArtifactID != "artifact_echo_blade" || item.ArtifactDescription == "" || item.ArtifactTrigger == "" || item.ArtifactValue <= 0 {
		t.Fatalf("artifact DTO = %+v, want identity, description and trigger", item)
	}
}

func TestPushCombatIncludesHitTimeline(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 8)}

	hub.pushCombat(sess, dungeon.TickResult{
		Win:               true,
		EnemyKind:         dungeon.EncounterMinion,
		EnemyFamily:       data.MonsterFamilyBeast,
		EnemyElement:      "fire",
		Floor:             3,
		PlayerPower:       120,
		EnemyPower:        60,
		EnemyResistances:  map[data.AffixType]float64{data.ATFireDmg: 0.18},
		MinionsKilled:     1,
		MinionsTotal:      dungeon.MinionsPerFloor,
		PlayerStartHP:     220,
		EnemyStartHP:      148,
		PlayerStartShield: 30,
		PlayerEndHP:       210,
		EnemyEndHP:        0,
		Events: []combat.HitEvent{{
			Index:        1,
			Actor:        combat.ActorPlayer,
			Kind:         combat.EventHit,
			Damage:       42,
			PlayerHP:     220,
			EnemyHP:      106,
			PlayerShield: 30,
		}},
	})

	var cd protocol.CombatData
	if !readEnvelope(sess.Send, protocol.TypeCombat, &cd) {
		t.Fatal("no combat message sent")
	}
	if cd.PlayerStartHP != 220 || cd.PlayerStartShield != 30 || len(cd.Events) != 1 {
		t.Fatalf("combat data = %+v, want hp, shield and events", cd)
	}
	if cd.EnemyFamily != data.MonsterFamilyBeast || cd.EnemyElement != "fire" || len(cd.EnemyResistances) != 1 {
		t.Fatalf("combat monster data = %+v, want family, element and resistances", cd)
	}
	if cd.Events[0].Actor != combat.ActorPlayer || cd.Events[0].Damage != 42 {
		t.Fatalf("combat event = %+v, want player hit", cd.Events[0])
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

func TestHandleTransferUpgrade_InheritsFromEquippedAndEquipsTarget(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 16)}
	store.LoadOrCreate("hero")
	store.WithPlayer("hero", func(p *model.Player) {
		p.Equipped[data.SlotWeapon] = &model.Equipment{
			UID:       "old_weapon",
			BaseID:    "old_base",
			Name:      "Old Sword",
			Slot:      data.SlotWeapon,
			Upgrade:   6,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 20},
		}
		p.EquipBag = append(p.EquipBag, &model.Equipment{
			UID:       "new_weapon",
			BaseID:    "new_base",
			Name:      "New Sword",
			Slot:      data.SlotWeapon,
			Upgrade:   1,
			BaseStats: map[data.AffixType]float64{data.ATStrength: 30},
		})
	})

	reqData, _ := json.Marshal(protocol.TransferUpgradeRequest{SourceUID: "old_weapon", TargetUID: "new_weapon"})
	env := protocol.Envelope{T: protocol.TypeTransferUpg, ID: "transfer", Data: reqData}
	raw, _ := json.Marshal(env)
	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		equipped := p.Equipped[data.SlotWeapon]
		if equipped == nil || equipped.UID != "new_weapon" {
			t.Fatalf("equipped weapon = %+v, want new weapon", equipped)
		}
		if equipped.Upgrade != 6 {
			t.Fatalf("new weapon upgrade = %d, want inherited +6", equipped.Upgrade)
		}
		if len(p.EquipBag) != 1 || p.EquipBag[0].UID != "old_weapon" {
			t.Fatalf("bag = %+v, want old weapon returned", p.EquipBag)
		}
		if p.EquipBag[0].Upgrade != 1 {
			t.Fatalf("old weapon upgrade = %d, want target previous +1", p.EquipBag[0].Upgrade)
		}
	})

	var result protocol.CraftResult
	if !readEnvelope(sess.Send, protocol.TypeCraftResult, &result) {
		t.Fatal("no craft result sent")
	}
	if !result.OK {
		t.Fatalf("transfer should succeed: %+v", result)
	}
	if result.UID != "new_weapon" || result.Upgrade != 6 {
		t.Fatalf("craft result = %+v, want target uid and inherited upgrade", result)
	}
	if !strings.Contains(result.Msg, "继承强化成功") {
		t.Fatalf("craft result msg = %q, want transfer hint", result.Msg)
	}
}

func TestHandleTransferUpgrade_LockedEquipmentRejected(t *testing.T) {
	store := save.NewMemoryStore()
	hub := NewHub(store)
	sess := &Session{Account: "hero", Send: make(chan []byte, 16)}
	store.LoadOrCreate("hero")
	store.WithPlayer("hero", func(p *model.Player) {
		p.EquipBag = append(p.EquipBag,
			&model.Equipment{UID: "source", Slot: data.SlotWeapon, Upgrade: 3},
			&model.Equipment{UID: "target", Slot: data.SlotWeapon, Upgrade: 0},
		)
		p.Locked["source"] = true
	})

	reqData, _ := json.Marshal(protocol.TransferUpgradeRequest{SourceUID: "source", TargetUID: "target"})
	env := protocol.Envelope{T: protocol.TypeTransferUpg, ID: "transfer", Data: reqData}
	raw, _ := json.Marshal(env)
	hub.handleMessage(sess, raw)

	store.WithPlayer("hero", func(p *model.Player) {
		if p.EquipBag[0].Upgrade != 3 || p.EquipBag[1].Upgrade != 0 {
			t.Fatalf("locked transfer changed upgrades: %+v", p.EquipBag)
		}
	})
	var result protocol.CraftResult
	if !readEnvelope(sess.Send, protocol.TypeCraftResult, &result) {
		t.Fatal("no craft result sent")
	}
	if result.OK {
		t.Fatal("locked transfer should fail")
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
