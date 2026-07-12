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
	TypeLogin         = "login"            // 请求：登录
	TypeSync          = "sync"             // 推送：全量同步
	TypeLoot          = "loot"             // 推送：掉落装备
	TypeFloor         = "floor"            // 推送：层数推进
	TypeEquip         = "equip"            // 请求：穿戴
	TypeUnequip       = "unequip"          // 请求：卸下
	TypeBag           = "bag"              // 推送：背包全量
	TypePower         = "power"            // 推送：当前战力
	TypeCombat        = "combat"           // 推送：战斗 tick 结果
	TypeDecompose     = "decompose"        // 请求：分解
	TypeCompose       = "compose"          // 请求：合成
	TypeReforge       = "reforge"          // 请求：重铸
	TypeUpgrade       = "upgrade"          // 请求：强化
	TypeTransferUpg   = "transfer_upgrade" // 请求：强化继承/转移
	TypeLockEquipment = "lock_equipment"   // 请求：锁定/解锁装备
	TypeMaterials     = "materials"        // 推送：材料库存
	TypeCraftResult   = "craft_result"     // 推送：养成操作结果
	TypeOfflineResult = "offline_result"   // 推送：离线结算结果
	TypeReincarn      = "reincarn"         // 请求：转生
	TypeTalentUp      = "talent_up"        // 请求：天赋升级
	TypeTalents       = "talents"          // 推送：天赋状态
)

// LoginRequest 登录请求体。
type LoginRequest struct {
	Account string `json:"account"` // 临时账号名
}

// SyncData 全量同步消息体。阶段 0 最小字段，后续阶段扩充。
type SyncData struct {
	Account              string   `json:"account"`                // 账号
	Floor                int      `json:"floor"`                  // 当前层数
	FloorKills           int      `json:"floor_kills"`            // 当前层小兵击杀数
	MinionsTotal         int      `json:"minions_total"`          // 每层小兵总数
	EquipmentDataVersion int      `json:"equipment_data_version"` // 装备基底数据版本
	LegendaryDataVersion int      `json:"legendary_data_version"` // 传奇定义数据版本
	ArtifactDataVersion  int      `json:"artifact_data_version"`  // 神器定义数据版本
	Souls                int      `json:"souls"`                  // 魂点
	Inventory            []string `json:"inventory"`              // 背包物品 ID 占位
}

// CombatData describes one authoritative combat tick for client animation.
type CombatData struct {
	Floor             int              `json:"floor"`
	EnemyKind         string           `json:"enemy_kind"`
	EnemyFamily       string           `json:"enemy_family,omitempty"`
	EnemyElement      string           `json:"enemy_element,omitempty"`
	Win               bool             `json:"win"`
	PlayerPower       float64          `json:"player_power"`
	EnemyPower        float64          `json:"enemy_power"`
	EnemyResistances  []ResistanceDTO  `json:"enemy_resistances,omitempty"`
	MinionsKilled     int              `json:"minions_killed"`
	MinionsTotal      int              `json:"minions_total"`
	FloorAdvanced     bool             `json:"floor_advanced"`
	PlayerStartHP     float64          `json:"player_start_hp,omitempty"`
	EnemyStartHP      float64          `json:"enemy_start_hp,omitempty"`
	PlayerStartShield float64          `json:"player_start_shield,omitempty"`
	EnemyStartShield  float64          `json:"enemy_start_shield,omitempty"`
	PlayerEndHP       float64          `json:"player_end_hp,omitempty"`
	EnemyEndHP        float64          `json:"enemy_end_hp,omitempty"`
	PlayerEndShield   float64          `json:"player_end_shield,omitempty"`
	EnemyEndShield    float64          `json:"enemy_end_shield,omitempty"`
	Events            []CombatEventDTO `json:"events,omitempty"`
}

// ResistanceDTO describes one elemental resistance or vulnerability.
type ResistanceDTO struct {
	Type  string  `json:"type"`
	Value float64 `json:"value"`
}

// CombatEventDTO is one server-authored animation event inside a combat tick.
type CombatEventDTO struct {
	Index        int     `json:"index"`
	Actor        string  `json:"actor"`
	Kind         string  `json:"kind"`
	Damage       float64 `json:"damage,omitempty"`
	Critical     bool    `json:"critical,omitempty"`
	PlayerHP     float64 `json:"player_hp"`
	EnemyHP      float64 `json:"enemy_hp"`
	PlayerShield float64 `json:"player_shield,omitempty"`
	EnemyShield  float64 `json:"enemy_shield,omitempty"`
}

// LootData 掉落推送消息体。
type LootData struct {
	UID                  string     `json:"uid"`     // 装备唯一 ID
	BaseID               string     `json:"base_id"` // 基底 ID
	LegendaryID          string     `json:"legendary_id,omitempty"`
	ArtifactID           string     `json:"artifact_id,omitempty"`
	LegendaryDescription string     `json:"legendary_description,omitempty"`
	ArtifactDescription  string     `json:"artifact_description,omitempty"`
	LegendaryBonuses     []AffixDTO `json:"legendary_bonuses,omitempty"`
	ArtifactBonuses      []AffixDTO `json:"artifact_bonuses,omitempty"`
	LegendaryPowerBonus  float64    `json:"legendary_power_bonus,omitempty"`
	BossRewardBonus      float64    `json:"boss_reward_bonus,omitempty"`
	ArtifactTrigger      string     `json:"artifact_trigger,omitempty"`
	ArtifactValue        float64    `json:"artifact_value,omitempty"`
	Name                 string     `json:"name"`    // 装备名
	Slot                 int        `json:"slot"`    // 槽位
	Rarity               int        `json:"rarity"`  // 稀有度
	Upgrade              int        `json:"upgrade"` // 强化等级
	Affixes              []AffixDTO `json:"affixes"` // 词缀列表
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
	UID                  string     `json:"uid"`
	BaseID               string     `json:"base_id"`
	LegendaryID          string     `json:"legendary_id,omitempty"`
	ArtifactID           string     `json:"artifact_id,omitempty"`
	LegendaryDescription string     `json:"legendary_description,omitempty"`
	ArtifactDescription  string     `json:"artifact_description,omitempty"`
	LegendaryBonuses     []AffixDTO `json:"legendary_bonuses,omitempty"`
	ArtifactBonuses      []AffixDTO `json:"artifact_bonuses,omitempty"`
	LegendaryPowerBonus  float64    `json:"legendary_power_bonus,omitempty"`
	BossRewardBonus      float64    `json:"boss_reward_bonus,omitempty"`
	ArtifactTrigger      string     `json:"artifact_trigger,omitempty"`
	ArtifactValue        float64    `json:"artifact_value,omitempty"`
	Name                 string     `json:"name"`
	Slot                 int        `json:"slot"`
	Rarity               int        `json:"rarity"`
	Upgrade              int        `json:"upgrade"`
	Locked               bool       `json:"locked"`
	PowerScore           float64    `json:"power_score"`
	PowerScoreValid      bool       `json:"power_score_valid"`
	Affixes              []AffixDTO `json:"affixes"`
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

// TransferUpgradeRequest 强化继承请求体。
type TransferUpgradeRequest struct {
	SourceUID string `json:"source_uid"`
	TargetUID string `json:"target_uid"`
}

// LockEquipmentRequest 锁定/解锁装备请求体。
type LockEquipmentRequest struct {
	UID    string `json:"uid"`
	Locked bool   `json:"locked"`
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
