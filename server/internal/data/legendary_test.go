package data

import "testing"

func TestAllLegendariesContainsTwelveValidDefinitionsAcrossEverySlot(t *testing.T) {
	defs := AllLegendaries()
	if LegendaryDataVersion() != 1 {
		t.Fatalf("legendary data version = %d, want 1", LegendaryDataVersion())
	}
	if len(defs) != 12 {
		t.Fatalf("legendary count = %d, want 12", len(defs))
	}
	ids := map[string]bool{}
	slots := map[Slot]int{}
	for _, def := range defs {
		if ids[def.ID] {
			t.Fatalf("duplicate legendary id %q", def.ID)
		}
		ids[def.ID] = true
		slots[def.Slot]++
		if len(def.BonusStats) == 0 && def.PowerMultiplier == 0 && def.BossRewardMultiplier == 0 {
			t.Fatalf("legendary %q has no real effect", def.ID)
		}
		base, ok := BaseByID(def.BaseID)
		if !ok || base.Slot != def.Slot {
			t.Fatalf("legendary %q has invalid base %q", def.ID, def.BaseID)
		}
	}
	for _, slot := range AllSlots() {
		if slots[slot] == 0 {
			t.Fatalf("slot %d has no legendary definition", slot)
		}
	}
}
