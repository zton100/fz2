package data

import (
	"math"

	"equipment-idle-server/internal/locale"
)

// Monster 怪物定义。
type Monster struct {
	Name   string
	Power  float64
	IsBoss bool
}

// MonsterPower 按层数计算怪物强度（非 Boss 基础值）。
func MonsterPower(floor int) float64 {
	if floor <= 0 {
		return 3
	}
	var normal float64
	if floor <= 20 {
		const base = 3.0
		const step = 5.0
		normal = base + float64(floor-1)*step
	} else if floor <= 80 {
		const baseAt20 = 98.0
		normal = baseAt20 * math.Pow(1.05, float64(floor-20))
	} else {
		const baseAt20 = 98.0
		baseAt80 := baseAt20 * math.Pow(1.05, 60)
		normal = baseAt80 * math.Pow(1.055, float64(floor-80))
	}
	if floor%5 == 0 {
		return normal * 1.2
	}
	return normal
}

// MonsterAt 生成某层的怪物（使用当前 locale 的名称）。
func MonsterAt(floor int) Monster {
	l := locale.Current()
	isBoss := floor%5 == 0
	name := l.MonsterNormal
	if isBoss {
		name = l.MonsterBoss
	}
	return Monster{
		Name:   name,
		Power:  MonsterPower(floor),
		IsBoss: isBoss,
	}
}
