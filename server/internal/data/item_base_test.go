package data

import "testing"

func TestAllItemBases_Count8(t *testing.T) {
	bases := AllItemBases()
	if len(bases) != 8 {
		t.Fatalf("bases count = %d, want 8", len(bases))
	}
	seen := map[Slot]bool{}
	for _, b := range bases {
		if seen[b.Slot] {
			t.Fatalf("duplicate slot %d", b.Slot)
		}
		seen[b.Slot] = true
		if len(b.BaseStats) == 0 {
			t.Fatalf("base %s has empty BaseStats", b.ID)
		}
	}
}

func TestBaseBySlot(t *testing.T) {
	b := BaseBySlot(SlotWeapon)
	if b.Slot != SlotWeapon {
		t.Fatalf("got slot %d, want weapon", b.Slot)
	}
	if b.ID != "base_weapon" {
		t.Fatalf("got id %s, want base_weapon", b.ID)
	}
}
