# fz2 项目问题整理与修复建议

## 当前结论

仓库最新版本已经推进到阶段 6：转生系统 MVP。整体方向是对的，已经加入了转生、魂点、4 个天赋、协议、服务端 handler、客户端面板和测试。

但是当前存在几个关键问题：部分系统"看起来完成了"，但实际没有完整接入核心战斗、掉落、离线结算和客户端刷新流程。下一步不要继续加新系统，优先修这些问题。

---

# P0 必须马上修的问题

## 1. damage 天赋只影响显示战力，没有影响在线战斗

当前 `pushPower` 里对战力乘了 damage 天赋加成，但是在线战斗推进的 `Runner.Tick()` 仍然直接使用 `combat.ComputePower`，没有乘上 damage 天赋加成。UI 显示战力变强，实际打怪没变强。

**修复**：增加统一战力函数 `ComputePlayerPower(p)`，所有地方（pushPower/Runner/offline）统一使用。

## 2. drop 天赋和 quality 天赋没有真正接入掉落系统

已有 `DropBonus(p)` / `QualityFloor(p)`，但 `DropTable.DropRandomSlot` 和 `RollRarity` 仍只按层数计算，没使用天赋。

**修复**：掉落表增加带加成的方法，drop 提高高稀有度权重，quality 强制稀有度下限。

## 3. 到达转生层数后客户端转生按钮可能不及时出现

客户端 `CanReincarn` 只在收到 `talents` 推送后更新，但层数推进时只推 floor 不推 talents。

**修复**：`FloorCallback` 中同时推送 talents。

## 4. 转生魂点公式有长期经济漏洞

当前 `Souls += MaxFloor / 5`，MaxFloor 历史最高后会重复吃大量魂点。

**修复**：魂点按当前 Floor 发放，MaxFloor 只作历史记录。

## 5. MaxFloor 没有在普通推进层数时更新

MaxFloor 主要在转生时更新，导致 UI 显示不准确。

**修复**：每次推进层数时更新 MaxFloor，封装 `AdvanceFloor`。

---

# P1 需要尽快修的问题

## 6. 离线收益加成写在 handler 里，逻辑不干净

登录时先 `offline.Calc` 再在 handler 里额外模拟 tick，逻辑拆散、TicksSimulated 不准、原始 Calc 没吃 damage 天赋。

**修复**：offline_gain 放进 `offline.Calc` 内部，使用统一战力函数。

## 7. 仍然是内存存档，转生系统上线后风险更高

服务端重启数据全丢（Souls/Talents/MaxFloor/Materials/Equipment）。

**修复**：JSON 文件存档 `server/saves/{account}.json`，关键操作后保存。

## 8. 客户端 JSON 手写解析比较脆

如 `msg.dataJson.Contains("\"can_reincarn\":true")` 等写法，协议复杂后易出问题。

**修复**：talents map 改数组，写明确 DTO，减少手写解析。

## 9. 同账号多开会导致重复战斗循环

两个 battleLoop 同时推进同一 Player，掉落翻倍、数据竞争。

**修复**：Hub 维护 `accountSessions`，同账号新连接踢旧。

---

# P2 体验层面问题

## 10. 系统推进太快，体验未验证

下一阶段重点做可玩性打磨：掉落弹窗、战力变化提示、一键穿戴、转生提示、离线总结、强化反馈、卡点提示、Boss 首通奖励。

---

# 执行顺序

## 第一批：修真实生效
- 增加 `ComputePlayerPower(player)`
- 在线战斗 Runner / pushPower / offline.Calc 使用统一战力
- drop 天赋接入掉落权重
- quality 天赋接入稀有度下限
- 推层时更新 MaxFloor
- 推层时推送 talents 刷新 CanReincarn

## 第二批：修转生经济
- 转生魂点按当前 Floor 发放
- MaxFloor 只作历史最高记录
- 转生后完整推送
- 测试：历史 MaxFloor 高时低层转生不能重复吃

## 第三批：重构离线结算
- offline_gain 放进 offline.Calc
- TicksSimulated 含 offline_gain tick
- 用统一战力函数
- 删 handler 里额外离线 tick 模拟
- 测试

## 第四批：做持久化
- JSON 文件存档
- 登录读取、关键操作后保存
- 重启后数据存在
- 测试

## 第五批：修客户端协议解析
- talents 不用 Contains 判 bool
- talents map 改数组
- 写明确 DTO
- 减少手写解析

---

# 核心结论

不要继续加新功能。优先修：damage 天赋接入战斗、drop/quality 接入掉落、转生按钮刷新、魂点公式、MaxFloor 推层更新、离线逻辑回收、持久化存档。修完后才算进入"可试玩转生闭环"状态。
