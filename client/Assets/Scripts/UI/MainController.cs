using EquipmentIdle.Net;
using EquipmentIdle.State;
using System.Collections.Generic;
using UnityEngine;

namespace EquipmentIdle.UI
{
    /// <summary>
    /// 主场景控制器：用 IMGUI(OnGUI) 画界面，零 UI 包依赖。
    /// 显示连接、战力、层数、背包列表与一键穿戴。
    /// </summary>
    public class MainController : MonoBehaviour
    {
        private string _accountInput = "";
        private string _statusText = "disconnected";
        private string _syncText = "(no sync yet)";
        private string _powerText = "power: 0";
        private GameState _gameState;
        private Vector2 _bagScroll;
        private bool _offlinePopup;
        private string _offlineText = "";

        // 战力变化追踪
        private float _prevPower = 0;

        // toast 通知系统（掉落/强化/转生/Boss 第一关）
        private struct Toast
        {
            public string text;
            public float expireAt;
            public Color color;
        }
        private readonly List<Toast> _toasts = new List<Toast>();
        private const float ToastDuration = 3f;

        // 之前 CanReincarn 状态（检测变化触发提示）
        private bool _prevCanReincarn = false;

        private void Start()
        {
            if (GameState.Instance == null)
            {
                var go = new GameObject("GameState");
                go.AddComponent<GameState>();
            }
            _gameState = GameState.Instance;
            _gameState.OnSyncReceived += OnSync;
            _gameState.OnBagReceived += OnBag;
            _gameState.OnPowerReceived += OnPower;
            _gameState.OnLootReceived += OnLoot;
            _gameState.OnFloorReceived += OnFloor;
            _gameState.OnOfflineResultReceived += OnOfflineResult;
            _gameState.OnCraftResult += OnCraftResult;
            _gameState.OnTalentsReceived += OnTalents;
        }

        private void OnSync(SyncData data)
        {
            _statusText = "connected";
            _syncText = $"account={data.account} floor={data.floor} souls={data.souls}";
        }

        private void OnBag(List<EquipmentDTO> bag)
        {
            // UI 在 OnGUI 里直接读 GameState.Instance.Bag，无需缓存
        }

        private void OnPower(float power)
        {
            float delta = power - _prevPower;
            if (delta > 0.5f)
                _powerText = $"power: {power:F1} <color=#4f4>+{delta:F1}</color>";
            else if (delta < -0.5f)
                _powerText = $"power: {power:F1} <color=#f44>{delta:F1}</color>";
            else
                _powerText = $"power: {power:F1}";
            _prevPower = power;
        }

        private void OnLoot(EquipmentDTO eq)
        {
            // 掉落弹窗 toast
            string color = RarityColor(eq.rarity);
            string rname = RarityName(eq.rarity);
            AddToast($"[Loot] <color={color}>{rname} {eq.name}</color>", ToastDuration, Color.white);
        }

        private void OnFloor(int newFloor)
        {
            _syncText = $"account={_gameState.Account} floor={newFloor} souls={_gameState.Souls}";
            // Boss 击败提示：每 5 层一次（刚推进到的层是 newFloor，上一个 boss 是 newFloor-1）
            // 在线 Runner.Tick 推层后调用此回调。如果在推进时 newFloor-1 是 5 的倍数，说明刚打完 Boss。
            int justCleared = newFloor - 1;
            if (justCleared > 0 && justCleared % 5 == 0)
            {
                AddToast($"<color=#f80>Boss Defeated!</color> Floor {justCleared} cleared", ToastDuration, new Color(1f, 0.5f, 0f));
            }
        }

        private void OnOfflineResult(OfflineResultData ord)
        {
            int h = ord.duration_seconds / 3600;
            int m = (ord.duration_seconds % 3600) / 60;
            string dur = h > 0 ? $"{h}h{m}m" : $"{m}m";
            _offlineText = $"--- Offline Summary ---\n"
                         + $"Duration: {dur} (capped 8h)\n"
                         + $"Loot gained: {ord.loot_count} items\n"
                         + $"Floors advanced: {ord.floors_advanced}\n"
                         + $"Simulated ticks: {ord.ticks_simulated}";
            _offlinePopup = true;
        }

        private void OnCraftResult(CraftResultData cr)
        {
            // 强化/铸造反馈 toast
            if (cr.ok)
            {
                if (!string.IsNullOrEmpty(cr.uid))
                    AddToast($"<color=#4f4>{cr.msg} +{cr.upgrade}</color>", ToastDuration, Color.green);
                else
                    AddToast($"<color=#4f4>{cr.msg}</color>", ToastDuration, Color.green);
            }
            else
            {
                AddToast($"<color=#f44>{cr.msg}</color>", ToastDuration, Color.red);
            }
        }

        private void OnTalents(int souls, int maxFloor, bool canReincarn, Dictionary<string, int> talents)
        {
            // 转生提示：CanReincarn 从 false 变 true 时弹提示
            if (canReincarn && !_prevCanReincarn)
            {
                AddToast("<color=#f80>Reincarnation available!</color> Tap REINCARNATE to reset for souls", 5f, new Color(1f, 0.5f, 0f));
            }
            _prevCanReincarn = canReincarn;
        }

        private void AddToast(string text, float duration, Color color)
        {
            _toasts.Add(new Toast
            {
                text = text,
                expireAt = Time.realtimeSinceStartup + duration,
                color = color,
            });
            if (_toasts.Count > 8) _toasts.RemoveAt(0);
        }

        private void OnGUI()
        {
            float w = 480f, h = 480f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label("EquipmentIdle - Stage 6");
            GUILayout.Space(4);
            GUILayout.Label("Status: " + _statusText);
            GUILayout.Label(_syncText);

            // 战力文本（含 delta，用 rich text 显示颜色）
            var richStyle = new GUIStyle(GUI.skin.label) { richText = true };
            GUILayout.Label(_powerText, richStyle);

            // 卡点提示：当前战力打不过当前层怪物
            float curPower = _gameState.Power;
            int curFloor = _gameState.Floor;
            float monsterPower = MonsterPowerAtFloor(curFloor);
            if (curPower > 0 && curPower <= monsterPower)
            {
                GUILayout.Label($"<color=#f44>STUCK! Power {curPower:F0} < Monster {monsterPower:F0} at Floor {curFloor}</color>", richStyle);
            }

            GUILayout.Space(6);

            GUILayout.Label("Account:");
            _accountInput = GUILayout.TextField(_accountInput);
            if (GUILayout.Button("Connect", GUILayout.Height(24)))
            {
                string acc = _accountInput.Trim();
                if (string.IsNullOrEmpty(acc)) acc = "hero";
                _statusText = "connecting...";
                _gameState.ConnectAndLogin(acc);
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Backpack (" + _gameState.Bag.Count + " items):");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Equip Best", GUILayout.Width(90)))
            {
                EquipBest();
            }
            GUILayout.EndHorizontal();

            _bagScroll = GUILayout.BeginScrollView(_bagScroll, GUILayout.Height(180));
            foreach (var eq in _gameState.Bag)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                int affCount = eq.affixes != null ? eq.affixes.Length : 0;
                string info = $"[{RarityName(eq.rarity)}] {eq.name} +{eq.upgrade} ({SlotName(eq.slot)}) {affCount}aff";
                GUILayout.Label(info, GUILayout.Width(280));
                if (GUILayout.Button("Equip", GUILayout.Width(55))) _gameState.Equip(eq.uid);
                if (GUILayout.Button("Dec", GUILayout.Width(42))) _gameState.Decompose(eq.uid);
                if (GUILayout.Button("Ref", GUILayout.Width(42))) _gameState.Reforge(eq.uid);
                if (GUILayout.Button("Up", GUILayout.Width(42))) _gameState.Upgrade(eq.uid);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // 工坊区
            GUILayout.Space(6);
            GUILayout.Label("--- Workshop ---");
            string matStr = "Mats: ";
            foreach (var kv in _gameState.Materials)
                matStr += kv.Key + "=" + kv.Value + " ";
            GUILayout.Label(matStr);

            GUILayout.Label("Compose (cost 10 base_mat):");
            GUILayout.BeginHorizontal();
            for (int s = 0; s < 8; s++)
            {
                if (GUILayout.Button(SlotName(s), GUILayout.Width(48)))
                    _gameState.Compose(s);
            }
            GUILayout.EndHorizontal();

            // 转生面板
            GUILayout.Space(8);
            GUILayout.Label("--- Reincarnation ---");
            GUILayout.Label($"Souls: {_gameState.Souls}  MaxFloor: {_gameState.MaxFloor}  CanReincarn: {_gameState.CanReincarn}");
            if (_gameState.CanReincarn && GUILayout.Button("REINCARNATE", GUILayout.Height(26)))
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

            GUILayout.EndArea();

            // toast 通知（屏幕右侧从上往下排列）
            DrawToasts();

            // 离线结算弹窗（覆盖在主面板上方）
            if (_offlinePopup)
            {
                DrawOfflinePopup();
            }
        }

        /// <summary>一键穿戴：找背包中每槽位战力最高的穿上。</summary>
        private void EquipBest()
        {
            // 找出背包中所有装备的战力评分并穿戴最高的
            string bestUid = null;
            float bestScore = -1f;
            foreach (var eq in _gameState.Bag)
            {
                float score = ScoreEquipment(eq);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUid = eq.uid;
                }
            }
            if (bestUid != null)
            {
                _gameState.Equip(bestUid);
                AddToast("<color=#4f4>Equipped best item</color>", 2f, Color.green);
            }
        }

        /// <summary>装备评分：稀有度权重 + 强化等级 + 词缀数。</summary>
        private static float ScoreEquipment(EquipmentDTO eq)
        {
            float s = (eq.rarity + 1) * 100f + eq.upgrade * 10f;
            if (eq.affixes != null) s += eq.affixes.Length * 5f;
            return s;
        }

        private void DrawToasts()
        {
            float now = Time.realtimeSinceStartup;
            // 移除过期的 toast
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (now >= _toasts[i].expireAt) _toasts.RemoveAt(i);
            }
            if (_toasts.Count == 0) return;

            float toastW = 280f;
            float toastH = 28f;
            float tx = Screen.width - toastW - 10f;
            float ty = 10f;
            var richStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };

            for (int i = 0; i < _toasts.Count; i++)
            {
                var t = _toasts[i];
                float age = t.expireAt - now;
                float alpha = Mathf.Clamp01(age / 1f); // 最后一秒淡出
                Color c = t.color;
                c.a *= alpha;
                GUI.color = c;
                GUI.Box(new Rect(tx, ty + i * (toastH + 4), toastW, toastH), t.text, GUI.skin.box);
                GUI.color = Color.white;
            }
        }

        private void DrawOfflinePopup()
        {
            float pw = 300f, ph = 220f;
            float px = (Screen.width - pw) / 2f;
            float py = (Screen.height - ph) / 2f;
            GUI.Box(new Rect(px, py, pw, ph), "");
            GUILayout.BeginArea(new Rect(px, py, pw, ph), GUI.skin.box);
            GUILayout.Label(_offlineText);
            GUILayout.Space(12);
            if (GUILayout.Button("OK", GUILayout.Height(28)))
            {
                _offlinePopup = false;
            }
            GUILayout.EndArea();
        }

        /// <summary>怪物战力公式（与服务端 data.MonsterPower 一致）。</summary>
        private static float MonsterPowerAtFloor(int floor)
        {
            float p = 10f + (floor - 1) * 8f;
            if (floor % 5 == 0) p *= 1.8f; // Boss 层
            return p;
        }

        private static string RarityName(int r)
        {
            switch (r)
            {
                case 0: return "普通";
                case 1: return "魔法";
                case 2: return "稀有";
                case 3: return "传奇";
                case 4: return "神器";
                default: return "?";
            }
        }

        private static string RarityColor(int r)
        {
            switch (r)
            {
                case 0: return "#aaa"; // 普通 白
                case 1: return "#4af"; // 魔法 蓝
                case 2: return "#fd4"; // 稀有 黄
                case 3: return "#f80"; // 传奇 橙
                case 4: return "#f44"; // 神器 红
                default: return "#fff";
            }
        }

        private static string SlotName(int s)
        {
            string[] names = { "武器", "头盔", "护甲", "手套", "靴子", "戒指1", "戒指2", "项链" };
            if (s >= 0 && s < names.Length) return names[s];
            return "?";
        }
    }
}