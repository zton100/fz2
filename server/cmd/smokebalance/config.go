package main

type tickRange struct {
	Min int
	Max int
}

type balanceConfig struct {
	EarlyTargetFloor                     int
	EarlyTicks                           tickRange
	EarlyMinLoot                         int
	ReincarnationSoulsPerCycle           int
	LongTargetFloor                      int
	LongTicks                            tickRange
	LongMinBossReward                    int
	DeepStartFloor                       int
	DeepTargetFloor                      int
	DeepTicks                            tickRange
	RequireDeepSpecialAffix              bool
	LateStartFloor                       int
	LateTargetFloor                      int
	LateTicks                            tickRange
	LateMinArtifactDrops                 int
	EndgameStartFloor                    int
	EndgameTargetFloor                   int
	EndgameTicks                         tickRange
	EndgameMinArtifactDrops              int
	PostReincarnationTargetFloor         int
	RequirePostReincarnationDamageTalent bool
}

func defaultBalanceConfig() balanceConfig {
	return balanceConfig{
		EarlyTargetFloor:                     10,
		EarlyTicks:                           tickRange{Min: 5, Max: 25},
		EarlyMinLoot:                         8,
		ReincarnationSoulsPerCycle:           2,
		LongTargetFloor:                      30,
		LongTicks:                            tickRange{Min: 25, Max: 400},
		LongMinBossReward:                    180,
		DeepStartFloor:                       30,
		DeepTargetFloor:                      50,
		DeepTicks:                            tickRange{Min: 15, Max: 800},
		RequireDeepSpecialAffix:              true,
		LateStartFloor:                       50,
		LateTargetFloor:                      80,
		LateTicks:                            tickRange{Min: 25, Max: 1500},
		LateMinArtifactDrops:                 1,
		EndgameStartFloor:                    80,
		EndgameTargetFloor:                   120,
		EndgameTicks:                         tickRange{Min: 35, Max: 3000},
		EndgameMinArtifactDrops:              2,
		PostReincarnationTargetFloor:         120,
		RequirePostReincarnationDamageTalent: true,
	}
}
