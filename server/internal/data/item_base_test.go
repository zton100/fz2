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

func TestAllBases_Count8(t *testing.T) {
	bases := AllBases()
	if len(bases) != 8 {
		t.Fatalf("AllBases len = %d, want 8", len(bases))
	}
	for _, b := range bases {
		if b.ID == "" || b.Name == "" {
			t.Fatalf("base %+v has empty ID or Name", b)
		}
	}
}
