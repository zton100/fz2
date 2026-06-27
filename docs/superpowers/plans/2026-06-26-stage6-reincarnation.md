# 阶段 6：简化转生 — 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现转生系统——达第 10 层可转生，重置层数/装备/材料/强化，换魂点投入 4 个永久天赋节点，形成长线动力。MVP 收尾之作。

**Architecture:** `reincarnation` 包是纯逻辑：判断转生条件、计算魂点、执行重置、管理天赋树。玩家模型扩展 `MaxFloor`（历史最高层数，决定魂点）与 `Talents`（4 个天赋等级）。天赋效果通过修改 combat/loot/offline 的输入参数生效（全局伤害加成影响战力，掉落率加成影响稀有度权重，离线收益加成影响时长系数，初始品质影响新生成装备稀有度下限）。协议新增转生请求响应 + 天赋升级请求响应 + talents 推送。ws handler 处理请求并推送。

**Tech Stack:** Go 1.26。

**前置条件：** 阶段 0~5 完成。

---

## 设计要点

### 转生规则（设计文档）
- **转生条件**：达到第 10 层可转生
- **重置**：当前层数(→1) / 装备背包 / 已穿戴 / 材料 / 强化等级
- **保留**：魂点、天赋等级、MaxFloor
- **魂点公式**：`Souls += floor(MaxFloor / 5)`
- **4 个天赋节点**：
  1. `damage`：全局伤害 +5%/级（最多 10 级）
  2. `quality`：初始装备品质 +1 档/级（最多 3 级）
  3. `drop`：掉落率 +3%/级（最多 10 级）
  4. `offline_gain`：离线收益 +10%/级（最多 5 级）

### 天赋效果接入
- `damage`：combat.ComputePower 结果乘以 `(1 + 0.05*talentLv)`
- `quality`：loot.Drop 时稀有度下限提升（普通→魔法→稀有，按 talentLv）
- `drop`：loot.DropTable 的稀有度权重向高稀有度倾斜
- `offline_gain`：offline.Calc 的 tick 数乘以 `(1 + 0.10*talentLv)`

MVP 简化：天赋效果在调用点手动传入参数，不改各包内部签名。

---

## 文件结构

**服务端**
- `server/internal/reincarnation/reincarnation.go` — 转生与天赋纯逻辑
- `server/internal/reincarnation/reincarnation_test.go` — TDD
- `server/internal/model/player.go` — 扩展 MaxFloor、Talents 字段
- `server/internal/protocol/message.go` — 扩展 reincarn/talent_up 请求 + talents 推送
- `server/internal/ws/handler.go` — 处理转生/天赋请求，天赋效果接入战斗/掉落/离线

**客户端**
- `client/Assets/Scripts/Net/Message.cs` — 扩展 reincarn/talent_up 编码 + talents 解析
- `client/Assets/Scripts/State/GameState.cs` — 魂点/天赋状态 + 方法
- `client/Assets/Scripts/UI/MainController.cs` — 转生面板 UI

---

## Task 1: 玩家模型扩展（MaxFloor + Talents）

**Files:**
- Modify: `server/internal/model/player.go`

- [ ] **Step 1: 追加字段与方法**

在 Player struct 追加：
```go
	MaxFloor int                       // 历史最高层数（决定转生魂点）
	Talents  map[string]int            // 天赋等级 {damage,quality,drop,offline_gain}
```

NewPlayer 初始化追加：
```go
		MaxFloor: 1,
		Talents:  map[string]int{},
```

追加方法：
```go
// TalentLevel 返回指定天赋的当前等级。
func (p *Player) TalentLevel(name string) int {
	return p.Talents[name]
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出。

- [ ] **Step 3: 提交**

```bash
git add server/internal/model/player.go
git commit -m "feat: player MaxFloor and Talents fields"
```

---

## Task 2: 转生与天赋纯逻辑（TDD）

**Files:**
- Create: `server/internal/reincarnation/reincarnation.go`
- Create: `server/internal/reincarnation/reincarnation_test.go`

- [ ] **Step 1: 写转生测试**

Create `server/internal/reincarnation/reincarnation_test.go`:
```go
package reincarnation

import (
	"testing"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
)

func TestCanReincarn_NotEnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	if CanReincarnate(p) {
		t.Fatal("floor 5 should not allow reincarnate")
	}
}

func TestCanReincarn_EnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 10
	if !CanReincarnate(p) {
		t.Fatal("floor 10 should allow reincarnate")
	}
}

func TestReincarnate_ResetsProgressGivesSouls(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 15
	p.MaxFloor = 15
	p.Equipped[data.SlotWeapon] = &model.Equipment{UID: "e1"}
	p.AddEquipment(&model.Equipment{UID: "e2"})
	p.AddMaterial(data.MatBase, 50)

	soulsBefore := p.Souls
	err := Reincarnate(p)
	if err != nil {
		t.Fatalf("reincarnate error: %v", err)
	}
	// 重置
	if p.Floor != 1 {
		t.Fatalf("floor = %d, want 1", p.Floor)
	}
	if len(p.EquipBag) != 0 {
		t.Fatalf("bag = %d, want 0", len(p.EquipBag))
	}
	if len(p.Equipped) != 0 {
		t.Fatalf("equipped = %d, want 0", len(p.Equipped))
	}
	if p.Materials[data.MatBase] != 0 {
		t.Fatalf("materials should be reset")
	}
	// 魂点 = floor(15/5) = 3
	if p.Souls != soulsBefore+3 {
		t.Fatalf("souls = %d, want %d", p.Souls, soulsBefore+3)
	}
	// MaxFloor 保留
	if p.MaxFloor != 15 {
		t.Fatalf("MaxFloor = %d, want 15", p.MaxFloor)
	}
}

func TestReincarnate_NotEnoughFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 5
	err := Reincarnate(p)
	if err == nil {
		t.Fatal("should error when floor < 10")
	}
}

func TestReincarnate_MaxFloorUpdates(t *testing.T) {
	p := model.NewPlayer("t")
	p.Floor = 20
	p.MaxFloor = 15 // MaxFloor 低于当前 floor
	Reincarnate(p)
	if p.MaxFloor != 20 {
		t.Fatalf("MaxFloor = %d, want 20", p.MaxFloor)
	}
}

func TestTalentUpgrade_Success(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 5
	err := UpgradeTalent(p, "damage")
	if err != nil {
		t.Fatalf("talent upgrade error: %v", err)
	}
	if p.Talents["damage"] != 1 {
		t.Fatalf("damage talent = %d, want 1", p.Talents["damage"])
	}
	if p.Souls != 4 {
		t.Fatalf("souls = %d, want 4", p.Souls)
	}
}

func TestTalentUpgrade_MaxLevel(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 100
	// damage 最多 10 级
	for i := 0; i < 10; i++ {
		UpgradeTalent(p, "damage")
	}
	err := UpgradeTalent(p, "damage")
	if err == nil {
		t.Fatal("should error at max level")
	}
}

func TestTalentUpgrade_NoSouls(t *testing.T) {
	p := model.NewPlayer("t")
	p.Souls = 0
	err := UpgradeTalent(p, "damage")
	if err == nil {
		t.Fatal("should error when no souls")
	}
}

func TestDamageBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["damage"] = 4
	// 4 级 = 20% 加成
	bonus := DamageBonus(p)
	if bonus != 0.20 {
		t.Fatalf("damage bonus = %.2f, want 0.20", bonus)
	}
}

func TestDropBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["drop"] = 3
	bonus := DropBonus(p)
	if bonus != 0.09 {
		t.Fatalf("drop bonus = %.2f, want 0.09", bonus)
	}
}

func TestOfflineGainBonus(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["offline_gain"] = 2
	bonus := OfflineGainBonus(p)
	if bonus != 0.20 {
		t.Fatalf("offline gain bonus = %.2f, want 0.20", bonus)
	}
}

func TestQualityFloor(t *testing.T) {
	p := model.NewPlayer("t")
	p.Talents["quality"] = 1
	// quality 1 级 = 稀有度下限 +1 档（普通→魔法）
	floor := QualityFloor(p)
	if floor != data.RarityMagic {
		t.Fatalf("quality floor = %d, want magic(%d)", floor, data.RarityMagic)
	}
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:
```bash
cd server
go test ./internal/reincarnation/...
```
Expected: 编译失败。

- [ ] **Step 3: 写转生实现**

Create `server/internal/reincarnation/reincarnation.go`:
```go
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
```

- [ ] **Step 4: 运行测试确认通过**

Run:
```bash
cd server
go test ./internal/reincarnation/...
```
Expected: `ok`。

- [ ] **Step 5: 提交**

```bash
git add server/internal/reincarnation/
git commit -m "feat: reincarnation logic with 4 talent nodes"
```

---

## Task 3: 协议扩展（reincarn/talent_up/talents）

**Files:**
- Modify: `server/internal/protocol/message.go`

- [ ] **Step 1: 追加协议类型**

在常量块追加：
```go
	TypeReincarn  = "reincarn"  // 请求：转生
	TypeTalentUp  = "talent_up" // 请求：天赋升级
	TypeTalents   = "talents"   // 推送：天赋状态
```

在文件末尾追加：
```go
// TalentUpRequest 天赋升级请求体。
type TalentUpRequest struct {
	Name string `json:"name"`
}

// TalentsData 天赋状态推送。
type TalentsData struct {
	Souls     int            `json:"souls"`
	MaxFloor  int            `json:"max_floor"`
	CanReincarn bool         `json:"can_reincarn"`
	Talents   map[string]int `json:"talents"`
}
```

- [ ] **Step 2: 验证编译**

Run:
```bash
cd server
go build ./...
```
Expected: 无输出。

- [ ] **Step 3: 提交**

```bash
git add server/internal/protocol/message.go
git commit -m "feat: protocol reincarn/talent_up/talents types"
```

---

## Task 4: ws handler 转生与天赋处理 + 天赋效果接入

**Files:**
- Modify: `server/internal/ws/handler.go`

这个任务较大：处理转生/天赋请求 + 在战斗循环和掉落中接入天赋效果。

- [ ] **Step 1: import 追加 reincarnation 包**

在 handler.go import 块追加：
```go
	"equipment-idle-server/internal/reincarnation"
```

- [ ] **Step 2: handleMessage switch 追加两个 case**

```go
	case protocol.TypeReincarn:
		h.handleReincarn(sess, env)
	case protocol.TypeTalentUp:
		h.handleTalentUp(sess, env)
```

- [ ] **Step 3: 追加处理函数**

在 handler.go 追加：
```go
// handleReincarn 转生请求。
func (h *Hub) handleReincarn(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	if err := reincarnation.Reincarnate(player); err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	// 转生后推送全套状态
	h.pushSync(sess, player, env.ID)
	h.pushBag(sess, player)
	h.pushPower(sess, player)
	h.pushMaterials(sess, player)
	h.pushTalents(sess, player)
	h.pushCraftResult(sess, true, "reincarnated", "", 0)
}

// handleTalentUp 天赋升级请求。
func (h *Hub) handleTalentUp(sess *Session, env protocol.Envelope) {
	if sess.Account == "" {
		return
	}
	player := h.store.LoadOrCreate(sess.Account)
	var req protocol.TalentUpRequest
	if err := json.Unmarshal(env.Data, &req); err != nil {
		return
	}
	if err := reincarnation.UpgradeTalent(player, req.Name); err != nil {
		h.pushCraftResult(sess, false, err.Error(), "", 0)
		return
	}
	h.pushTalents(sess, player)
	h.pushCraftResult(sess, true, "talent upgraded: "+req.Name, "", 0)
}

// pushTalents 推送天赋状态。
func (h *Hub) pushTalents(sess *Session, player *model.Player) {
	td := protocol.TalentsData{
		Souls:       player.Souls,
		MaxFloor:    player.MaxFloor,
		CanReincarn: reincarnation.CanReincarnate(player),
		Talents:     player.Talents,
	}
	dataBytes, _ := json.Marshal(td)
	env := protocol.Envelope{T: protocol.TypeTalents, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}

// pushSync 推送全量同步（抽取自 handleLogin 的同步逻辑，供转生后复用）。
func (h *Hub) pushSync(sess *Session, player *model.Player, id string) {
	sync := protocol.SyncData{
		Account:   player.Account,
		Floor:     player.Floor,
		Souls:     player.Souls,
		Inventory: player.Inventory,
	}
	syncData, _ := json.Marshal(sync)
	resp := protocol.Envelope{T: protocol.TypeSync, ID: id, Data: syncData}
	out, _ := json.Marshal(resp)
	select {
	case sess.Send <- out:
	default:
	}
}
```

- [ ] **Step 4: handleLogin 追加 pushTalents**

在 handleLogin 的 pushMaterials 之后追加：
```go
	h.pushTalents(sess, player)
```

- [ ] **Step 5: 战斗循环接入天赋效果**

修改 handleLogin 中创建 Runner 的部分，在 LootCallback 里接入 drop 天赋加成。但 Runner 内部用的是 drop.DropRandomSlot，不改 Runner 内部。

更简洁的接入方式：在 battleLoop 的 tick 之前/之后不强改。天赋效果在战斗结算的**战力计算**和**掉落**环节接入——但 Runner 内部已封装。

MVP 简化方案：天赋效果暂不在战斗循环里实时生效（避免大改 Runner），而是在**离线结算**和**登录战力推送**时生效。战斗循环的 tick 仍用 Runner 原逻辑。

这样 damage 天赋在 pushPower 时生效，offline_gain 天赋在离线结算时生效。drop 和 quality 天赋留作 v1.2（需要改 loot 包，范围较大）。

修改 `pushPower` 接入 damage 天赋：
```go
// pushPower 推送当前战力（含 damage 天赋加成）。
func (h *Hub) pushPower(sess *Session, player *model.Player) {
	stats := combat.AggregateStats(player.EquippedList())
	power := combat.ComputePower(stats)
	power *= (1.0 + reincarnation.DamageBonus(player))
	pd := protocol.PowerData{Power: power}
	dataBytes, _ := json.Marshal(pd)
	env := protocol.Envelope{T: protocol.TypePower, Data: dataBytes}
	out, _ := json.Marshal(env)
	select {
	case sess.Send <- out:
	default:
	}
}
```

修改 `handleLogin` 离线结算部分，接入 offline_gain 天赋：
```go
		if offlineDuration > 0 {
			drop := loot.NewDropTable(h.gen)
			result := offline.Calc(player, combat.ComputePower, drop, offlineDuration)
			// offline_gain 天赋加成：模拟 tick 数按比例增加
			gainBonus := reincarnation.OfflineGainBonus(player)
			if gainBonus > 0 && result.TicksSimulated > 0 {
				extraTicks := int(float64(result.TicksSimulated) * gainBonus)
				for i := 0; i < extraTicks; i++ {
					stats := combat.AggregateStats(player.EquippedList())
					playerPower := combat.ComputePower(stats) * (1.0 + reincarnation.DamageBonus(player))
					monster := data.MonsterAt(player.Floor)
					if playerPower > monster.Power {
						eq := drop.DropRandomSlot(player.Floor)
						if eq != nil {
							player.AddEquipment(eq)
							result.LootCount++
						}
						player.Floor++
						result.FloorsAdvanced++
					}
				}
			}
			if result.TicksSimulated > 0 {
				h.pushOfflineResult(sess, result)
			}
		}
```

- [ ] **Step 6: 验证编译与测试**

Run:
```bash
cd server
go build ./... && go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 7: 提交**

```bash
git add server/internal/ws/handler.go
git commit -m "feat: ws handler reincarn/talent_up with damage+offline talent effects"
```

---

## Task 5: 客户端 Message.cs + GameState 扩展

**Files:**
- Modify: `client/Assets/Scripts/Net/Message.cs`
- Modify: `client/Assets/Scripts/State/GameState.cs`

- [ ] **Step 1: Message.cs 追加类型与编码**

```csharp
public const string TypeReincarn = "reincarn";
public const string TypeTalentUp = "talent_up";
public const string TypeTalents = "talents";

public static string EncodeReincarn(string id)
{
    return "{\"t\":\"" + TypeReincarn + "\",\"id\":\"" + Escape(id) + "\"}";
}

public static string EncodeTalentUp(string id, string name)
{
    string dataJson = "{\"name\":\"" + Escape(name) + "\"}";
    return "{\"t\":\"" + TypeTalentUp + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
}
```

追加数据类：
```csharp
[Serializable]
public class TalentsData
{
    public int souls;
    public int max_floor;
    public bool can_reincarn;
    // talents 是 map，JsonUtility 不支持，用手写解析
}
```

追加 ParseTalents 方法（类似 ParseMaterials）：
```csharp
public static System.Collections.Generic.Dictionary<string, int> ParseTalents(string dataJson)
{
    var dict = new System.Collections.Generic.Dictionary<string, int>();
    int i = 0;
    while (i < dataJson.Length)
    {
        int q1 = dataJson.IndexOf('"', i);
        if (q1 < 0) break;
        int q2 = dataJson.IndexOf('"', q1 + 1);
        if (q2 < 0) break;
        string key = dataJson.Substring(q1 + 1, q2 - q1 - 1);
        int colon = dataJson.IndexOf(':', q2);
        if (colon < 0) break;
        int comma = dataJson.IndexOf(',', colon);
        int brace = dataJson.IndexOf('}', colon);
        int end = comma;
        if (comma < 0 || (brace >= 0 && brace < comma)) end = brace;
        if (end < 0) end = dataJson.Length;
        string valStr = dataJson.Substring(colon + 1, end - colon - 1).Trim();
        int val;
        if (int.TryParse(valStr, out val)) dict[key] = val;
        i = end + 1;
    }
    return dict;
}
```

注意 TalentsData 的 souls/max_floor/can_reincarn 用 JsonUtility 解析，talents map 用 ParseTalents 单独解析 dataJson。在 GameState 里分别提取。

- [ ] **Step 2: GameState 追加状态与方法**

```csharp
public event System.Action<int, int, bool, System.Collections.Generic.Dictionary<string,int>> OnTalentsReceived;
public int Souls { get; private set; } = 0;
public int MaxFloor { get; private set; } = 1;
public bool CanReincarn { get; private set; } = false;
public System.Collections.Generic.Dictionary<string,int> Talents { get; private set; } = new System.Collections.Generic.Dictionary<string,int>();

public void Reincarn() { _ws.SendText(Message.EncodeReincarn("r" + (_reqSeq++))); }
public void TalentUp(string name) { _ws.SendText(Message.EncodeTalentUp("r" + (_reqSeq++), name)); }
```

HandleMessage 追加 case：
```csharp
case Message.TypeTalents:
    // 解析标量字段
    int soulsVal = ExtractInt(msg.dataJson, "souls");
    int maxFloorVal = ExtractInt(msg.dataJson, "max_floor");
    bool canReinc = msg.dataJson.Contains("\"can_reincarn\":true");
    Souls = soulsVal;
    MaxFloor = maxFloorVal;
    CanReincarn = canReinc;
    Talents = Message.ParseTalents(msg.dataJson);
    OnTalentsReceived?.Invoke(Souls, MaxFloor, CanReincarn, Talents);
    break;
```

追加辅助方法 ExtractInt（类似 Message 里的提取）：
```csharp
private static int ExtractInt(string json, string key)
{
    string pattern = "\"" + key + "\":";
    int i = json.IndexOf(pattern);
    if (i < 0) return 0;
    i += pattern.Length;
    int end = i;
    while (end < json.Length && json[end] >= '0' && json[end] <= '9') end++;
    int val;
    int.TryParse(json.Substring(i, end - i), out val);
    return val;
}
```

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/Net/Message.cs client/Assets/Scripts/State/GameState.cs
git commit -m "feat: client reincarn/talent message and gamestate"
```

---

## Task 6: 客户端转生面板 UI

**Files:**
- Modify: `client/Assets/Scripts/UI/MainController.cs`

- [ ] **Step 1: 在 OnGUI 末尾追加转生面板**

在工坊区之后、EndArea 之前追加：
```csharp
GUILayout.Space(8);
GUILayout.Label("--- Reincarnation ---");
GUILayout.Label($"Souls: {_gameState.Souls}  MaxFloor: {_gameState.MaxFloor}  CanReincarn: {_gameState.CanReincarn}");
if (_gameState.CanReincarn && GUILayout.Button("REINCARNATE", GUILayout.Height(28)))
{
    _gameState.Reincarn();
}
GUILayout.Space(4);
GUILayout.Label("Talents (cost 1 soul each):");
string[] talentNames = { "damage", "quality", "drop", "offline_gain" };
string[] talentDesc = { "+5% dmg/lvl(max10)", "+1 quality/lvl(max3)", "+3% drop/lvl(max10)", "+10% offline/lvl(max5)" };
int[] talentMax = { 10, 3, 10, 5 };
for (int i = 0; i < 4; i++)
{
    GUILayout.BeginHorizontal();
    int lv = _gameState.Talents.ContainsKey(talentNames[i]) ? _gameState.Talents[talentNames[i]] : 0;
    GUILayout.Label($"{talentNames[i]} Lv{lv}/{talentMax[i]} - {talentDesc[i]}", GUILayout.Width(300));
    if (GUILayout.Button("+", GUILayout.Width(30)))
        _gameState.TalentUp(talentNames[i]);
    GUILayout.EndHorizontal();
}
```

- [ ] **Step 2: 验证编译**

Unity batchmode 编译，无 CS 错误。

- [ ] **Step 3: 提交**

```bash
git add client/Assets/Scripts/UI/MainController.cs
git commit -m "feat: client reincarnation panel UI"
```

---

## Task 7: 端到端验证 + 全量回归

- [ ] **Step 1: 转生冒烟测试**

Create `server/cmd/smokereincarn/main.go`:
```go
package main

import (
	"fmt"

	"equipment-idle-server/internal/data"
	"equipment-idle-server/internal/model"
	"equipment-idle-server/internal/reincarnation"
)

func main() {
	p := model.NewPlayer("smoke")
	p.Floor = 12
	p.MaxFloor = 12
	p.Equipped[data.SlotWeapon] = &model.Equipment{UID: "e1"}
	p.AddEquipment(&model.Equipment{UID: "e2"})
	p.AddMaterial(data.MatBase, 30)

	fmt.Printf("before: floor=%d souls=%d bag=%d equipped=%d mats=%d\n",
		p.Floor, p.Souls, len(p.EquipBag), len(p.Equipped), p.Materials[data.MatBase])

	err := reincarnation.Reincarnate(p)
	if err != nil {
		fmt.Println("FAIL reincarnate:", err); return
	}
	fmt.Printf("after: floor=%d souls=%d bag=%d equipped=%d mats=%d maxfloor=%d\n",
		p.Floor, p.Souls, len(p.EquipBag), len(p.Equipped), p.Materials[data.MatBase], p.MaxFloor)

	if p.Souls != 2 { // floor(12/5)=2
		fmt.Println("FAIL: souls should be 2"); return
	}

	// 升级天赋
	reincarnation.UpgradeTalent(p, "damage")
	reincarnation.UpgradeTalent(p, "damage")
	fmt.Printf("talents: %+v souls=%d\n", p.Talents, p.Souls)
	if p.Talents["damage"] != 2 || p.Souls != 0 {
		fmt.Println("FAIL: talent upgrade"); return
	}
	fmt.Printf("damage bonus: %.0f%%\n", reincarnation.DamageBonus(p)*100)

	fmt.Println("SMOKE_OK: reincarnation works")
}
```

- [ ] **Step 2: 运行冒烟**

Run:
```bash
cd server
go run ./cmd/smokereincarn/
```
Expected: `SMOKE_OK: reincarnation works`。

- [ ] **Step 3: 全量回归**

Run:
```bash
cd server
go test ./...
```
Expected: 全部 `ok`。

- [ ] **Step 4: 提交**

```bash
git add server/cmd/smokereincarn/main.go
git commit -m "test: smoke test reincarnation"
```

---

## 验收标准

阶段 6 完成后须满足：
1. `cd server && go test ./...` 全部通过（含 reincarnation 包）
2. 转生：达第 10 层可转生，重置层数/装备/材料，加魂点 `floor(MaxFloor/5)`
3. 4 个天赋节点可升级（消耗 1 魂点/级），有最大等级限制
4. damage 天赋影响 pushPower 的战力，offline_gain 天赋影响离线结算
5. 客户端转生面板：显示魂点/MaxFloor/天赋列表，可转生/升级天赋
6. 冒烟测试：转生后魂点正确、天赋升级正确
