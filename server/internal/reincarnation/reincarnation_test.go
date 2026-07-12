package reincarnation

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestCanReincarn_NotEnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	if CanReincarnate(p) {
		t.Fatal("floor 5 should not allow reincarnate")
	}
}

func TestCanReincarn_EnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 10
	if !CanReincarnate(p) {
		t.Fatal("floor 10 should allow reincarnate")
	}
}

func TestReincarnate_ResetsProgressGivesSouls(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 15
	p.FloorKills = 2
	p.MaxFloor = 15
	p.Equipped[data.SlotWeapon] = &model.Equipment{UID: "e1"}
	p.AddEquipment(&model.Equipment{UID: "e2"})
	p.AddMaterial(data.MatBase, 50)

	soulsBefore := p.Souls
	err := Reincarnate(p)
	if err != nil {
		t.Fatalf("reincarnate error: %v", err)
	}
	if p.Floor != 1 {
		t.Fatalf("floor = %d, want 1", p.Floor)
	}
	if p.FloorKills != 0 {
		t.Fatalf("floor kills = %d, want 0", p.FloorKills)
	}
	if len(p.EquipBag) != 0 {
		t.Fatalf("bag = %d, want 0", len(p.EquipBag))
	}
	if len(p.Equipped) != 0 {
		t.Fatalf("equipped = %d, want 0", len(p.Equipped))
	}
	if p.Materials[data.MatBase] != 0 {
		t.Fatalf("materials should be reset")
	}
	if p.Souls != soulsBefore+3 {
		t.Fatalf("souls = %d, want %d", p.Souls, soulsBefore+3)
	}
	if p.MaxFloor != 15 {
		t.Fatalf("MaxFloor = %d, want 15", p.MaxFloor)
	}
}

func TestReincarnate_NotEnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	err := Reincarnate(p)
	if err == nil {
		t.Fatal("should error when floor < 10")
	}
}

func TestReincarnate_MaxFloorUpdates(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 20
	p.MaxFloor = 15
	Reincarnate(p)
	if p.MaxFloor != 20 {
		t.Fatalf("MaxFloor = %d, want 20", p.MaxFloor)
	}
}

func TestTalentUpgrade_Success(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 5
	err := UpgradeTalent(p, "damage")
	if err != nil {
		t.Fatalf("talent upgrade error: %v", err)
	}
	if p.Talents["damage"] != 1 {
		t.Fatalf("damage talent = %d, want 1", p.Talents["damage"])
	}
	if p.Souls != 4 {
		t.Fatalf("souls = %d, want 4", p.Souls)
	}
}

func TestTalentUpgrade_MaxLevel(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 100
	for i := 0; i < 10; i++ {
		UpgradeTalent(p, "damage")
	}
	err := UpgradeTalent(p, "damage")
	if err == nil {
		t.Fatal("should error at max level")
	}
}

func TestTalentUpgrade_NoSouls(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 0
	err := UpgradeTalent(p, "damage")
	if err == nil {
		t.Fatal("should error when no souls")
	}
}

func TestDamageBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["damage"] = 4
	bonus := DamageBonus(p)
	if bonus != 0.20 {
		t.Fatalf("damage bonus = %.2f, want 0.20", bonus)
	}
}

func TestDropBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["drop"] = 3
	bonus := DropBonus(p)
	if bonus != 0.09 {
		t.Fatalf("drop bonus = %.2f, want 0.09", bonus)
	}
}

func TestDropBonus_IncludesEquippedDropRate(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["drop"] = 2
	p.Equipped[data.SlotRing1] = &model.Equipment{
		Affixes: []model.AffixInstance{{Type: data.ATDropRate, Value: 0.12}},
	}
	if bonus := DropBonus(p); bonus != 0.18 {
		t.Fatalf("drop bonus = %.2f, want talent 0.06 + equipment 0.12", bonus)
	}
}

func TestApplyResourceGain_UsesEquippedAffix(t *testing.T) {
	p := model.NewPlayer("t")
	p.Equipped[data.SlotNeck] = &model.Equipment{
		Affixes: []model.AffixInstance{{Type: data.ATResourceGain, Value: 0.50}},
	}
	if got := ApplyResourceGain(p, 10); got != 15 {
		t.Fatalf("resource gain = %d, want 15", got)
	}
}

func TestApplyBossRewardCombinesResourceAndLegendaryBonuses(t *testing.T) {
	p := model.NewPlayer("legendary-reward")
	p.Equipped[data.SlotArmor] = &model.Equipment{
		LegendaryID: "legendary_ash_mantle",
		Slot:        data.SlotArmor,
		BaseStats:   map[data.AffixType]float64{},
	}
	p.Equipped[data.SlotNeck] = &model.Equipment{
		LegendaryID: "legendary_harvest_seal",
		Slot:        data.SlotNeck,
		BaseStats:   map[data.AffixType]float64{},
	}

	// 15% fixed resource gain + 25% and 20% boss bonuses = 60% total.
	if got := ApplyBossReward(p, 100); got != 160 {
		t.Fatalf("boss reward = %d, want 160", got)
	}
}

func TestOfflineGainBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["offline_gain"] = 2
	bonus := OfflineGainBonus(p)
	if bonus != 0.20 {
		t.Fatalf("offline gain bonus = %.2f, want 0.20", bonus)
	}
}

func TestQualityFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["quality"] = 1
	floor := QualityFloor(p)
	if floor != data.RarityMagic {
		t.Fatalf("quality floor = %d, want magic(%d)", floor, data.RarityMagic)
	}
}

func TestReincarnate_HistoricMaxFloorDoesNotRepeatReward(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 10
	p.MaxFloor = 100
	p.Souls = 0
	err := Reincarnate(p)
	if err != nil {
		t.Fatalf("reincarnate error: %v", err)
	}
	if p.Souls != 2 {
		t.Fatalf("souls = %d, want 2 (Floor/5=2, not MaxFloor/5=20)", p.Souls)
	}
	if p.MaxFloor != 100 {
		t.Fatalf("MaxFloor = %d, want 100 (preserved)", p.MaxFloor)
	}
}

func TestAdvanceFloor_UpdatesBothFloorAndMaxFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 9
	p.MaxFloor = 9
	AdvanceFloor(p)
	if p.Floor != 10 {
		t.Fatalf("Floor = %d, want 10", p.Floor)
	}
	if p.MaxFloor != 10 {
		t.Fatalf("MaxFloor = %d, want 10", p.MaxFloor)
	}
}

func TestAdvanceFloor_MaxFloorHigherDoesNotDecrease(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	p.MaxFloor = 20
	AdvanceFloor(p)
	if p.Floor != 6 {
		t.Fatalf("Floor = %d, want 6", p.Floor)
	}
	if p.MaxFloor != 20 {
		t.Fatalf("MaxFloor = %d, want 20 (unchanged)", p.MaxFloor)
	}
}
