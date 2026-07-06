# 可试玩 Demo 重构开发记录

## 2026-07-02

### 目标确认

用户明确指出：当前项目虽然有自动战斗、装备、强化、离线、转生等系统，但试玩感受仍然不像 demo，只是功能骨架。参考目标是暗黑装备放置截图，需要把开发方向切到“可试玩 Demo 重构”。

### 已决定

- 暂停零散功能堆叠。
- 进入 Demo 重构线。
- 每轮开发必须记录做什么、完成什么、验证结果。

### 本轮计划

- 建立 `docs/demo-rebuild/PLAN.md`。
- 建立 `docs/demo-rebuild/LOG.md`。
- 开始 Demo-1：主界面骨架重构。

### 本轮完成

- 已建立 Demo 重构计划和开发记录。
- 主界面开始从工程面板切到游戏 HUD：
  - 顶部改为暗黑 HUD：标题、在线状态、账号、当前层数、魂点、战力、连接入口。
  - 卡关提示从旧 header 移入战斗区。
  - 底部新增系统日志栏和“材料 / 工坊 / 转生天赋”入口按钮。
  - 沿用当前暗黑配色、按钮边框、稀有度行样式。

### 验证

- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 暂未完成

- Unity batchmode 编译和主场景 smoke 暂未执行：当前 Unity 编辑器已经打开该项目，Unity 不允许第二个实例同时以 batchmode 打开同一项目。

### 用户更新概念图

- 用户提供新的竖屏移动端 UI 概念图。
- 已保存为 `docs/demo-rebuild/references/mobile-dark-idle-ui-concept.png`。
- 规划调整：Demo-1 从横版三栏主界面改为竖屏移动端主界面。
- 新结构优先级：
  - 顶部资源 HUD。
  - 大战斗区。
  - 当前目标 / Boss 进度 / 最近掉落。
  - 穿戴装备 / 背包 / 强化 / 材料摘要功能卡。
  - 底部装备详情操作面板。
  - 底部导航。

### 本轮追加完成

- UI 参考分辨率改为竖屏 `945 x 1672`。
- 主界面结构改为移动端纵向布局：
  - 顶部资源 HUD：层数、战力、魂点、账号、连接入口。
  - 大战斗区：Boss 标题、Boss 进度条、战斗舞台、战斗状态。
  - 状态卡：当前目标、Boss 进度、最近掉落。
  - 功能卡：穿戴装备、背包、强化、材料摘要。
  - 底部装备详情和操作区。
  - 底部系统日志与“材料 / 工坊 / 转生天赋”入口。

### 本轮追加验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。

### 仍需补验

- Unity 编译和主场景 smoke 仍需在 Unity 编辑器关闭后执行。

### 本轮继续目标

- 固化“每轮开发必须记录”的执行规则。
- 补齐 Demo-1 底部导航：从临时“材料 / 工坊 / 转生天赋”入口改为概念图方向的“战斗 / 背包 / 锻造 / 天赋”。

### 本轮继续完成

- `PLAN.md` 增加执行规则：每轮必须记录目标、完成内容、验证结果、遗留问题、提交信息。
- `PLAN.md` 增加 Demo-1 checklist。
- 底部区域调整为两层：
  - 上层保留系统日志。
  - 下层改为四个移动端导航按钮：战斗、背包、锻造、天赋。

### 本轮继续验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。

### 本轮继续遗留

- Unity 编译和主场景 smoke 已在用户关闭 Unity 后补跑通过。

### 补充验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过，结果 `MAIN_SMOKE_OK`。
- Unity 日志异常关键词扫描无命中。
- `go test -race ./...` 通过。

### 准备提交

- 本次提交范围：Demo-1 竖屏主界面骨架第一版、Demo 重构计划和日志、UI 参考图资产。

### 提交信息

- Commit subject: `Start mobile demo UI rebuild`

## 2026-07-02 Demo-2 第一轮

### 本轮目标

- 开始 Demo-2：装备体验成型。
- 把“穿戴装备”从文字列表改成固定部位装备格。
- 把背包行压缩成更像装备卡片的可点击列表。

### 本轮计划

- 更新装备区布局。
- 更新背包项样式。
- 跑 Unity / Go / 端到端验证。
- 提交并推送后汇报下一步计划。

### 本轮完成

- 穿戴装备区从文字列表改为固定部位装备格第一版。
- 每个装备格展示部位、装备名、强化等级、评分；空槽显示等待掉落。
- 背包装备从普通行改为卡片式信息：
  - 装备名和强化等级。
  - 部位、评分、相对当前装备的提升/更弱/持平。
  - 穿戴和分解按钮保留。

### 本轮验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过，结果 `MAIN_SMOKE_OK`。
- Unity 日志异常关键词扫描无命中。
- `go test ./...` 通过。
- `go test -race ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 本轮遗留

- 装备详情仍是单栏文本，下一轮拆成“当前选择 / 已装备 / 对比结果”。
- 装备格目前仍是文字化格子，真实图标/美术放到后续美术切片。

## 2026-07-05 Demo-2 第二轮

### 本轮目标

- 继续 Demo-2：把装备详情从单栏文本改成可决策的对比面板。
- 让玩家选中背包装备后，能同时看到当前选择、已装备、逐条差异。

### 本轮完成

- `EquipmentPresenter` 增加结构化装备对比行：
  - 评分、强化、品质。
  - 当前选择和已装备的词缀差异。
  - 正向变化用绿色上箭头，负向变化用红色下箭头。
- 装备详情面板拆成三块：
  - 当前选择。
  - 已装备。
  - 对比结果。
- 新增 presenter 测试覆盖结构化对比：
  - 评分提升。
  - 选择装备的词缀提升。
  - 已装备独有词缀在更换后显示下降。

### 本轮验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过。
- `go test ./...` 通过。
- `go test -race ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 本轮遗留

- 锁定/筛选雏形仍未做。
- 详情布局已能表达对比，但还没有真实装备图标和长文本截断细调。

## 2026-07-05 Demo-2 第三轮：暗黑像素 UI 风格基线

### 本轮目标

- 按用户提供的新参考图重定 UI 风格方向。
- 优先建立可复用的视觉基线：顶部 HUD、战斗舞台、状态卡、装备卡、详情面板。

### 本轮完成

- 保存新参考图：
  - `docs/demo-rebuild/references/dark-idle-rpg-ui-target.png`
- 使用 ImageGen 生成项目内战斗背景资产：
  - `client/Assets/Art/UIReferences/dark-dungeon-battle-bg.png`
  - `client/Assets/Resources/UI/dark-dungeon-battle-bg.png`
- 战斗舞台接入位图背景，改成暗黑地牢场景底图。
- 顶部 HUD 改成参考图方向的头像 + 分块资源栏。
- Boss 标题、进度条、战斗状态改成红/金高对比层级。
- 状态卡、装备详情、装备格、背包卡统一成黑铜面板和橙红品质光效。
- 按钮改为更厚重的金边暗黑按钮风格。

### 本轮验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 本轮遗留

- 目前是风格基线，不是完整复刻参考图。
- 英雄、Boss、装备图标仍主要是文字/面板表达；后续需要继续生成或接入像素角色、Boss、装备图标资产。
- 还没有做浏览器/截图级视觉回归对比。

## 2026-07-05 Demo-2 第四轮：布局稳定、资产接入、锁定筛选

### 本轮目标

- 修复 Unity Game 视口下整页重叠的问题。
- 补齐上一轮计划中的角色、Boss、装备图标资产。
- 完成 Demo-2 剩余的锁定/筛选雏形。

### 本轮完成

- 布局稳定化：
  - 主 UI 从横屏自适应缩放改成固定 `945px` 竖屏手机画布。
  - 外层增加垂直滚动容器，横向 Free Aspect 下居中显示，避免挤压重叠。
  - 顶部 HUD 收窄，详情面板改固定高度。
- 新增并接入视觉资产：
  - 英雄战斗像素图：`client/Assets/Resources/UI/hero-combat-sprite.png`
  - Boss 战斗像素图：`client/Assets/Resources/UI/boss-combat-sprite.png`
  - 装备图标：武器、头盔、护甲、手套、鞋子、戒指、项链、锻造。
- UI 接入：
  - 战斗舞台显示左侧英雄和右侧 Boss。
  - 穿戴装备格显示对应部位图标。
  - 背包卡和最近掉落显示装备图标。
  - 装备详情的当前选择/已装备区域显示装备图标。
- 交互补齐：
  - 背包筛选：全部 / 提升 / 稀有 / 分解。
  - 装备锁定/解锁按钮。
  - 锁定装备禁用手动分解。
  - 一键分解弱装会跳过锁定装备。

### 本轮验证

- Unity `EquipmentPresenterTestRunner.Run` 通过。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `go test ./...` 通过。
- `./scripts/verify-flow.sh` 通过。
- `git diff --check` 通过。

### 本轮遗留

- 锁定状态目前是客户端会话内状态，刷新或重登后不会持久化。
- 仍需人工在 Unity Game 视口检查不同缩放下的观感，尤其是横屏 Free Aspect 和竖屏比例。
- 下一轮可以把锁定状态持久化到服务端协议/存档。

## 2026-07-05 Demo-2 第五轮：第一屏切页止血

### 本轮目标

- 继续修复 Unity Game 视口里 UI 互相覆盖的问题。
- 从结构上减少第一屏同时显示的系统数量。

### 本轮完成

- 主界面拆成底部导航页：
  - 战斗页：战斗舞台、当前目标、Boss 进度、最近掉落、穿戴装备概览。
  - 背包页：背包筛选、背包列表、当前穿戴镜像、装备详情。
  - 锻造页：强化、材料、合成入口。
  - 天赋页：转生状态和天赋升级。
- 底部导航按钮现在切换实际页面，不再只是 toast 提示。
- 详情面板从战斗页移出，避免和战斗/状态卡互相挤压。
- 锻造、合成、转生、天赋文本不再塞进装备详情面板。
- 战斗舞台继续裁剪英雄/Boss 溢出，避免盖住 HUD。

### 本轮验证

- `go test ./...` 通过。
- `git diff --check` 通过。
- 当前 Unity Editor 日志未发现 C# 编译错误。

### 本轮遗留

- Unity Editor 当前打开项目，batchmode smoke 被 Unity 拒绝，需关闭 Editor 后补跑。
- 底部导航选中态目前不会随切页重绘颜色，只影响视觉反馈，不影响切页功能。

## 2026-07-06 Demo-3 第一轮：战斗反馈成型

### 本轮目标

- 按计划进入 Demo-3。
- 先做不依赖新协议的战斗体验增强：攻击节奏、命中反馈、怪物血条扰动、Boss/受阻状态强调。
- 保持展示逻辑可测试，不把所有规则继续堆进 `MainController`。

### 本轮完成

- `EquipmentPresenter` 新增 `CombatBeatState` 和 `BuildCombatBeatState`：
  - 根据楼层、玩家战力、攻击动画时间计算伤害文本。
  - 输出怪物血条瞬时推进、英雄/Boss 位移、命中特效透明度。
- `MainController` 接入自动战斗节奏：
  - 在线且有战力时按 2 秒节奏自动播放命中。
  - 推层事件触发一次更明确的命中反馈。
  - 命中时英雄向前、Boss 后退、中央显示伤害数字。
  - 怪物血条在命中瞬间短暂下降，再回到当前战斗态估算值。
- Boss/受阻状态强化：
  - Boss 关中央标识从 `VS` 改为 `BOSS`。
  - Boss 提示和受阻状态使用更强红色警示。
- 工程卫生：
  - `.gitignore` 增加本地存档、日志、Unity 验证结果忽略，避免提交运行产物。
- `PLAN.md` 标记 Demo-2 已完成，并把当前优先级切到 Demo-3。

### 本轮验证

- `go test ./...` 通过。
- `git diff --check` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`，`client/verify_result.txt` 为 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- 胜利/掉落仪式还只是已有 toast 和最近掉落高亮，需要继续加强高稀有掉落表现。
- Boss 通关反馈目前依赖推层 toast，后续需要做更像“首通奖励”的战斗区反馈。
- 推层进度仍是 Boss 进度条，后续可以做连续战斗节点或层数轨道。
- `MainController.cs` 仍偏大，Demo-3 后应开始拆分 Battle/Equipment/Craft/Talent 视图。

### 提交信息

- Commit: `2e7b9aa`

## 2026-07-06 Demo 后续硬化：战斗控制器拆分

### 本轮目标

- 加快维护性重构进度，优先处理 `MainController.cs` 继续膨胀的问题。
- 将战斗视图构建、推层进度、掉落反馈、舞台动效从主控制器拆出，降低后续继续开发装备/工坊/天赋时的冲突风险。

### 本轮完成

- 新增 `MainController.Battle.cs` partial：
  - 战斗面板和移动端状态卡构建。
  - 战斗舞台、角色卡、血条、Boss 横幅。
  - 地牢状态刷新、5 层进度节点刷新。
  - 最近掉落列表和掉落行渲染。
  - 自动战斗节奏、伤害数字、Boss/掉落横幅反馈。
- `MainController.cs` 删除对应战斗方法，继续保留装备、背包、锻造、天赋、离线和通用 UI 辅助逻辑。
- `PLAN.md` 将战斗 partial 拆分标记为完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。

### 本轮遗留

- `MainController.cs` 仍可继续拆出 Equipment/Craft/Talent partial。
- 后续应补更细的客户端侧纯逻辑测试，减少每次都依赖 Unity 批处理的成本。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：PlayMode 主界面 smoke 增强

### 本轮目标

- 增加真实 Main 场景层面的 UI 装配回归测试。
- 避免 partial 拆分后只靠“启动不崩”判断，补上关键面板和 tab 切换断言。

### 本轮完成

- `PlayModeRunner.RunMainSmoke` 增强为结构化 smoke：
  - 检查 `UIDocument` 和 root visual tree。
  - 检查主框架、顶部 HUD、战斗区、背包页、锻造页、天赋页、底部导航、离线弹层节点存在。
  - 检查底部导航四个按钮存在。
  - 通过反射调用 `SetActiveTab`，验证战斗 / 背包 / 锻造 / 天赋四个 tab 切换后对应面板可见。
- smoke 失败时写入 `MAIN_SMOKE_FAIL <reason>`，并用非 0 退出码中断批处理。
- `PLAN.md` 将 PlayMode 主界面 smoke 增强标记完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`，`client/verify_result.txt` 为 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- PlayMode smoke 已覆盖结构和 tab 切换，还未模拟具体按钮操作如锁定、穿戴、分解、强化。
- 下一轮可以进入数值平衡打磨，或继续补按钮级交互 smoke。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：客户端决策回归测试

### 本轮目标

- 从结构拆分转向质量补强。
- 先覆盖 UI 最容易回归的决策点：背包筛选、锁定保护、锁定协议编码、工坊/转生边界文案。

### 本轮完成

- `EquipmentPresenter` 新增 `EquipmentBagFilter` 和 `ShouldShowInBag`：
  - `All` 显示普通背包项。
  - `Upgrades` 只显示正向提升。
  - `Rare` 显示稀有及以上。
  - `Decompose` 只显示未锁定、低稀有、非提升装备。
- `MainController.Equipment.cs` 的背包筛选改为调用 presenter 决策，避免 UI 私有逻辑无法测试。
- `EquipmentPresenterTestRunner` 增加回归覆盖：
  - 背包四种筛选模式。
  - 锁定装备不会出现在分解筛选。
  - `lock_equipment` 请求编码和解析。
  - +10 装备不再提示继续强化。
  - 合成材料不足显示精确缺口。
  - 无推荐天赋时给出明确文案。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- 还可以继续补 PlayMode 级细粒度 smoke，模拟点击底部导航和关键按钮。
- 数值平衡尚未开始系统化调参。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：UI helper 收口

### 本轮目标

- 完成 `MainController.cs` 的最终收口。
- 将跨视图复用的 UI helper、toast、离线弹层和样式常量迁出主控制器。

### 本轮完成

- 新增 `MainController.UI.cs` partial：
  - 颜色和按钮样式常量。
  - toast 状态和刷新逻辑。
  - 离线收益弹层构建。
  - `Panel` / `Row` / `Column` / `Text` / `SectionTitle` / `ActionButton` / `IconImage` 等 UI helper。
  - 装备稀有度颜色、边框、背景和动作按钮颜色映射。
- `MainController.cs` 删除通用 UI helper 和样式常量，剩余职责收敛为：
  - Unity 生命周期。
  - 服务端事件绑定和刷新调度。
  - 主界面 frame、HUD、tab、底部导航编排。
  - 少量跨视图状态辅助。
- 清理拆分后留下的多余空行。
- `PLAN.md` 将 UI helper partial 拆分标记为完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- `MainController` partial 拆分计划已基本完成。
- 下一轮可以转向功能质量：补客户端交互回归测试、背包/锻造/转生的细粒度 smoke，或开始数值平衡打磨。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：天赋控制器拆分

### 本轮目标

- 完成 `MainController` 视图型 partial 拆分的最后一块。
- 将转生页、天赋按钮、转生计划和推荐天赋逻辑迁出主控制器。

### 本轮完成

- 新增 `MainController.Talent.cs` partial：
  - 天赋常量。
  - 转生天赋页构建。
  - 转生状态和天赋文本刷新。
  - 转生计划卡刷新。
  - 天赋升级按钮刷新。
  - 推荐天赋选择逻辑。
- `MainController.cs` 删除对应天赋方法，剩余职责进一步收敛到生命周期、刷新调度、离线弹层、toast 和通用 UI helper。
- `PLAN.md` 将天赋 partial 拆分标记为完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- `MainController.cs` 还可继续拆出通用 UI helper partial。
- 下一轮建议做主文件最终收口：`MainController.UI.cs` 承载 Panel/Row/Text/Button/Icon/颜色 helper，主文件只留 orchestration。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：锻造控制器拆分

### 本轮目标

- 继续按维护性重构计划拆分 `MainController.cs`。
- 将锻造页、材料展示、工坊计划和强化/重铸完成反馈集中到独立 partial。

### 本轮完成

- 新增 `MainController.Craft.cs` partial：
  - 锻造页构建。
  - 材料摘要刷新和材料命名。
  - 工坊计划刷新和计划行样式。
  - 强化/重铸 pending 状态追踪。
  - 按 UID 查找装备并汇报评分变化。
- `MainController.cs` 删除对应锻造方法，继续保留天赋页、生命周期、toast 和通用 UI helper。
- `PLAN.md` 将锻造 partial 拆分标记为完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- `MainController.cs` 还剩天赋/转生页和通用 UI helper。
- 下一轮拆 `MainController.Talent.cs`，再评估主文件是否需要二次整理。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：装备控制器拆分

### 本轮目标

- 继续加快维护性重构进度。
- 将装备总览、背包、装备详情、对比结果和装备操作从 `MainController.cs` 拆出，降低后续开发锻造/天赋时的文件冲突和回归风险。

### 本轮完成

- 新增 `MainController.Equipment.cs` partial：
  - 穿戴装备总览和背包页构建。
  - 背包筛选按钮、装备格、背包装备卡。
  - 装备详情三栏、逐条对比结果、详情操作按钮。
  - 选择装备、保持选中装备、最佳穿戴、一键分解弱装、按槽位查找已装备。
- `MainController.cs` 删除对应装备方法，继续保留锻造、天赋、材料、toast、通用 UI helper 和生命周期逻辑。
- `PLAN.md` 将装备 partial 拆分标记为完成。

### 本轮验证

- `git diff --check` 通过。
- `go test ./...` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- `MainController.cs` 仍可继续拆出 Craft 和 Talent partial。
- 锻造完成反馈相关方法仍在主文件，下一轮拆 Craft 时一并收口。

### 提交信息

- 待提交。

## 2026-07-06 Demo 后续硬化：锁定持久化和局部拆分

### 本轮目标

- 按下一步计划继续完成维护性重构和锁定持久化。
- 让装备锁定从客户端会话状态升级为服务端权威状态。
- 先拆出一块高内聚逻辑，降低 `MainController.cs` 继续膨胀的风险。

### 本轮完成

- 服务端协议：
  - 新增 `lock_equipment` 请求。
  - `EquipmentDTO` 新增 `locked` 字段。
  - `bag` 推送携带背包/已穿戴装备的锁定状态。
- 服务端模型和存档：
  - `Player` 新增 `Locked map[string]bool`。
  - 旧 JSON 存档加载后补齐运行时默认 map。
  - 锁定状态写入 JSON 存档。
  - 转生会清空锁定 UID，避免旧装备 UID 残留。
- 服务端行为：
  - `handleLockEquipment` 持久化锁定/解锁并回推 `bag`。
  - 分解已锁定装备会失败，服务端不再只依赖客户端保护。
- 客户端：
  - `Message` 新增 `lock_equipment` 编码。
  - `GameState` 新增 `LockEquipment`。
  - `MainController` 的锁定集合改为由 `bag` 同步驱动，重连后保留。
  - 锁定按钮改为发送服务端请求。
- 局部重构：
  - `MainController` 改成 partial。
  - 新增 `MainController.Locking.cs` 承载锁定、分解保护、锁定同步逻辑。
- 文档：
  - `docs/protocol.md` 更新锁定协议。
  - `README.md` 增加锁定持久化说明。
  - `PLAYTEST-5MIN.md` 增加锁定重连验收。
  - `PLAN.md` 增加 Demo 后续硬化完成项。

### 本轮验证

- `go test ./...` 通过。
- `go test -race ./...` 通过。
- `git diff --check` 通过。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`，`client/verify_result.txt` 为 `MAIN_SMOKE_OK`。

### 本轮遗留

- `MainController.cs` 仍然偏大，后续可继续拆 Battle/Craft/Talent 视图。
- 推送仍依赖当前 GitHub HTTPS 链路恢复。

### 提交信息

- 待提交。

## 2026-07-06 Demo-3/4/5 收口：完整可试玩验收

### 本轮目标

- 按用户要求继续直到 Demo 重构计划全部完成。
- 完成 Demo-3 剩余推层进度表达。
- 完成 Demo-4 工坊和转生体验成型。
- 完成 Demo-5 试玩验收文档和 README 状态更新。
- 所有内容完成后再统一提交和推送。

### 本轮完成

- 战斗推进：
  - Boss 进度卡新增 5 节点推层轨道。
  - 当前层、已通过层、Boss 节点有不同颜色和状态。
  - 推层不再只依赖单条进度条，玩家能看到本轮 5 层周期。
- 工坊体验：
  - 锻造页从固定“可强化”文案改为动态工坊计划。
  - 展示当前选择装备的下一强化等级和基础材料消耗。
  - 展示重铸词缀消耗数量。
  - 展示合成是否可用、弱装清理数量或一键穿戴预估提升。
- 转生体验：
  - 转生页展示当前条件、预计魂点、历史最高层、重置代价。
  - 增加下一点天赋推荐。
  - 转生不再只是按钮和原始天赋文本。
- 导航体验：
  - 底部导航切页后会刷新选中态颜色。
- Demo-5 验收：
  - 新增 `docs/demo-rebuild/PLAYTEST-5MIN.md`。
  - README 状态更新为“可试玩暗黑装备放置 Demo”。
  - README 增加 5 分钟试玩验收入口。
- `PLAN.md` 已将 Demo-3、Demo-4、Demo-5 checklist 全部标记完成。

### 本轮验证

- `go test ./...` 通过。
- `go test -race ./...` 通过。
- `git diff --check` 通过。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`，`client/verify_result.txt` 为 `MAIN_SMOKE_OK`。

### 本轮遗留

- 计划功能已收口。
- `MainController.cs` 仍然偏大，后续应作为单独维护任务拆分视图类。
- 锁定状态仍是客户端会话内状态，后续如要正式化应接入协议和存档。

### 提交信息

- Commit: `2e7b9aa`

## 2026-07-06 Demo-3 第二轮：掉落和 Boss 仪式

### 本轮目标

- 继续推进 Demo-3 剩余体验项。
- 加强掉落仪式和 Boss 通关反馈，让战斗区承担更明确的“发生了重要事件”的反馈。

### 本轮完成

- `EquipmentPresenter` 新增仪式文案：
  - `BuildLootCeremonyText`：稀有及以上掉落、可穿戴提升装备会生成舞台横幅文案。
  - `BuildBossClearBanner`：Boss 层通关后生成首通奖励横幅文案。
- `MainController` 战斗舞台新增横幅：
  - 稀有/传奇/神器掉落会在战斗区短暂显示品质横幅。
  - 普通但可提升的装备会显示“可穿戴提升”横幅。
  - Boss 击破会显示“Boss 击破 + 首通奖励”横幅。
  - 横幅使用现有黑铜底、金边、品质色，不新增外部资源。
- `EquipmentPresenterTestRunner` 增加掉落仪式和 Boss 击破文案测试。
- `PLAN.md` 将 Demo-3 的掉落仪式、Boss 通关反馈标记为完成。

### 本轮验证

- `go test ./...` 通过。
- `git diff --check` 通过。
- Unity `EquipmentPresenterTestRunner.Run` 通过，日志包含 `[EquipmentPresenterTestRunner] OK`。
- Unity `PlayModeRunner.RunMainSmoke` 通过，日志包含 `MAIN_SMOKE_OK`，`client/verify_result.txt` 为 `MAIN_SMOKE_OK`。
- `./scripts/verify-flow.sh` 通过，输出 `VERIFY_OK`。

### 本轮遗留

- 推层进度仍需要做成更明确的连续进度表达。
- `MainController.cs` 已经很大，下一轮建议先拆 Battle 视图，降低继续迭代风险。
- 当前未提交，提交前需要确认是否把 Demo-2 资产和 Demo-3 改动合并为一个提交，还是拆成两个提交。

### 提交信息

- Commit: `2e7b9aa`
