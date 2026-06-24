package save

import (
	"testing"
)

func TestMemoryStore_LoadOrCreate_NewPlayer(t *testing.T) {
	store := NewMemoryStore()
	p := store.LoadOrCreate("hero")
	if p.Account != "hero" {
		t.Fatalf("account = %q, want hero", p.Account)
	}
	if p.Floor != 1 {
		t.Fatalf("floor = %d, want 1", p.Floor)
	}
	if len(p.Inventory) != 0 {
		t.Fatalf("inventory len = %d, want 0", len(p.Inventory))
	}
}

func TestMemoryStore_LoadOrCreate_ExistingPlayer(t *testing.T) {
	store := NewMemoryStore()
	p1 := store.LoadOrCreate("hero")
	p1.Floor = 5
	p2 := store.LoadOrCreate("hero")
	if p2.Floor != 5 {
		t.Fatalf("floor = %d, want 5 (should reuse existing)", p2.Floor)
	}
	if p1 != p2 {
		t.Fatal("should return same pointer for same account")
	}
}
