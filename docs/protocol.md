# WebSocket 消息协议

## 信封格式

所有消息统一信封：

```json
{ "t": "<type>", "id": "<reqId>", "data": {...} }
```

- `t`：消息类型
- `id`：请求 ID，用于 req/resp 匹配；服务端推送/同步可空
- `data`：消息体，按 `t` 解析

## 枚举值

### Slot 装备槽位

| 值 | 槽位 |
|---|---|
| 0 | 武器 |
| 1 | 头盔 |
| 2 | 护甲 |
| 3 | 手套 |
| 4 | 靴子 |
| 5 | 戒指1 |
| 6 | 戒指2 |
| 7 | 项链 |

### Rarity 稀有度

| 值 | 稀有度 | 颜色 | 前缀/后缀数 |
|---|---|---|---|
| 0 | 普通 | 白 | 0/0 |
| 1 | 魔法 | 蓝 | 1/1 |
| 2 | 稀有 | 黄 | 2/2 |
| 3 | 传奇 | 橙 | 1/2 |
| 4 | 神器 | 红 | 2/2 |

---

## 客户端 → 服务端（请求）

### login — 登录

服务端加载或创建玩家，计算离线收益，回发 `sync` + `bag` + `power` + `materials` + `talents` + `offline_result`。

```json
{ "t": "login", "id": "r1", "data": { "account": "hero" } }
```

### equip — 穿戴装备

从背包穿戴指定 UID 的装备到对应槽位，旧装备退回背包。服务端回发 `bag` + `power`。

```json
{ "t": "equip", "id": "r2", "data": { "uid": "eq_1" } }
```

### unequip — 卸下装备

按槽位卸下装备，退回背包。服务端回发 `bag` + `power`。

```json
{ "t": "unequip", "id": "r3", "data": { "slot": 0 } }
```

### decompose — 分解装备

从背包分解指定 UID 的装备，获得基础材料 + 词缀材料。服务端回发 `materials` + `bag` + `craft_result`。

```json
{ "t": "decompose", "id": "r4", "data": { "uid": "eq_1" } }
```

### compose — 合成装备

消耗 10 个基础材料，生成指定槽位的普通品质装备。服务端回发 `materials` + `bag` + `craft_result`。

```json
{ "t": "compose", "id": "r5", "data": { "slot": 0 } }
```

### reforge — 重铸装备

消耗词缀材料，重新随机所有词缀（保留稀有度/基底/强化等级）。服务端回发 `materials` + `bag` + `craft_result`。

```json
{ "t": "reforge", "id": "r6", "data": { "uid": "eq_1" } }
```

### upgrade — 强化装备

消耗基础材料，尝试提升强化等级（+0 ~ +10）。+7 以上失败不掉级。服务端回发 `materials` + `bag` + `power` + `craft_result`。

```json
{ "t": "upgrade", "id": "r7", "data": { "uid": "eq_1" } }
```

### transfer_upgrade — 强化继承

将同部位来源装备的强化等级转移给背包里的目标装备，来源装备回到目标原强化等级。若来源是当前穿戴装备，服务端会在继承后直接穿戴目标装备，并把旧装退回背包。锁定装备不能参与继承。服务端回发 `bag` + `power` + `craft_result`。

```json
{ "t": "transfer_upgrade", "id": "r8", "data": { "source_uid": "old_eq", "target_uid": "new_eq" } }
```

### lock_equipment — 锁定/解锁装备

锁定背包装备，锁定后不会被一键分解或手动分解。服务端持久化锁定状态，并回发 `bag` + `craft_result`。

```json
{ "t": "lock_equipment", "id": "r9", "data": { "uid": "eq_1", "locked": true } }
```

### reincarn — 转生

达到第 10 层后可转生。重置层数/背包/装备/材料，按当前 Floor/5 获得魂点，保留 MaxFloor 和天赋。服务端回发 `sync` + `bag` + `power` + `materials` + `talents` + `craft_result`。

```json
{ "t": "reincarn", "id": "r10" }
```

### talent_up — 天赋升级

消耗 1 魂点升级指定天赋。服务端回发 `talents` + `craft_result`。

天赋名称：`damage`(max10), `quality`(max3), `drop`(max10), `offline_gain`(max5)。

```json
{ "t": "talent_up", "id": "r10", "data": { "name": "damage" } }
```

---

## 服务端 → 客户端（推送/响应）

### sync — 全量同步

登录响应或转生后推送。包含账号、当前层数、当前层小兵进度和魂点。

```json
{
  "t": "sync",
  "id": "r1",
  "data": {
    "account": "hero",
    "floor": 1,
    "floor_kills": 0,
    "minions_total": 3,
    "equipment_data_version": 1,
	"legendary_data_version": 1,
    "souls": 0,
    "inventory": []
  }
}
```

### combat — 战斗事件

每个服务端战斗 tick 推送一次，客户端据此播放攻击、受击、死亡和关卡切换动效。事件顺序早于同一场战斗产生的 `loot`、`bag` 和 `floor`。

`enemy_kind` 为 `minion`、`guardian` 或 `boss`。每层先清理 3 个小兵，随后挑战守关敌人；每 5 层的守关敌人为 Boss。
服务端会在一个 tick 内完成本次遭遇结算，并返回 `events` 逐击时间线，客户端只负责播放，不自行重算伤害。

```json
{
  "t": "combat",
  "data": {
    "floor": 5,
    "enemy_kind": "boss",
    "win": true,
    "player_power": 125.5,
    "enemy_power": 117.6,
    "minions_killed": 3,
    "minions_total": 3,
    "floor_advanced": true,
    "player_start_hp": 260,
    "enemy_start_hp": 194.1,
    "player_start_shield": 30,
    "player_end_hp": 208.5,
    "enemy_end_hp": 0,
    "events": [
      {
        "index": 1,
        "actor": "player",
        "kind": "hit",
        "damage": 43.9,
        "critical": false,
        "player_hp": 260,
        "enemy_hp": 150.2,
        "player_shield": 30
      }
    ]
  }
}
```

### bag — 背包全量

推送背包中所有未穿戴装备。穿戴/卸下/分解/合成/重铸/强化/掉落后推送。
`power_score` 是该装备在玩家当前构筑中相对空槽产生的权威战力贡献；同槽候选的分数差等于实际换装战力变化。`power_score_valid` 用于区分权威零分和旧客户端回退数据。
固定传奇额外包含 `legendary_id`、效果说明、固定属性、全局战力倍率和 Boss 首通材料倍率。没有对应效果的字段会省略。

```json
{
  "t": "bag",
  "data": {
    "items": [
      {
        "uid": "eq_1",
		"base_id": "weapon_ember_axe",
		"legendary_id": "legendary_ember_cleaver",
		"legendary_description": "余烬吞噬战意，全面提升伤害。",
		"legendary_bonuses": [
		  { "type": "fire_dmg", "tier": 0, "value": 18 }
		],
		"legendary_power_bonus": 0.12,
		"name": "焚城者之誓",
        "slot": 0,
		"rarity": 3,
        "upgrade": 3,
        "locked": false,
        "power_score": 48.2,
        "power_score_valid": true,
        "affixes": [
          { "type": "strength", "tier": 2, "value": 12.5 }
        ]
      }
    ]
  }
}
```

### power — 当前战力

推送玩家当前战力（含 damage 天赋加成）。穿戴/卸下/强化后推送。

```json
{ "t": "power", "data": { "power": 125.5 } }
```

### loot — 掉落装备

击败守关敌人时推送单件掉落装备。随后紧跟 `bag` 推送刷新背包；小兵不会直接掉落整件装备。

```json
{
  "t": "loot",
  "data": {
    "uid": "eq_5",
    "base_id": "leather_helm",
    "name": "皮盔",
    "slot": 1,
    "rarity": 0,
    "upgrade": 0,
    "locked": false,
    "affixes": []
  }
}
```

### floor — 层数推进

在线战斗胜利后推进层数时推送。同时会紧跟 `talents` 推送刷新转生按钮状态。

```json
{ "t": "floor", "data": { "floor": 3 } }
```

### materials — 材料库存

推送材料库存（数组格式，客户端 JsonUtility 可解析）。分解/合成/重铸/强化后推送。

```json
{
  "t": "materials",
  "data": {
    "materials": [
      { "k": "base_mat", "v": 50 },
      { "k": "affix_mat_1", "v": 3 }
    ]
  }
}
```

### craft_result — 养成操作结果

分解/合成/重铸/强化/转生/天赋升级的统一结果反馈。

```json
{ "t": "craft_result", "data": { "ok": true, "msg": "upgraded", "uid": "eq_1", "upgrade": 4 } }
```

| 字段 | 类型 | 说明 |
|---|---|---|
| ok | bool | 操作是否成功 |
| msg | string | 结果描述 |
| uid | string | 相关装备 UID（可选） |
| upgrade | int | 强化后等级（可选） |

### talents — 天赋状态

推送魂点、历史最高层、是否可转生、天赋等级。登录/推层/转生/天赋升级后推送。

```json
{
  "t": "talents",
  "data": {
    "souls": 2,
    "max_floor": 15,
    "can_reincarn": true,
    "talents": [
      { "name": "damage", "level": 2 },
      { "name": "quality", "level": 0 }
    ]
  }
}
```

### offline_result — 离线结算结果

登录时若上次离线时长 > 0，计算离线收益后推送。

```json
{
  "t": "offline_result",
  "data": {
    "duration_seconds": 7200,
    "ticks_simulated": 3600,
    "loot_count": 15,
    "floors_advanced": 8
  }
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| duration_seconds | int | 结算时长（秒，封顶 8h=28800） |
| ticks_simulated | int | 模拟 tick 数（含 offline_gain 加成） |
| loot_count | int | 离线掉落装备数 |
| floors_advanced | int | 离线推进层数 |

---

## 消息类型总览

| 类型 | 方向 | 说明 |
|---|---|---|
| `login` | C→S | 登录请求 |
| `sync` | S→C | 全量同步（响应登录/转生） |
| `bag` | S→C | 背包全量推送 |
| `power` | S→C | 当前战力推送 |
| `combat` | S→C | 权威战斗 tick 与小兵/守关进度 |
| `loot` | S→C | 单件掉落装备推送 |
| `floor` | S→C | 层数推进推送 |
| `equip` | C→S | 穿戴请求 |
| `unequip` | C→S | 卸下请求 |
| `decompose` | C→S | 分解请求 |
| `compose` | C→S | 合成请求 |
| `reforge` | C→S | 重铸请求 |
| `upgrade` | C→S | 强化请求 |
| `transfer_upgrade` | C→S | 强化继承请求 |
| `materials` | S→C | 材料库存推送 |
| `craft_result` | S→C | 养成操作结果 |
| `offline_result` | S→C | 离线结算结果 |
| `reincarn` | C→S | 转生请求 |
| `talent_up` | C→S | 天赋升级请求 |
| `talents` | S→C | 天赋状态推送 |

## 天赋系统

| 天赋 | 最大等级 | 效果 |
|---|---|---|
| damage | 10 | +5% 伤害/级（影响在线战斗 + 离线结算 + UI 战力） |
| quality | 3 | +1 稀有度下限/级（影响在线 + 离线掉落） |
| drop | 10 | +3% 高稀有度权重/级（影响在线 + 离线掉落） |
| offline_gain | 5 | +10% 离线模拟 tick 数/级 |
