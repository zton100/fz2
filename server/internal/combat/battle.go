package combat

import (
	"math"

	"equipment-idle-server/internal/data"
)

const (
	ActorPlayer = "player"
	ActorEnemy  = "enemy"

	EventHit    = "hit"
	EventShield = "shield"
)

// BattleInput describes one authoritative encounter settlement.
type BattleInput struct {
	PlayerPower float64
	EnemyPower  float64
	PlayerStats Stats
}

// HitEvent is one animation-ready combat event emitted by the server.
type HitEvent struct {
	Index        int     `json:"index"`
	Actor        string  `json:"actor"`
	Kind         string  `json:"kind"`
	Damage       float64 `json:"damage,omitempty"`
	Critical     bool    `json:"critical,omitempty"`
	PlayerHP     float64 `json:"player_hp"`
	EnemyHP      float64 `json:"enemy_hp"`
	PlayerShield float64 `json:"player_shield,omitempty"`
	EnemyShield  float64 `json:"enemy_shield,omitempty"`
}

// BattleResult 战斗结果。
type BattleResult struct {
	Win               bool
	PlayerStartHP     float64
	EnemyStartHP      float64
	PlayerStartShield float64
	EnemyStartShield  float64
	PlayerEndHP       float64
	EnemyEndHP        float64
	PlayerEndShield   float64
	EnemyEndShield    float64
	Events            []HitEvent
}

// Battle 执行一次战斗结算。
func Battle(playerPower, monsterPower float64) BattleResult {
	return ResolveBattle(BattleInput{PlayerPower: playerPower, EnemyPower: monsterPower})
}

// ResolveBattle resolves a full encounter in one server tick while preserving
// a hit timeline for client animation.
func ResolveBattle(input BattleInput) BattleResult {
	playerPower := math.Max(1, input.PlayerPower)
	enemyPower := math.Max(1, input.EnemyPower)
	stats := input.PlayerStats
	playerHP := math.Max(1, 100+playerPower*0.8+stats[data.ATMaxHP]+stats[data.ATVitality]*5)
	enemyHP := math.Max(1, 100+enemyPower*0.8)
	playerShield := math.Max(0, stats[data.ATShield])
	enemyShield := 0.0
	playerDamage := math.Max(1, playerPower*0.35+elementalDamage(stats)*0.2)
	enemyDamage := math.Max(1, enemyPower*0.30)
	if armor := stats[data.ATArmor]; armor > 0 {
		enemyDamage /= 1 + armor/120
	}
	if evasion := clamp(stats[data.ATEvasion], 0, 0.65); evasion > 0 {
		enemyDamage *= 1 - evasion*0.45
	}
	critRate := clamp(stats[data.ATCritRate], 0, 1)
	critDamage := math.Max(0, stats[data.ATCritDamage])
	attackSpeed := math.Max(0, stats[data.ATAttackSpeed])
	playerHitsPerRound := 1
	if attackSpeed >= 0.35 {
		playerHitsPerRound = 2
	}
	if attackSpeed >= 0.90 {
		playerHitsPerRound = 3
	}
	result := BattleResult{
		PlayerStartHP:     round2(playerHP),
		EnemyStartHP:      round2(enemyHP),
		PlayerStartShield: round2(playerShield),
		EnemyStartShield:  enemyShield,
	}
	events := make([]HitEvent, 0, 12)
	nextEvent := func(actor string, damage float64, critical bool) {
		events = append(events, HitEvent{
			Index:        len(events) + 1,
			Actor:        actor,
			Kind:         EventHit,
			Damage:       round2(damage),
			Critical:     critical,
			PlayerHP:     round2(math.Max(0, playerHP)),
			EnemyHP:      round2(math.Max(0, enemyHP)),
			PlayerShield: round2(math.Max(0, playerShield)),
			EnemyShield:  round2(math.Max(0, enemyShield)),
		})
	}
	for round := 1; round <= 40 && playerHP > 0 && enemyHP > 0; round++ {
		for hit := 1; hit <= playerHitsPerRound && enemyHP > 0; hit++ {
			critical := shouldCrit(round, hit, critRate)
			damage := playerDamage
			if critical {
				damage *= 1 + critDamage
			}
			enemyHP -= damage
			nextEvent(ActorPlayer, damage, critical)
		}
		if enemyHP <= 0 {
			break
		}
		damage := enemyDamage
		if playerShield > 0 {
			absorbed := math.Min(playerShield, damage)
			playerShield -= absorbed
			damage -= absorbed
		}
		playerHP -= damage
		nextEvent(ActorEnemy, damage, false)
	}
	if enemyHP <= 0 {
		enemyHP = 0
	}
	if playerHP <= 0 {
		playerHP = 0
	}
	result.Win = enemyHP <= 0 && playerHP > 0
	if input.PlayerPower >= input.EnemyPower && !result.Win {
		result.Win = true
		enemyHP = 0
	}
	result.PlayerEndHP = round2(playerHP)
	result.EnemyEndHP = round2(enemyHP)
	result.PlayerEndShield = round2(playerShield)
	result.EnemyEndShield = round2(enemyShield)
	result.Events = events
	return result
}

func elementalDamage(stats Stats) float64 {
	return stats[data.ATFireDmg] + stats[data.ATColdDmg] + stats[data.ATLightningDmg]
}

func shouldCrit(round int, hit int, critRate float64) bool {
	if critRate <= 0 {
		return false
	}
	if critRate >= 1 {
		return true
	}
	interval := int(math.Round(1 / critRate))
	if interval < 2 {
		interval = 2
	}
	return ((round-1)*3+hit)%interval == 0
}

func clamp(v float64, min float64, max float64) float64 {
	if v < min {
		return min
	}
	if v > max {
		return max
	}
	return v
}

func round2(v float64) float64 {
	return math.Round(v*100) / 100
}
