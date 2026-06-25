package loot

import (
	"fmt"
	"math/rand"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// Generator 装备生成器。注入 *rand.Rand 便于测试确定性。
type Generator struct {
	rng    *rand.Rand
	pool   []data.AffixDef
	uidSeq int
}

// NewGenerator 创建生成器。
func NewGenerator(rng *rand.Rand) *Generator {
	return &Generator{
		rng:  rng,
		pool: data.BuildAffixPool(),
	}
}

// Generate 按槽位与稀有度生成一件装备实例。
func (g *Generator) Generate(slot data.Slot, rarity data.Rarity) *model.Equipment {
	base := data.BaseBySlot(slot)
	eq := &model.Equipment{
		UID:       g.nextUID(),
		BaseID:    base.ID,
		Name:      base.Name,
		Slot:      slot,
		Rarity:    rarity,
		BaseStats: copyStats(base.BaseStats),
		Upgrade:   0,
	}
	rule := data.RarityAffixCount[rarity]
	prefixes := data.AffixesByPosition(g.pool, data.PosPrefix)
	suffixes := data.AffixesByPosition(g.pool, data.PosSuffix)

	chosenTypes := map[data.AffixType]bool{}
	for i := 0; i < rule.Prefix && len(prefixes) > 0; i++ {
		a := g.pickAffix(prefixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	for i := 0; i < rule.Suffix && len(suffixes) > 0; i++ {
		a := g.pickAffix(suffixes, chosenTypes)
		if a != nil {
			eq.Affixes = append(eq.Affixes, g.rollValue(a))
			chosenTypes[a.Type] = true
		}
	}
	return eq
}

// pickAffix 从候选词缀中随机选一个（避开已选类型），返回 AffixDef 指针。
func (g *Generator) pickAffix(candidates []data.AffixDef, chosen map[data.AffixType]bool) *data.AffixDef {
	var avail []data.AffixDef
	for _, c := range candidates {
		if !chosen[c.Type] {
			avail = append(avail, c)
		}
	}
	if len(avail) == 0 {
		return nil
	}
	idx := g.rng.Intn(len(avail))
	picked := avail[idx]
	return &picked
}

// rollValue 在词缀 Min~Max 间随机生成数值。
func (g *Generator) rollValue(def *data.AffixDef) model.AffixInstance {
	val := def.Min + g.rng.Float64()*(def.Max-def.Min)
	return model.AffixInstance{
		Type:  def.Type,
		Tier:  def.Tier,
		Value: val,
	}
}

// nextUID 生成全局唯一 ID。
func (g *Generator) nextUID() string {
	g.uidSeq++
	return fmt.Sprintf("eq_%d", g.uidSeq)
}

// copyStats 复制基础属性 map，避免共享底层数据。
func copyStats(src map[data.AffixType]float64) map[data.AffixType]float64 {
	dst := make(map[data.AffixType]float64, len(src))
	for k, v := range src {
		dst[k] = v
	}
	return dst
}
