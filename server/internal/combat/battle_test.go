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

func TestBattle_EmitsAuthoritativeHitTimeline(t *testing.T) {
	r := Battle(220, 100)
	if !r.Win {
		t.Fatal("player should win")
	}
	if r.PlayerStartHP <= 0 || r.EnemyStartHP <= 0 {
		t.Fatalf("start hp = player %.2f enemy %.2f, want positive", r.PlayerStartHP, r.EnemyStartHP)
	}
	if len(r.Events) == 0 {
		t.Fatal("battle should emit hit events for client animation")
	}
	first := r.Events[0]
	if first.Actor != ActorPlayer || first.Kind != EventHit {
		t.Fatalf("first event = %+v, want player hit", first)
	}
	if first.Damage <= 0 {
		t.Fatalf("first event damage = %.2f, want positive", first.Damage)
	}
	if r.EnemyEndHP != 0 {
		t.Fatalf("enemy end hp = %.2f, want 0 after win", r.EnemyEndHP)
	}
}

func TestResolveBattle_StatsAffectTimeline(t *testing.T) {
	base := ResolveBattle(BattleInput{PlayerPower: 120, EnemyPower: 160})
	withSurvival := ResolveBattle(BattleInput{
		PlayerPower: 120,
		EnemyPower:  160,
		PlayerStats: Stats{
			"max_hp": 200,
			"shield": 80,
		},
	})
	if withSurvival.PlayerStartHP <= base.PlayerStartHP {
		t.Fatalf("player start hp = %.2f, want above base %.2f", withSurvival.PlayerStartHP, base.PlayerStartHP)
	}
	if withSurvival.PlayerStartShield <= base.PlayerStartShield {
		t.Fatalf("player start shield = %.2f, want above base %.2f", withSurvival.PlayerStartShield, base.PlayerStartShield)
	}
}
