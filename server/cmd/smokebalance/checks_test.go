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

func TestCheckFrontierRun(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		FinalFloor:          cfg.FrontierTargetFloor,
		Ticks:               80,
		MatchedUpgradeDrops: cfg.FrontierMinMatchedUpgradeDrops,
		TransferCount:       cfg.FrontierMinTransfers,
		RarityCounts: map[data.Rarity]int{
			data.RarityArtifact: cfg.FrontierMinArtifactDrops,
		},
	}
	if failures := checkFrontierRun(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid frontier run got failures: %v", failures)
	}

	tests := []struct {
		name string
		edit func(*cycleMetrics)
		want string
	}{
		{
			name: "target not reached",
			edit: func(metrics *cycleMetrics) { metrics.FinalFloor = cfg.FrontierTargetFloor - 1 },
			want: "frontier run reached floor",
		},
		{
			name: "too fast",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.FrontierTicks.Min - 1 },
			want: "ticks out of target range",
		},
		{
			name: "too slow",
			edit: func(metrics *cycleMetrics) { metrics.Ticks = cfg.FrontierTicks.Max + 1 },
			want: "ticks out of target range",
		},
		{
			name: "artifact drop missing",
			edit: func(metrics *cycleMetrics) {
				metrics.RarityCounts[data.RarityArtifact] = cfg.FrontierMinArtifactDrops - 1
			},
			want: "artifact drops",
		},
		{
			name: "matched upgrade drop missing",
			edit: func(metrics *cycleMetrics) {
				metrics.MatchedUpgradeDrops = cfg.FrontierMinMatchedUpgradeDrops - 1
			},
			want: "matched-upgrade drops",
		},
		{
			name: "upgrade transfer missing",
			edit: func(metrics *cycleMetrics) {
				metrics.TransferCount = cfg.FrontierMinTransfers - 1
			},
			want: "upgrade transfers",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			metrics := valid
			metrics.RarityCounts = map[data.Rarity]int{
				data.RarityArtifact: valid.RarityCounts[data.RarityArtifact],
			}
			tt.edit(&metrics)
			assertFailureContains(t, checkFrontierRun(metrics, cfg), tt.want)
		})
	}
}

func TestCheckArtifactDistribution(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := artifactDistributionMetrics{
		Endgame: artifactSegmentDistribution{
			Name:          "80 -> 120",
			SeedCount:     len(cfg.ArtifactDistributionSeeds),
			Total:         cfg.EndgameArtifactDistribution.MinTotal,
			SeedsWithDrop: cfg.EndgameArtifactDistribution.MinSeedsWithDrop,
		},
		Frontier: artifactSegmentDistribution{
			Name:          "120 -> 160",
			SeedCount:     len(cfg.ArtifactDistributionSeeds),
			Total:         cfg.FrontierArtifactDistribution.MinTotal,
			SeedsWithDrop: cfg.FrontierArtifactDistribution.MinSeedsWithDrop,
		},
	}
	if failures := checkArtifactDistribution(valid, cfg); len(failures) != 0 {
		t.Fatalf("valid artifact distribution got failures: %v", failures)
	}

	totalLow := valid
	totalLow.Frontier.Total = cfg.FrontierArtifactDistribution.MinTotal - 1
	assertFailureContains(t, checkArtifactDistribution(totalLow, cfg), "artifact total")

	seedsLow := valid
	seedsLow.Endgame.SeedsWithDrop = cfg.EndgameArtifactDistribution.MinSeedsWithDrop - 1
	assertFailureContains(t, checkArtifactDistribution(seedsLow, cfg), "seeds_with_drop")
}

func TestCheckPostReincarnationLoop(t *testing.T) {
	cfg := defaultBalanceConfig()
	valid := cycleMetrics{
		StartPower: 110,
		FinalFloor: cfg.PostReincarnationTargetFloor,
		Ticks:      49,
	}
	if failures := checkPostReincarnationLoop(valid, cfg, 100, 50, 1); len(failures) != 0 {
		t.Fatalf("valid post-reincarnation loop got failures: %v", failures)
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
			metrics:     cycleMetrics{StartPower: 100, FinalFloor: cfg.PostReincarnationTargetFloor, Ticks: 49},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "should exceed first run start power",
		},
		{
			name:        "target not reached",
			metrics:     cycleMetrics{StartPower: 110, FinalFloor: cfg.PostReincarnationTargetFloor - 1, Ticks: 49},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "post-reincarnation loop reached floor",
		},
		{
			name:        "slower than first loop",
			metrics:     cycleMetrics{StartPower: 110, FinalFloor: cfg.PostReincarnationTargetFloor, Ticks: 51},
			firstPower:  100,
			firstTicks:  50,
			spentDamage: 1,
			want:        "should not exceed first run ticks",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			assertFailureContains(t, checkPostReincarnationLoop(tt.metrics, cfg, tt.firstPower, tt.firstTicks, tt.spentDamage), tt.want)
		})
	}
}

func TestBalanceConfigOverridesDefaults(t *testing.T) {
	cfg := defaultBalanceConfig()
	cfg.EarlyTicks = tickRange{Min: 10, Max: 11}
	cfg.ReincarnationSoulsPerCycle = 3
	cfg.RequireDeepSpecialAffix = false

	earlyMetrics := cycleMetrics{
		Ticks:      9,
		FinalFloor: cfg.EarlyTargetFloor,
		LootCount:  cfg.EarlyMinLoot,
		StartPower: 100,
		FinalPower: 120,
	}
	assertFailureContains(t, checkEarlyCycle(earlyMetrics, cfg), "10..11")

	if failures := checkReincarnationSouls(6, 2, cfg); len(failures) != 0 {
		t.Fatalf("custom souls-per-cycle got failures: %v", failures)
	}

	deepMetrics := cycleMetrics{
		FinalFloor:          cfg.DeepTargetFloor,
		Ticks:               cfg.DeepTicks.Min,
		AffixCategoryCounts: map[data.AffixCategory]int{},
	}
	if failures := checkDeepRun(deepMetrics, cfg); len(failures) != 0 {
		t.Fatalf("disabled special-affix rule got failures: %v", failures)
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
