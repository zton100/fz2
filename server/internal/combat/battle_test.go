package combat

import "testing"

func TestBattle_PlayerWins_WhenPowerExceeds(t *testing.T) {
	r := Battle(500, 100)
	if !r.Win {
		t.Fatal("player power 500 vs monster 100 should win")
	}
}

func TestBattle_PlayerLoses_WhenPowerLower(t *testing.T) {
	r := Battle(50, 100)
	if r.Win {
		t.Fatal("player power 50 vs monster 100 should lose")
	}
}

func TestBattle_TieCountsAsWin(t *testing.T) {
	r := Battle(100, 100)
	if !r.Win {
		t.Fatal("equal power should count as win")
	}
}
