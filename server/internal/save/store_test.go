package save

import "fmt"



import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestStore_SaveAndLoad_BasicFields(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	// Create player with specific data
	p := s.LoadOrCreate("hero")
	p.Floor = 12
	p.Souls = 3
	p.MaxFloor = 20
	p.Talents["damage"] = 2
	p.Talents["drop"] = 1
	p.Materials[data.MatBase] = 50

	if err := s.Save("hero"); err != nil {
		t.Fatalf("save error: %v", err)
	}

	// Create a fresh store and reload
	s2 := NewStore(dir)
	p2 := s2.LoadOrCreate("hero")

	if p2.Floor != 12 {
		t.Errorf("Floor = %d, want 12", p2.Floor)
	}
	if p2.Souls != 3 {
		t.Errorf("Souls = %d, want 3", p2.Souls)
	}
	if p2.MaxFloor != 20 {
		t.Errorf("MaxFloor = %d, want 20", p2.MaxFloor)
	}
	if p2.Talents["damage"] != 2 {
		t.Errorf("Talent damage = %d, want 2", p2.Talents["damage"])
	}
	if p2.Talents["drop"] != 1 {
		t.Errorf("Talent drop = %d, want 1", p2.Talents["drop"])
	}
	if p2.Materials[data.MatBase] != 50 {
		t.Errorf("Material base_mat = %d, want 50", p2.Materials[data.MatBase])
	}
}

func TestStore_SaveAndLoad_EquipBag(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	p := s.LoadOrCreate("hero")
	eq := &model.Equipment{
		UID: "eq_test1", BaseID: "base_weapon", Name: "TestSword",
		Slot: data.SlotWeapon, Rarity: data.RarityRare, Upgrade: 3,
		Affixes: []model.AffixInstance{
			{Type: data.ATStrength, Tier: 2, Value: 5.0},
		},
	}
	p.EquipBag = append(p.EquipBag, eq)

	if err := s.Save("hero"); err != nil {
		t.Fatalf("save error: %v", err)
	}

	s2 := NewStore(dir)
	p2 := s2.LoadOrCreate("hero")

	if len(p2.EquipBag) != 1 {
		t.Fatalf("EquipBag len = %d, want 1", len(p2.EquipBag))
	}
	e := p2.EquipBag[0]
	if e.UID != "eq_test1" {
		t.Errorf("UID = %s, want eq_test1", e.UID)
	}
	if e.Rarity != data.RarityRare {
		t.Errorf("Rarity = %d, want %d", e.Rarity, data.RarityRare)
	}
	if e.Upgrade != 3 {
		t.Errorf("Upgrade = %d, want 3", e.Upgrade)
	}
	if len(e.Affixes) != 1 || e.Affixes[0].Value != 5.0 {
		t.Errorf("Affixes mismatch: %+v", e.Affixes)
	}
}

func TestStore_SaveAndLoad_Equipped(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)

	p := s.LoadOrCreate("hero")
	eq := &model.Equipment{
		UID: "eq_weapon1", BaseID: "base_weapon", Name: "Sword",
		Slot: data.SlotWeapon, Rarity: data.RarityLegendary, Upgrade: 5,
	}
	p.Equipped[data.SlotWeapon] = eq

	if err := s.Save("hero"); err != nil {
		t.Fatalf("save error: %v", err)
	}

	s2 := NewStore(dir)
	p2 := s2.LoadOrCreate("hero")

	e, ok := p2.Equipped[data.SlotWeapon]
	if !ok {
		t.Fatal("Equipped weapon slot is empty after reload")
	}
	if e.UID != "eq_weapon1" {
		t.Errorf("UID = %s, want eq_weapon1", e.UID)
	}
	if e.Rarity != data.RarityLegendary {
		t.Errorf("Rarity = %d, want %d", e.Rarity, data.RarityLegendary)
	}
}


func TestStore_ConcurrentLoadOrCreate(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	const goroutines = 20
	done := make(chan struct{})
	for i := 0; i < goroutines; i++ {
		go func(id int) {
			_ = s.LoadOrCreate("hero")
			done <- struct{}{}
		}(i)
	}
	for i := 0; i < goroutines; i++ {
		<-done
	}
}

func TestStore_ConcurrentMarkDirty(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	_ = s.LoadOrCreate("hero")
	const goroutines = 20
	done := make(chan struct{})
	for i := 0; i < goroutines; i++ {
		go func() {
			s.MarkDirty("hero")
			done <- struct{}{}
		}()
	}
	for i := 0; i < goroutines; i++ {
		<-done
	}
}

func TestStore_ConcurrentMultipleAccounts(t *testing.T) {
	dir := t.TempDir()
	s := NewStore(dir)
	const goroutines = 10
	done := make(chan struct{})
	for i := 0; i < goroutines; i++ {
		go func(id int) {
			account := fmt.Sprintf("player%d", id)
			p := s.LoadOrCreate(account)
			p.Floor = id + 1
			s.Save(account)
			done <- struct{}{}
		}(i)
	}
	for i := 0; i < goroutines; i++ {
		<-done
	}
}
