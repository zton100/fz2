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
	FrontierStartFloor                   int
	FrontierTargetFloor                  int
	FrontierTicks                        tickRange
	FrontierMinArtifactDrops             int
	FrontierMinMatchedUpgradeDrops       int
	FrontierMinTransfers                 int
	PostReincarnationTargetFloor         int
	RequirePostReincarnationDamageTalent bool
	ArtifactDistributionSeeds            []int64
	EndgameArtifactDistribution          artifactDistributionTarget
	FrontierArtifactDistribution         artifactDistributionTarget
}

type artifactDistributionTarget struct {
	MinTotal         int
	MinSeedsWithDrop int
}

func defaultBalanceConfig() balanceConfig {
	return balanceConfig{
		EarlyTargetFloor:                     10,
		EarlyTicks:                           tickRange{Min: 30, Max: 60},
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
		FrontierStartFloor:                   120,
		FrontierTargetFloor:                  160,
		FrontierTicks:                        tickRange{Min: 35, Max: 5000},
		FrontierMinArtifactDrops:             2,
		FrontierMinMatchedUpgradeDrops:       1,
		FrontierMinTransfers:                 1,
		PostReincarnationTargetFloor:         160,
		RequirePostReincarnationDamageTalent: true,
		ArtifactDistributionSeeds:            []int64{84, 126, 168, 210, 252},
		EndgameArtifactDistribution: artifactDistributionTarget{
			MinTotal:         8,
			MinSeedsWithDrop: 4,
		},
		FrontierArtifactDistribution: artifactDistributionTarget{
			MinTotal:         6,
			MinSeedsWithDrop: 3,
		},
	}
}
