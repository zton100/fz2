package reincarnation

import (
	"errors"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

// 转生条件：达到第 10 层。
const ReincarnateFloorReq = 10

// 天赋配置：名称 -> 最大等级。
var talentMaxLevel = map[string]int{
	"damage":       10,
	"quality":      3,
	"drop":         10,
	"offline_gain": 5,
}

// CanReincarnate 判断是否满足转生条件。
func CanReincarnate(p *model.Player) bool {
	return p.Floor >= ReincarnateFloorReq
}

// Reincarnate 执行转生：更新 MaxFloor、加魂点、重置进度。
func Reincarnate(p *model.Player) error {
	if !CanReincarnate(p) {
		return errors.New("floor not enough to reincarnate")
	}
	// 更新历史最高层数
	if p.Floor > p.MaxFloor {
		p.MaxFloor = p.Floor
	}
	// 魂点 = floor(MaxFloor / 5)
	p.Souls += p.MaxFloor / 5
	// 重置进度
	p.Floor = 1
	p.EquipBag = []*model.Equipment{}
	p.Equipped = map[data.Slot]*model.Equipment{}
	p.Materials = map[data.MaterialType]int{}
	return nil
}

// UpgradeTalent 升级一个天赋，消耗 1 魂点。
func UpgradeTalent(p *model.Player, name string) error {
	maxLv, ok := talentMaxLevel[name]
	if !ok {
		return errors.New("unknown talent: " + name)
	}
	if p.Souls < 1 {
		return errors.New("not enough souls")
	}
	if p.Talents[name] >= maxLv {
		return errors.New("talent at max level: " + name)
	}
	p.Souls--
	p.Talents[name]++
	return nil
}

// DamageBonus 全局伤害加成（0.05/级）。
func DamageBonus(p *model.Player) float64 {
	return 0.05 * float64(p.Talents["damage"])
}

// DropBonus 掉落率加成（0.03/级）。
func DropBonus(p *model.Player) float64 {
	return 0.03 * float64(p.Talents["drop"])
}

// OfflineGainBonus 离线收益加成（0.10/级）。
func OfflineGainBonus(p *model.Player) float64 {
	return 0.10 * float64(p.Talents["offline_gain"])
}

// QualityFloor 初始装备品质下限（稀有度 +quality 级档）。
func QualityFloor(p *model.Player) data.Rarity {
	return data.Rarity(p.Talents["quality"])
}
