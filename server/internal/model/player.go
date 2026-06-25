package model

import "equipment-idle-server/internal/data"

// Player 玩家领域模型。
type Player struct {
	Account   string                   // 账号名
	Floor     int                      // 当前层数
	Souls     int                      // 魂点
	Inventory []string                 // 背包物品 ID 占位（保留兼容阶段0同步）
	EquipBag  []*Equipment             // 装备背包（掉落/未穿戴的装备）
	Equipped  map[data.Slot]*Equipment // 已穿戴装备，按槽位
}

// NewPlayer 创建新玩家，默认第 1 层、0 魂、空背包。
func NewPlayer(account string) *Player {
	return &Player{
		Account:   account,
		Floor:     1,
		Souls:     0,
		Inventory: []string{},
		EquipBag:  []*Equipment{},
		Equipped:  map[data.Slot]*Equipment{},
	}
}

// AddEquipment 把掉落的装备加入背包。
func (p *Player) AddEquipment(eq *Equipment) {
	if eq == nil {
		return
	}
	p.EquipBag = append(p.EquipBag, eq)
}

// EquippedList 返回所有已穿戴装备（非空槽位），用于战力计算。
func (p *Player) EquippedList() []*Equipment {
	out := []*Equipment{}
	for _, eq := range p.Equipped {
		if eq != nil {
			out = append(out, eq)
		}
	}
	return out
}
