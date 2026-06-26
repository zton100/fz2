package inventory

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func makePlayerWithBag() *model.Player {
	p := model.NewPlayer("test")
	p.AddEquipment(&model.Equipment{UID: "e1", BaseID: "base_helmet", Name: "皮盔", Slot: data.SlotHelmet})
	p.AddEquipment(&model.Equipment{UID: "e2", BaseID: "base_weapon", Name: "铁剑", Slot: data.SlotWeapon})
	return p
}

func TestEquip_FromBagToSlot(t *testing.T) {
	p := makePlayerWithBag()
	err := Equip(p, "e1")
	if err != nil {
		t.Fatalf("equip error: %v", err)
	}
	if p.Equipped[data.SlotHelmet] == nil || p.Equipped[data.SlotHelmet].UID != "e1" {
		t.Fatal("helmet slot should have e1")
	}
	if findInBag(p, "e1") != -1 {
		t.Fatal("e1 should be removed from bag after equip")
	}
}

func TestEquip_ReplacesOldEquipment(t *testing.T) {
	p := makePlayerWithBag()
	Equip(p, "e1")
	p.AddEquipment(&model.Equipment{UID: "e3", BaseID: "base_helmet", Name: "皮盔2", Slot: data.SlotHelmet})
	err := Equip(p, "e3")
	if err != nil {
		t.Fatalf("equip e3 error: %v", err)
	}
	if p.Equipped[data.SlotHelmet].UID != "e3" {
		t.Fatal("helmet slot should have e3 now")
	}
	if findInBag(p, "e1") == -1 {
		t.Fatal("e1 should be back in bag after replaced")
	}
}

func TestEquip_UIDNotFound(t *testing.T) {
	p := makePlayerWithBag()
	err := Equip(p, "nope")
	if err == nil {
		t.Fatal("should error when UID not found")
	}
}

func TestUnequip_MoveBackToBag(t *testing.T) {
	p := makePlayerWithBag()
	Equip(p, "e1")
	err := Unequip(p, data.SlotHelmet)
	if err != nil {
		t.Fatalf("unequip error: %v", err)
	}
	if p.Equipped[data.SlotHelmet] != nil {
		t.Fatal("helmet slot should be empty after unequip")
	}
	if findInBag(p, "e1") == -1 {
		t.Fatal("e1 should be back in bag after unequip")
	}
}

func TestUnequip_EmptySlot(t *testing.T) {
	p := makePlayerWithBag()
	err := Unequip(p, data.SlotBoots)
	if err == nil {
		t.Fatal("should error when unequip empty slot")
	}
}
