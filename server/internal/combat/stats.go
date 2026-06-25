package combat

import (
	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Stats 玩家总属性：AffixType -> 总值。
type Stats map[data.AffixType]float64

// AggregateStats 把多件装备的 AllStats 聚合为玩家总属性。
func AggregateStats(eqs []*model.Equipment) Stats {
	out := Stats{}
	for _, eq := range eqs {
		if eq == nil {
			continue
		}
		for k, v := range eq.AllStats() {
			out[k] += v
		}
	}
	return out
}
