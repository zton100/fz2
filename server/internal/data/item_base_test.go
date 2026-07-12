package data

import "testing"

func TestBaseBySlot_Matches(t *testing.T) {
	for _, slot := range AllSlots() {
		b := BaseBySlot(slot)
		if b.Slot != slot {
			t.Fatalf("BaseBySlot(%d).Slot = %d", slot, b.Slot)
		}
	}
}

func TestAllBases_FiveUniqueBasesPerSlot(t *testing.T) {
	bases := AllBases()
	if EquipmentDataVersion() != 1 {
		t.Fatalf("equipment data version = %d, want 1", EquipmentDataVersion())
	}
	if len(bases) != 40 {
		t.Fatalf("AllBases len = %d, want 40", len(bases))
	}
	ids := map[string]bool{}
	counts := map[Slot]int{}
	for _, b := range bases {
		if b.ID == "" || b.Name == "" {
			t.Fatalf("base %+v has empty ID or Name", b)
		}
		if ids[b.ID] {
			t.Fatalf("duplicate base id %q", b.ID)
		}
		ids[b.ID] = true
		counts[b.Slot]++
	}
	for _, slot := range AllSlots() {
		if counts[slot] != 5 {
			t.Fatalf("slot %d base count = %d, want 5", slot, counts[slot])
		}
	}
}
