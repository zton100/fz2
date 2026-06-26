package offline

import (
	"time"

	"equipment-idle-server/internal/combat"
	"equipment-idle-server/internal/data"
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
	TicksSimulated int           // 模拟的 tick 数
	LootCount      int           // 掉落装备数
	FloorsAdvanced int           // 推进层数
}

// Calc 离线结算：按玩家战力模拟离线期间的战斗，掉落装备入背包。
// powerFn 计算战力，纯逻辑可单测。
func Calc(p *model.Player, powerFn func(combat.Stats) float64, drop *loot.DropTable, rawDuration time.Duration) OfflineResult {
	if rawDuration > MaxOfflineDuration {
		rawDuration = MaxOfflineDuration
	}
	if rawDuration <= 0 {
		return OfflineResult{}
	}
	ticks := int(rawDuration / TickInterval)
	result := OfflineResult{Duration: rawDuration, TicksSimulated: ticks}

	for i := 0; i < ticks; i++ {
		stats := combat.AggregateStats(p.EquippedList())
		playerPower := powerFn(stats)
		monster := data.MonsterAt(p.Floor)
		if playerPower <= monster.Power {
			continue // 打不过，停留
		}
		eq := drop.DropRandomSlot(p.Floor)
		if eq != nil {
			p.AddEquipment(eq)
			result.LootCount++
		}
		p.Floor++
		result.FloorsAdvanced++
	}
	return result
}
