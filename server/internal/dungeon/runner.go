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

// Runner 单玩家地下城推进器。
type Runner struct {
	player  *model.Player
	powerFn PowerFunc
	drop    *loot.DropTable
	// LootCallback 每次掉落时回调（用于 ws 层 push 给客户端）。
	LootCallback func(eq *model.Equipment)
	// FloorCallback 每次推进层数时回调。
	FloorCallback func(newFloor int)
	// BossRewardCallback 首次击败历史最高 Boss 层时回调。
	BossRewardCallback func(floor int, amount int)
}

// NewRunner 创建推进器。powerFn 传 nil 时默认用统一战力函数（含 damage 天赋）。
func NewRunner(player *model.Player, powerFn PowerFunc, drop *loot.DropTable) *Runner {
	if powerFn == nil {
		powerFn = reincarnation.ComputePlayerPower
	}
	return &Runner{player: player, powerFn: powerFn, drop: drop}
}

// Tick 执行一次战斗 tick：算战力 → 战斗 → 胜则掉落+推进 → 负则停留。
func (r *Runner) Tick() {
	playerPower := r.powerFn(r.player)
	monster := data.MonsterAt(r.player.Floor)
	result := combat.Battle(playerPower, monster.Power)
	if !result.Win {
		return
	}
	// 胜利：掉落装备（使用 drop/quality 天赋加成）
	mod := loot.DropModifier{
		DropBonus:    reincarnation.DropBonus(r.player),
		QualityFloor: reincarnation.QualityFloor(r.player),
	}
	eq := r.drop.DropRandomSlotModified(r.player.Floor, mod)
	if eq != nil {
		r.player.AddEquipment(eq)
		if r.LootCallback != nil {
			r.LootCallback(eq)
		}
	}
	if reward := BossFirstClearReward(r.player.Floor, r.player.MaxFloor); reward > 0 {
		r.player.AddMaterial(data.MatBase, reward)
		if r.BossRewardCallback != nil {
			r.BossRewardCallback(r.player.Floor, reward)
		}
	}
	// 推进下一层（同时更新 MaxFloor）
	reincarnation.AdvanceFloor(r.player)
	if r.FloorCallback != nil {
		r.FloorCallback(r.player.Floor)
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
