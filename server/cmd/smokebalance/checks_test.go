package main

import (
	"strings"
	"testing"

	"equipment-idle-server/internal/data"
)

func TestCheckEarlyCycle(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		Ticks:      9,
		FinalFloor: cfg.EarlyTargetFloor,
		LootCount:  cfg.EarlyMinLoot,
		StartPower: 100,
		FinalPower: 120,
	}
	if failures := checkEarlyCycle(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid cycle got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "ticks too low",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.EarlyTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "ticks too high",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.EarlyTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "floor gate missed",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.EarlyTargetFloor - 1 },
			want: "final floor",
		},
		{
			name: "too little loot",
			edit: func(metrics *cycleMetrics) { metrics.LootCount = cfg.EarlyMinLoot - 1 },
			want: "loot count",
		},
		{
			name: "power regressed",
			edit: func(metrics *cycleMetrics) { metrics.FinalPower = 99 },
			want: "below start power",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			tt.edit(&metrics)
			assertFailureContains(t, checkEarlyCycle(metrics, cfg), tt.want)
		})
	}
}

func TestCheckReincarnationSouls(t *testing.T) {
	cfg := defaultBalanceConfig()
	if failures := checkReincarnationSouls(4, 2, cfg); len(failures) != 0 {
		t.Fatalf("valid souls got failures: %v", failures)
	}
	assertFailureContains(t, checkReincarnationSouls(3, 2, cfg), "souls after reincarnation = 3, want 4")
}

func TestCheckLongRun(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		FinalFloor: cfg.LongTargetFloor,
		Ticks:      30,
		BossReward: cfg.LongMinBossReward,
	}
	if failures := checkLongRun(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid long run got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "target not reached",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.LongTargetFloor - 1 },
			want: "long run reached floor",
		},
		{
			name: "too fast",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.LongTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "too slow",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.LongTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "boss reward low",
			edit: func(metrics *cycleMetrics) { metrics.BossReward = cfg.LongMinBossReward - 1 },
			want: "boss reward",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			tt.edit(&metrics)
			assertFailureContains(t, checkLongRun(metrics, cfg), tt.want)
		})
	}
}

func TestCheckDeepRun(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		FinalFloor: cfg.DeepTargetFloor,
		Ticks:      20,
		AffixCategoryCounts: map[data.AffixCategory]int{
			data.AffixSpecial: 1,
		},
	}
	if failures := checkDeepRun(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid deep run got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "target not reached",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.DeepTargetFloor - 1 },
			want: "deep run reached floor",
		},
		{
			name: "too fast",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.DeepTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "too slow",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.DeepTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "no special affixes",
			edit: func(metrics *cycleMetrics) { metrics.AffixCategoryCounts[data.AffixSpecial] = 0 },
			want: "saw no special affixes",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			metrics.AffixCategoryCounts = map[data.AffixCategory]int{
				data.AffixSpecial: valid.AffixCategoryCounts[data.AffixSpecial],
			}
			tt.edit(&metrics)
			assertFailureContains(t, checkDeepRun(metrics, cfg), tt.want)
		})
	}
}

func TestCheckLateRun(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		FinalFloor: cfg.LateTargetFloor,
		Ticks:      40,
		RarityCounts: map[data.Rarity]int{
			data.RarityArtifact: cfg.LateMinArtifactDrops,
		},
	}
	if failures := checkLateRun(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid late run got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "target not reached",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.LateTargetFloor - 1 },
			want: "late run reached floor",
		},
		{
			name: "too fast",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.LateTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "too slow",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.LateTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "artifact drop missing",
			edit: func(metrics *cycleMetrics) { metrics.RarityCounts[data.RarityArtifact] = cfg.LateMinArtifactDrops - 1 },
			want: "artifact drops",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			metrics.RarityCounts = map[data.Rarity]int{
				data.RarityArtifact: valid.RarityCounts[data.RarityArtifact],
			}
			tt.edit(&metrics)
			assertFailureContains(t, checkLateRun(metrics, cfg), tt.want)
		})
	}
}

func TestCheckEndgameRun(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		FinalFloor: cfg.EndgameTargetFloor,
		Ticks:      60,
		RarityCounts: map[data.Rarity]int{
			data.RarityArtifact: cfg.EndgameMinArtifactDrops,
		},
	}
	if failures := checkEndgameRun(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid endgame run got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "target not reached",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.EndgameTargetFloor - 1 },
			want: "endgame run reached floor",
		},
		{
			name: "too fast",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.EndgameTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "too slow",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.EndgameTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "artifact drop missing",
			edit: func(metrics *cycleMetrics) {
				metrics.RarityCounts[data.RarityArtifact] = cfg.EndgameMinArtifactDrops - 1
			},
			want: "artifact drops",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			metrics.RarityCounts = map[data.Rarity]int{
				data.RarityArtifact: valid.RarityCounts[data.RarityArtifact],
			}
			tt.edit(&metrics)
			assertFailureContains(t, checkEndgameRun(metrics, cfg), tt.want)
		})
	}
}

func TestCheckSecondLoop(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		StartPower: 110,
		FinalFloor: cfg.SecondLoopTargetFloor,
		Ticks:      49,
	}
	if failures := checkSecondLoop(valid, cfg, 100, 50, 1); len(failures) != 0 {
		t.Fatalf("valid second loop got failures: %v", failures)
	}

	tests := []struct {
		name        string
		metrics     cycleMetrics
		firstPower  float64
		firstTicks  int
		spentDamage int
		want        string
	}{
		{
			name:        "damage not spent",
			metrics:     valid,
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 0,
			want:        "did not spend reincarnation souls",
		},
		{
			name:        "start power not improved",
			metrics:     cycleMetrics{StartPower: 100, FinalFloor: cfg.SecondLoopTargetFloor, Ticks: 49},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "should exceed first loop start power",
		},
		{
			name:        "target not reached",
			metrics:     cycleMetrics{StartPower: 110, FinalFloor: cfg.SecondLoopTargetFloor - 1, Ticks: 49},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "second loop reached floor",
		},
		{
			name:        "slower than first loop",
			metrics:     cycleMetrics{StartPower: 110, FinalFloor: cfg.SecondLoopTargetFloor, Ticks: 51},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "should not exceed first loop ticks",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			assertFailureContains(t, checkSecondLoop(tt.metrics, cfg, tt.firstPower, tt.firstTicks, tt.spentDamage), tt.want)
		})
	}
}

func assertFailureContains(t *testing.T, failures []string, want string) {
	t.Helper()
	if len(failures) == 0 {
		t.Fatalf("got no failures, want one containing %q", want)
	}
	for _, failure := range failures {
		if strings.Contains(failure, want) {
			return
		}
	}
	t.Fatalf("failures %v do not contain %q", failures, want)
}
