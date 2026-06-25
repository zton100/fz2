package data

// Monster 怪物定义。
type Monster struct {
	Name   string  // 怪物名
	Power  float64 // 怪物战力（强度）
	IsBoss bool    // 是否 Boss
}

// MonsterPower 按层数计算怪物强度。
// 普通层：线性增长，base + (floor-1)*step
// Boss 层（每 5 层）：强度跳升 1.8 倍
func MonsterPower(floor int) float64 {
	const base = 10.0
	const step = 8.0
	normal := base + float64(floor-1)*step
	if floor%5 == 0 {
		return normal * 1.8 // Boss 跳升
	}
	return normal
}

// MonsterAt 生成某层的怪物。
func MonsterAt(floor int) Monster {
	isBoss := floor%5 == 0
	name := "史莱姆"
	if isBoss {
		name = "守层Boss"
	}
	return Monster{
		Name:   name,
		Power:  MonsterPower(floor),
		IsBoss: isBoss,
	}
}
