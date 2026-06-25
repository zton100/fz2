package protocol

import "encoding/json"

// Envelope 是所有消息的统一信封。
type Envelope struct {
	T    string          `json:"t"`              // 消息类型
	ID   string          `json:"id,omitempty"`   // 请求 ID，用于 req/resp 匹配；推送/同步可空
	Data json.RawMessage `json:"data,omitempty"` // 消息体，按 T 解析
}

// 消息类型常量
const (
	TypeLogin = "login" // 请求：登录
	TypeSync  = "sync"  // 推送：全量同步
	TypeLoot  = "loot"  // 推送：掉落装备
	TypeFloor = "floor" // 推送：层数推进
)

// LoginRequest 登录请求体。
type LoginRequest struct {
	Account string `json:"account"` // 临时账号名
}

// SyncData 全量同步消息体。阶段 0 最小字段，后续阶段扩充。
type SyncData struct {
	Account   string   `json:"account"`   // 账号
	Floor     int      `json:"floor"`     // 当前层数
	Souls     int      `json:"souls"`     // 魂点
	Inventory []string `json:"inventory"` // 背包物品 ID 占位
}

// LootData 掉落推送消息体。
type LootData struct {
	UID     string     `json:"uid"`     // 装备唯一 ID
	BaseID  string     `json:"base_id"` // 基底 ID
	Name    string     `json:"name"`    // 装备名
	Slot    int        `json:"slot"`    // 槽位
	Rarity  int        `json:"rarity"`  // 稀有度
	Upgrade int        `json:"upgrade"` // 强化等级
	Affixes []AffixDTO `json:"affixes"` // 词缀列表
}

// AffixDTO 词缀传输对象。
type AffixDTO struct {
	Type  string  `json:"type"`
	Tier  int     `json:"tier"`
	Value float64 `json:"value"`
}

// FloorData 层数推送消息体。
type FloorData struct {
	Floor int `json:"floor"` // 新层数
}
