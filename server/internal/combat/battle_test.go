package combat

import "testing"

import "equipment-idle-server/internal/data"

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

func TestResolveBattle_ArtifactTriggers(t *testing.T) {
	tests := []struct {
		name    string
		effect  data.ArtifactDefinition
		input   BattleInput
		wantEvt string
	}{
		{
			name:    "echo strike adds extra hit",
			effect:  data.ArtifactDefinition{ID: "artifact_echo_blade", Trigger: data.ArtifactEchoStrike, Value: 0.45},
			input:   BattleInput{PlayerPower: 120, EnemyPower: 220},
			wantEvt: EventEcho,
		},
		{
			name:    "opening shield emits shield event",
			effect:  data.ArtifactDefinition{ID: "artifact_aegis_heart", Trigger: data.ArtifactOpeningShield, Value: 0.25},
			input:   BattleInput{PlayerPower: 120, EnemyPower: 220},
			wantEvt: EventShield,
		},
		{
			name:    "execute finishes low hp enemy",
			effect:  data.ArtifactDefinition{ID: "artifact_cull_signet", Trigger: data.ArtifactExecute, Value: 0.18},
			input:   BattleInput{PlayerPower: 120, EnemyPower: 150},
			wantEvt: EventExecute,
		},
		{
			name:    "hit heal restores player",
			effect:  data.ArtifactDefinition{ID: "artifact_blood_well", Trigger: data.ArtifactHitHeal, Value: 0.12},
			input:   BattleInput{PlayerPower: 90, EnemyPower: 220},
			wantEvt: EventHeal,
		},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tt.input.Artifacts = []data.ArtifactDefinition{tt.effect}
			result := ResolveBattle(tt.input)
			if !hasEventKind(result.Events, tt.wantEvt) {
				t.Fatalf("events = %+v, want kind %q", result.Events, tt.wantEvt)
			}
		})
	}
}

func TestResolveBattle_EnemyResistanceReducesMatchingElementDamage(t *testing.T) {
	base := ResolveBattle(BattleInput{
		PlayerPower: 120,
		EnemyPower:  180,
		PlayerStats: Stats{data.ATFireDmg: 100},
	})
	resisted := ResolveBattle(BattleInput{
		PlayerPower: 120,
		EnemyPower:  180,
		PlayerStats: Stats{data.ATFireDmg: 100},
		EnemyResistances: map[data.AffixType]float64{
			data.ATFireDmg: 0.50,
		},
	})
	if len(base.Events) == 0 || len(resisted.Events) == 0 {
		t.Fatal("battle should emit events")
	}
	if resisted.Events[0].Damage >= base.Events[0].Damage {
		t.Fatalf("resisted fire hit damage = %.2f, want below %.2f", resisted.Events[0].Damage, base.Events[0].Damage)
	}
}

func TestResolveBattle_EnemyVulnerabilityIncreasesMatchingElementDamage(t *testing.T) {
	base := ResolveBattle(BattleInput{
		PlayerPower: 120,
		EnemyPower:  180,
		PlayerStats: Stats{data.ATLightningDmg: 100},
	})
	vulnerable := ResolveBattle(BattleInput{
		PlayerPower: 120,
		EnemyPower:  180,
		PlayerStats: Stats{data.ATLightningDmg: 100},
		EnemyResistances: map[data.AffixType]float64{
			data.ATLightningDmg: -0.30,
		},
	})
	if vulnerable.Events[0].Damage <= base.Events[0].Damage {
		t.Fatalf("vulnerable lightning hit damage = %.2f, want above %.2f", vulnerable.Events[0].Damage, base.Events[0].Damage)
	}
}

func hasEventKind(events []HitEvent, kind string) bool {
	for _, event := range events {
		if event.Kind == kind {
			return true
		}
	}
	return false
}
