package combat

import (
	"math"

	"equipment-idle-server/internal/data"
)

const (
	ActorPlayer = "player"
	ActorEnemy  = "enemy"

	EventHit     = "hit"
	EventShield  = "shield"
	EventEcho    = "echo"
	EventExecute = "execute"
	EventHeal    = "heal"
)

// BattleInput describes one authoritative encounter settlement.
type BattleInput struct {
	PlayerPower      float64
	EnemyPower       float64
	PlayerStats      Stats
	EnemyResistances map[data.AffixType]float64
	Artifacts        []data.ArtifactDefinition
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
	playerMaxHP := playerHP
	enemyStartHP := enemyHP
	if openingShield := artifactValue(input.Artifacts, data.ArtifactOpeningShield); openingShield > 0 {
		playerShield += playerMaxHP * openingShield
	}
	playerDamage := math.Max(1, playerPower*0.35+elementalDamage(stats, input.EnemyResistances)*0.2)
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
	appendEvent := func(actor string, kind string, damage float64, critical bool) {
		events = append(events, HitEvent{
			Index:        len(events) + 1,
			Actor:        actor,
			Kind:         kind,
			Damage:       round2(damage),
			Critical:     critical,
			PlayerHP:     round2(math.Max(0, playerHP)),
			EnemyHP:      round2(math.Max(0, enemyHP)),
			PlayerShield: round2(math.Max(0, playerShield)),
			EnemyShield:  round2(math.Max(0, enemyShield)),
		})
	}
	if artifactValue(input.Artifacts, data.ArtifactOpeningShield) > 0 {
		appendEvent(ActorPlayer, EventShield, 0, false)
	}
	playerHitCount := 0
	for round := 1; round <= 40 && playerHP > 0 && enemyHP > 0; round++ {
		for hit := 1; hit <= playerHitsPerRound && enemyHP > 0; hit++ {
			critical := shouldCrit(round, hit, critRate)
			damage := playerDamage
			if critical {
				damage *= 1 + critDamage
			}
			enemyHP -= damage
			playerHitCount++
			appendEvent(ActorPlayer, EventHit, damage, critical)
			if healValue := artifactValue(input.Artifacts, data.ArtifactHitHeal); healValue > 0 {
				heal := damage * healValue
				playerHP = math.Min(playerMaxHP, playerHP+heal)
				appendEvent(ActorPlayer, EventHeal, heal, false)
			}
			if echoValue := artifactValue(input.Artifacts, data.ArtifactEchoStrike); echoValue > 0 && playerHitCount%3 == 0 && enemyHP > 0 {
				echoDamage := playerDamage * echoValue
				enemyHP -= echoDamage
				appendEvent(ActorPlayer, EventEcho, echoDamage, false)
			}
			if executeValue := artifactValue(input.Artifacts, data.ArtifactExecute); executeValue > 0 && enemyHP > 0 && enemyHP/enemyStartHP <= executeValue {
				executeDamage := enemyHP
				enemyHP = 0
				appendEvent(ActorPlayer, EventExecute, executeDamage, false)
			}
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
		appendEvent(ActorEnemy, EventHit, damage, false)
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

func artifactValue(artifacts []data.ArtifactDefinition, trigger string) float64 {
	for _, artifact := range artifacts {
		if artifact.Trigger == trigger {
			return artifact.Value
		}
	}
	return 0
}

func elementalDamage(stats Stats, resistances map[data.AffixType]float64) float64 {
	return elementalDamageFor(stats, resistances, data.ATFireDmg) +
		elementalDamageFor(stats, resistances, data.ATColdDmg) +
		elementalDamageFor(stats, resistances, data.ATLightningDmg)
}

func elementalDamageFor(stats Stats, resistances map[data.AffixType]float64, affix data.AffixType) float64 {
	resistance := clamp(resistances[affix], -0.75, 0.85)
	return stats[affix] * (1 - resistance)
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
