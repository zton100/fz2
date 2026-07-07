package main

import (
	"testing"

	"equipment-idle-server/internal/protocol"
)

func TestFindTransferPairChoosesDroppedSameSlotTarget(t *testing.T) {
	equipped := []protocol.EquipmentDTO{
		{UID: "old_weapon", Slot: 0},
		{UID: "old_helmet", Slot: 1},
	}
	bag := []protocol.EquipmentDTO{
		{UID: "drop_ring", Slot: 5},
		{UID: "drop_helmet", Slot: 1},
	}

	source, target := findTransferPair(equipped, bag)

	if source == nil || source.UID != "old_helmet" {
		t.Fatalf("source = %+v, want old_helmet", source)
	}
	if target == nil || target.UID != "drop_helmet" {
		t.Fatalf("target = %+v, want drop_helmet", target)
	}
}

func TestFindTransferPairReturnsNilWithoutSameSlotDrop(t *testing.T) {
	equipped := []protocol.EquipmentDTO{{UID: "old_weapon", Slot: 0}}
	bag := []protocol.EquipmentDTO{{UID: "drop_helmet", Slot: 1}}

	source, target := findTransferPair(equipped, bag)

	if source != nil || target != nil {
		t.Fatalf("pair = %+v -> %+v, want nil pair", source, target)
	}
}
