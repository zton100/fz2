package model

import "equipment-idle-server/internal/data"

// AffixInstance 词缀实例：词缀定义 + 随机生成的具体数值。
type AffixInstance struct {
	Type  data.AffixType
	Tier  int
	Value float64
}

// Equipment 装备实例：基底 + 稀有度 + 词缀实例列表 + 强化等级。
type Equipment struct {
	UID       string                 // 全局唯一 ID（生成时分配）
	BaseID    string                 // 基底 ID
	Name      string                 // 显示名（基底名，稀有度前缀在展示层加）
	Slot      data.Slot              // 槽位
	Rarity    data.Rarity            // 稀有度
	Affixes   []AffixInstance        // 生成的词缀实例
	BaseStats map[data.AffixType]float64 // 白板基础属性（复制自基底）
	Upgrade   int                    // 强化等级 0~10
}

// AllStats 聚合白板基础属性 + 词缀数值，返回每个 AffixType 的总值。
func (e *Equipment) AllStats() map[data.AffixType]float64 {
	out := make(map[data.AffixType]float64)
	for k, v := range e.BaseStats {
		out[k] += v
	}
	for _, a := range e.Affixes {
		out[a.Type] += a.Value
	}
	return out
}
