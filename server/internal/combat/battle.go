package combat

// BattleResult 单次战斗结算结果。
type BattleResult struct {
	Win          bool    // 是否胜利
	PlayerPower  float64 // 玩家战力
	MonsterPower float64 // 怪物战力
}

// Battle 战斗结算：玩家战力严格大于怪物战力则胜。
func Battle(playerPower, monsterPower float64) BattleResult {
	return BattleResult{
		Win:          playerPower > monsterPower,
		PlayerPower:  playerPower,
		MonsterPower: monsterPower,
	}
}
