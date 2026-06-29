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
	TypeLogin         = "login"          // 请求：登录
	TypeSync          = "sync"           // 推送：全量同步
	TypeLoot          = "loot"           // 推送：掉落装备
	TypeFloor         = "floor"          // 推送：层数推进
	TypeEquip         = "equip"          // 请求：穿戴
	TypeUnequip       = "unequip"        // 请求：卸下
	TypeBag           = "bag"            // 推送：背包全量
	TypePower         = "power"          // 推送：当前战力
	TypeDecompose     = "decompose"      // 请求：分解
	TypeCompose       = "compose"        // 请求：合成
	TypeReforge       = "reforge"        // 请求：重铸
	TypeUpgrade       = "upgrade"        // 请求：强化
	TypeMaterials     = "materials"      // 推送：材料库存
	TypeCraftResult   = "craft_result"   // 推送：养成操作结果
	TypeOfflineResult = "offline_result" // 推送：离线结算结果
	TypeReincarn      = "reincarn"       // 请求：转生
	TypeTalentUp      = "talent_up"      // 请求：天赋升级
	TypeTalents       = "talents"        // 推送：天赋状态
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

// EquipRequest 穿戴请求体。
type EquipRequest struct {
	UID string `json:"uid"`
}

// UnequipRequest 卸下请求体。
type UnequipRequest struct {
	Slot int `json:"slot"`
}

// BagData 背包全量推送。
type BagData struct {
	Items    []EquipmentDTO `json:"items"`
	Equipped []EquipmentDTO `json:"equipped"`
}

// EquipmentDTO 装备传输对象（背包与已穿戴通用）。
type EquipmentDTO struct {
	UID     string     `json:"uid"`
	BaseID  string     `json:"base_id"`
	Name    string     `json:"name"`
	Slot    int        `json:"slot"`
	Rarity  int        `json:"rarity"`
	Upgrade int        `json:"upgrade"`
	Affixes []AffixDTO `json:"affixes"`
}

// PowerData 战力推送。
type PowerData struct {
	Power float64 `json:"power"`
}

// DecomposeRequest 分解请求体。
type DecomposeRequest struct {
	UID string `json:"uid"`
}

// ComposeRequest 合成请求体。
type ComposeRequest struct {
	Slot int `json:"slot"`
}

// ReforgeRequest 重铸请求体。
type ReforgeRequest struct {
	UID string `json:"uid"`
}

// UpgradeRequest 强化请求体。
type UpgradeRequest struct {
	UID string `json:"uid"`
}

// MaterialKV 材料键值对。
type MaterialKV struct {
	Key string `json:"k"`
	Val int    `json:"v"`
}

// MaterialsData 材料库存推送（数组格式，客户端 JsonUtility 可解析）。
type MaterialsData struct {
	Materials []MaterialKV `json:"materials"`
}

// TalentKV 天赋键值对。
type TalentKV struct {
	Name  string `json:"name"`
	Level int    `json:"level"`
}

// TalentsData 天赋状态推送（数组格式，客户端 JsonUtility 可解析）。
type TalentsData struct {
	Souls       int        `json:"souls"`
	MaxFloor    int        `json:"max_floor"`
	CanReincarn bool       `json:"can_reincarn"`
	Talents     []TalentKV `json:"talents"`
}

// CraftResult 养成操作结果（分解/合成/重铸/强化通用响应推送）。
type CraftResult struct {
	OK      bool   `json:"ok"`
	Msg     string `json:"msg"`
	UID     string `json:"uid,omitempty"`
	Upgrade int    `json:"upgrade,omitempty"`
}

// OfflineResultData 离线结算结果推送。
type OfflineResultData struct {
	DurationSeconds int `json:"duration_seconds"` // 结算时长（秒）
	TicksSimulated  int `json:"ticks_simulated"`  // 模拟 tick 数
	LootCount       int `json:"loot_count"`       // 掉落数
	FloorsAdvanced  int `json:"floors_advanced"`  // 推进层数
}

// TalentUpRequest 天赋升级请求体。
type TalentUpRequest struct {
	Name string `json:"name"`
}
