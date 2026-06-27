package combat

// Battle 执行一次战斗结算。
func Battle(playerPower, monsterPower float64) BattleResult {
	return BattleResult{Win: playerPower >= monsterPower}
}

// BattleResult 战斗结果。
type BattleResult struct {
	Win bool
}
