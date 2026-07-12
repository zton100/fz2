package dungeon

import (
	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

// PowerFunc 计算玩家战力的函数（默认用 reincarnation.ComputePlayerPower，含 damage 天赋）。
type PowerFunc func(*model.Player) float64

// EncounterKind identifies the enemy currently blocking floor progression.
type EncounterKind string

const (
	EncounterMinion   EncounterKind = "minion"
	EncounterGuardian EncounterKind = "guardian"
	EncounterBoss     EncounterKind = "boss"
	MinionsPerFloor                 = 3
)

// TickResult describes the observable outcome of one combat tick.
type TickResult struct {
	Win               bool
	EnemyKind         EncounterKind
	EnemyFamily       string
	EnemyElement      string
	Floor             int
	PlayerPower       float64
	EnemyPower        float64
	EnemyResistances  map[data.AffixType]float64
	MinionsKilled     int
	MinionsTotal      int
	FloorAdvanced     bool
	PlayerStartHP     float64
	EnemyStartHP      float64
	PlayerStartShield float64
	EnemyStartShield  float64
	PlayerEndHP       float64
	EnemyEndHP        float64
	PlayerEndShield   float64
	EnemyEndShield    float64
	Events            []combat.HitEvent
}

// Runner 单玩家地下城推进器。
type Runner struct {
	player  *model.Player
	powerFn PowerFunc
	drop    *loot.DropTable
	dropMod *loot.DropModifier
	// LootCallback 每次掉落时回调（用于 ws 层 push 给客户端）。
	LootCallback func(eq *model.Equipment)
	// FloorCallback 每次推进层数时回调。
	FloorCallback func(newFloor int)
	// BossRewardCallback 首次击败历史最高 Boss 层时回调。
	BossRewardCallback func(floor int, amount int)
	// CombatCallback emits the authoritative tick before loot/floor callbacks.
	CombatCallback func(result TickResult)
}

// NewRunner 创建推进器。powerFn 传 nil 时默认用统一战力函数（含 damage 天赋）。
func NewRunner(player *model.Player, powerFn PowerFunc, drop *loot.DropTable) *Runner {
	if powerFn == nil {
		powerFn = reincarnation.ComputePlayerPower
	}
	return &Runner{player: player, powerFn: powerFn, drop: drop}
}

// NewRunnerWithDropModifier creates a runner with a fixed drop modifier. It is
// used by offline settlement, which snapshots talent bonuses at login time.
func NewRunnerWithDropModifier(player *model.Player, powerFn PowerFunc, drop *loot.DropTable, mod loot.DropModifier) *Runner {
	runner := NewRunner(player, powerFn, drop)
	runner.dropMod = &mod
	return runner
}

// Tick executes one combat tick. Players clear minions before fighting the
// floor guardian; every fifth guardian is a boss.
func (r *Runner) Tick() TickResult {
	playerPower := r.powerFn(r.player)
	monster := data.MonsterAt(r.player.Floor)
	kind := EncounterGuardian
	monsterPower := monster.Power
	if r.player.FloorKills < MinionsPerFloor {
		kind = EncounterMinion
		monsterPower *= 0.5
	} else if monster.IsBoss {
		kind = EncounterBoss
	}
	outcome := TickResult{
		EnemyKind:        kind,
		EnemyFamily:      monster.Family,
		EnemyElement:     monster.Element,
		Floor:            r.player.Floor,
		PlayerPower:      playerPower,
		EnemyPower:       monsterPower,
		EnemyResistances: copyResistances(monster.Resistances),
		MinionsKilled:    r.player.FloorKills,
		MinionsTotal:     MinionsPerFloor,
	}
	result := combat.ResolveBattle(combat.BattleInput{
		PlayerPower:      playerPower,
		EnemyPower:       monsterPower,
		PlayerStats:      combat.AggregateStats(r.player.EquippedList()),
		EnemyResistances: monster.Resistances,
		Artifacts:        equippedArtifacts(r.player),
	})
	outcome.Win = result.Win
	outcome.PlayerStartHP = result.PlayerStartHP
	outcome.EnemyStartHP = result.EnemyStartHP
	outcome.PlayerStartShield = result.PlayerStartShield
	outcome.EnemyStartShield = result.EnemyStartShield
	outcome.PlayerEndHP = result.PlayerEndHP
	outcome.EnemyEndHP = result.EnemyEndHP
	outcome.PlayerEndShield = result.PlayerEndShield
	outcome.EnemyEndShield = result.EnemyEndShield
	outcome.Events = result.Events
	if !result.Win {
		r.emitCombat(outcome)
		return outcome
	}
	if kind == EncounterMinion {
		r.player.FloorKills++
		outcome.MinionsKilled = r.player.FloorKills
		r.emitCombat(outcome)
		return outcome
	}
	outcome.FloorAdvanced = true
	r.emitCombat(outcome)
	// 胜利：掉落装备（使用 drop/quality 天赋加成）
	mod := r.dropMod
	if mod == nil {
		mod = &loot.DropModifier{
			DropBonus:    reincarnation.DropBonus(r.player),
			QualityFloor: reincarnation.QualityFloor(r.player),
		}
	}
	eq := r.drop.DropRandomSlotModified(r.player.Floor, *mod)
	if eq != nil {
		r.player.AddEquipment(eq)
		if r.LootCallback != nil {
			r.LootCallback(eq)
		}
	}
	if baseReward := BossFirstClearReward(r.player.Floor, r.player.MaxFloor); baseReward > 0 {
		reward := reincarnation.ApplyBossReward(r.player, baseReward)
		r.player.AddMaterial(data.MatBase, reward)
		if r.BossRewardCallback != nil {
			r.BossRewardCallback(r.player.Floor, reward)
		}
	}
	// 推进下一层（同时更新 MaxFloor）
	r.player.FloorKills = 0
	reincarnation.AdvanceFloor(r.player)
	if r.FloorCallback != nil {
		r.FloorCallback(r.player.Floor)
	}
	return outcome
}

func equippedArtifacts(player *model.Player) []data.ArtifactDefinition {
	var out []data.ArtifactDefinition
	for _, eq := range player.EquippedList() {
		if def, ok := data.ArtifactByID(eq.ArtifactID); ok {
			out = append(out, def)
		}
	}
	return out
}

func copyResistances(src map[data.AffixType]float64) map[data.AffixType]float64 {
	if len(src) == 0 {
		return nil
	}
	dst := make(map[data.AffixType]float64, len(src))
	for affix, value := range src {
		dst[affix] = value
	}
	return dst
}

func (r *Runner) emitCombat(result TickResult) {
	if r.CombatCallback != nil {
		r.CombatCallback(result)
	}
}

// BossFirstClearReward returns the one-time material reward for clearing a boss
// at the player's current highest reached floor. MaxFloor tracks the highest
// reached floor, so floor == maxFloor is still the first clear attempt for that
// floor; old boss reclears have floor < maxFloor.
func BossFirstClearReward(floor int, maxFloor int) int {
	if floor <= 0 || floor%5 != 0 || floor < maxFloor {
		return 0
	}
	return floor * 3
}
