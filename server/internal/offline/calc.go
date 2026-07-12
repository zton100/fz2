package offline

import (
	"time"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/dungeon"
	"equipment-idle-server/internal/loot"
	"equipment-idle-server/internal/model"
)

// MaxOfflineDuration 离线收益上限 8 小时。
const MaxOfflineDuration = 8 * time.Hour

// TickInterval 离线模拟的 tick 间隔，与在线一致 2 秒。
const TickInterval = 2 * time.Second

// OfflineResult 离线结算结果。
type OfflineResult struct {
	Duration       time.Duration // 实际结算的离线时长（封顶后）
	TicksSimulated int           // 模拟的 tick 数（含 offline_gain 加成）
	LootCount      int           // 掉落装备数
	FloorsAdvanced int           // 推进层数
}

// Calc 离线结算：按玩家战力模拟离线期间的战斗，掉落装备入背包。
// powerFn 计算战力（传 nil 用统一函数 reincarnation.ComputePlayerPower，含 damage 天赋）。
// offlineBonus 是 offline_gain 天赋加成（0.0=无加成），用于增加模拟 tick 数。
// dropBonus/qualityFloor 是 drop/quality 天赋，接入掉落权重与稀有度下限。
func Calc(
	p *model.Player,
	powerFn func(*model.Player) float64,
	drop *loot.DropTable,
	rawDuration time.Duration,
	offlineBonus float64,
	dropBonus float64,
	qualityFloor data.Rarity,
) OfflineResult {
	if rawDuration > MaxOfflineDuration {
		rawDuration = MaxOfflineDuration
	}
	if rawDuration <= 0 {
		return OfflineResult{}
	}
	// 基础 tick 数 + offline_gain 加成
	baseTicks := int(rawDuration / TickInterval)
	ticks := int(float64(baseTicks) * (1.0 + offlineBonus))
	result := OfflineResult{Duration: rawDuration, TicksSimulated: ticks}
	runner := dungeon.NewRunnerWithDropModifier(p, powerFn, drop, loot.DropModifier{
		DropBonus:    dropBonus,
		QualityFloor: qualityFloor,
	})
	runner.LootCallback = func(*model.Equipment) {
		result.LootCount++
	}

	for i := 0; i < ticks; i++ {
		outcome := runner.Tick()
		if outcome.FloorAdvanced {
			result.FloorsAdvanced++
		}
	}
	return result
}
