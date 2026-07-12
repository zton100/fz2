package data

import "testing"

func TestArtifactDefinitions(t *testing.T) {
	if ArtifactDataVersion() != 1 {
		t.Fatalf("artifact data version = %d, want 1", ArtifactDataVersion())
	}
	defs := AllArtifacts()
	if len(defs) != 4 {
		t.Fatalf("artifact count = %d, want 4", len(defs))
	}
	seen := map[string]bool{}
	triggers := map[string]bool{}
	for _, def := range defs {
		if seen[def.ID] {
			t.Fatalf("duplicate artifact id %q", def.ID)
		}
		seen[def.ID] = true
		triggers[def.Trigger] = true
		if def.Value <= 0 {
			t.Fatalf("artifact %q has no trigger value", def.ID)
		}
		base, ok := BaseByID(def.BaseID)
		if !ok || base.Slot != def.Slot {
			t.Fatalf("artifact %q has invalid base %q", def.ID, def.BaseID)
		}
	}
	for _, trigger := range []string{ArtifactEchoStrike, ArtifactOpeningShield, ArtifactExecute, ArtifactHitHeal} {
		if !triggers[trigger] {
			t.Fatalf("missing artifact trigger %q", trigger)
		}
	}
}
