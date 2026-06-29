using EquipmentIdle.Data;
using EquipmentIdle.Net;
using EquipmentIdle.State;
using System.Collections.Generic;
using UnityEngine;

namespace EquipmentIdle.UI
{
    public class MainController : MonoBehaviour
    {
        private string _accountInput = "";
        private string _statusText = L10n.UIStatusDisconnected;
        private string _syncText = L10n.UINoSync;
        private string _powerText = L10n.UIPowerLabel;
        private GameState _gameState;
        private Vector2 _bagScroll;
        private Vector2 _equippedScroll;
        private bool _offlinePopup;
        private string _offlineText = "";
        private float _prevPower = 0;

        private struct Toast
        {
            public string text;
            public float expireAt;
            public Color color;
        }
        private readonly List<Toast> _toasts = new List<Toast>();
        private const float ToastDuration = 3f;
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
            _statusText = L10n.UIStatusConnected;
            _syncText = $"account={data.account} floor={data.floor} souls={data.souls}";
        }

        private void OnBag(List<EquipmentDTO> bag, List<EquipmentDTO> equipped) { }

        private void OnPower(float power)
        {
            float delta = power - _prevPower;
            if (delta > 0.5f)
                _powerText = string.Format(L10n.UIPowerDeltaUp, power, delta);
            else if (delta < -0.5f)
                _powerText = string.Format(L10n.UIPowerDeltaDown, power, delta);
            else
                _powerText = string.Format(L10n.UIPowerLabel, power);
            _prevPower = power;
        }

        private void OnLoot(EquipmentDTO eq)
        {
            string color = RarityColor(eq.rarity);
            string rname = L10n.RarityName(eq.rarity);
            string msg = string.Format(L10n.UILootToast, rname, eq.name);
            AddToast($"<color={color}>{msg}</color>", ToastDuration, Color.white);
        }

        private void OnFloor(int newFloor)
        {
            _syncText = $"account={_gameState.Account} floor={newFloor} souls={_gameState.Souls}";
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
            _offlineText = $"{L10n.UIOfflineTitle}\n"
                         + $"{string.Format(L10n.UIOfflineDuration, dur)}\n"
                         + $"{string.Format(L10n.UIOfflineLoot, ord.loot_count)}\n"
                         + $"{string.Format(L10n.UIOfflineFloors, ord.floors_advanced)}\n"
                         + $"{string.Format(L10n.UIOfflineTicks, ord.ticks_simulated)}";
            _offlinePopup = true;
        }

        private void OnCraftResult(CraftResultData cr)
        {
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
            if (canReincarn && !_prevCanReincarn)
            {
                AddToast("<color=#f80>Reincarnation available!</color> Tap REINCARNATE to reset for souls", 5f, new Color(1f, 0.5f, 0f));
            }
            _prevCanReincarn = canReincarn;
        }

        private void AddToast(string text, float duration, Color color)
        {
            _toasts.Add(new Toast { text = text, expireAt = Time.realtimeSinceStartup + duration, color = color });
            if (_toasts.Count > 8) _toasts.RemoveAt(0);
        }

        private void OnGUI()
        {
            float w = 620f, h = 640f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label(L10n.UIStage);
            GUILayout.Space(4);
            GUILayout.Label("Status: " + _statusText);
            GUILayout.Label(_syncText);

            var richStyle = new GUIStyle(GUI.skin.label) { richText = true };
            GUILayout.Label(_powerText, richStyle);

            float curPower = _gameState.Power;
            int curFloor = _gameState.Floor;
            float monsterPower = MonsterPowerAtFloor(curFloor);
            if (curPower > 0 && curPower <= monsterPower)
            {
                string stuckMsg = string.Format(L10n.UIStuckPrefix, curPower, monsterPower, curFloor);
                GUILayout.Label($"<color=#f44>{stuckMsg}</color>", richStyle);
            }

            GUILayout.Space(6);

            GUILayout.Label(L10n.UIAccount);
            _accountInput = GUILayout.TextField(_accountInput);
            if (GUILayout.Button(L10n.UIConnect, GUILayout.Height(24)))
            {
                string acc = _accountInput.Trim();
                if (string.IsNullOrEmpty(acc)) acc = "hero";
                _statusText = L10n.UIStatusConnecting;
                _gameState.ConnectAndLogin(acc);
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L10n.UIEquipped, _gameState.Equipped.Count));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L10n.UIEquipBest, GUILayout.Width(110)))
            {
                EquipBestBySlot();
            }
            GUILayout.EndHorizontal();

            _equippedScroll = GUILayout.BeginScrollView(_equippedScroll, GUILayout.Height(110));
            for (int s = 0; s < 8; s++)
            {
                EquipmentDTO eq = EquippedAtSlot(s);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(L10n.SlotName(s), GUILayout.Width(54));
                if (eq != null)
                {
                    GUILayout.Label(FormatEquipment(eq), richStyle, GUILayout.Width(420));
                    if (GUILayout.Button(L10n.UIUnequip, GUILayout.Width(70))) _gameState.Unequip(s);
                }
                else
                {
                    GUILayout.Label(L10n.UIEmptySlot, GUILayout.Width(420));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format(L10n.UIBackpack, _gameState.Bag.Count));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(L10n.UIDecomposeWeak, GUILayout.Width(130)))
            {
                DecomposeWeakItems();
            }
            GUILayout.EndHorizontal();

            _bagScroll = GUILayout.BeginScrollView(_bagScroll, GUILayout.Height(180));
            foreach (var eq in _gameState.Bag)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(FormatEquipment(eq), richStyle, GUILayout.Width(360));
                if (GUILayout.Button(L10n.UIEquip, GUILayout.Width(55))) _gameState.Equip(eq.uid);
                if (GUILayout.Button(L10n.UIDecompose, GUILayout.Width(48))) _gameState.Decompose(eq.uid);
                if (GUILayout.Button(L10n.UIReforge, GUILayout.Width(48))) _gameState.Reforge(eq.uid);
                if (GUILayout.Button(L10n.UIUpgrade, GUILayout.Width(48))) _gameState.Upgrade(eq.uid);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.Label(L10n.UIWorkshop);
            string matStr = L10n.UIMaterials;
            foreach (var kv in _gameState.Materials) matStr += kv.Key + "=" + kv.Value + " ";
            GUILayout.Label(matStr);

            GUILayout.Label(L10n.UIComposeLabel);
            GUILayout.BeginHorizontal();
            for (int s = 0; s < 8; s++)
            {
                if (GUILayout.Button(L10n.SlotName(s), GUILayout.Width(48)))
                    _gameState.Compose(s);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label(L10n.UIReincarnation);
            GUILayout.Label(string.Format(L10n.UISouls, _gameState.Souls, _gameState.MaxFloor, _gameState.CanReincarn));
            if (_gameState.CanReincarn && GUILayout.Button(L10n.UIReincarnate, GUILayout.Height(26)))
            {
                _gameState.Reincarn();
            }
            GUILayout.Space(4);
            GUILayout.Label(L10n.UITalentsLabel);
            string[] talentNames = { L10n.TalentDamage, L10n.TalentQuality, L10n.TalentDrop, L10n.TalentOfflineGain };
            string[] talentDesc = { L10n.TalentDamageDesc, L10n.TalentQualityDesc, L10n.TalentDropDesc, L10n.TalentOfflineDesc };
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

            DrawToasts();
            if (_offlinePopup) DrawOfflinePopup();
        }

        private void EquipBestBySlot()
        {
            int equippedCount = 0;
            for (int slot = 0; slot < 8; slot++)
            {
                EquipmentDTO current = EquippedAtSlot(slot);
                EquipmentDTO best = null;
                float bestScore = current != null ? ScoreEquipment(current) : -1f;
                foreach (var eq in _gameState.Bag)
                {
                    if (eq.slot != slot) continue;
                    float score = ScoreEquipment(eq);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = eq;
                    }
                }
                if (best != null)
                {
                    _gameState.Equip(best.uid);
                    equippedCount++;
                }
            }
            if (equippedCount > 0)
            {
                AddToast(string.Format(L10n.UIEquipBestDone, equippedCount), 2f, Color.green);
            }
        }

        private void DecomposeWeakItems()
        {
            int count = 0;
            foreach (var eq in _gameState.Bag)
            {
                if (eq.rarity <= 1)
                {
                    _gameState.Decompose(eq.uid);
                    count++;
                }
            }
            if (count > 0)
            {
                AddToast(string.Format(L10n.UIDecomposeWeakDone, count), 2f, Color.green);
            }
        }

        private static float ScoreEquipment(EquipmentDTO eq)
        {
            float s = (eq.rarity + 1) * 100f + eq.upgrade * 20f;
            if (eq.affixes != null)
            {
                foreach (var affix in eq.affixes)
                {
                    s += affix.value;
                    s += affix.tier * 10f;
                }
            }
            return s;
        }

        private EquipmentDTO EquippedAtSlot(int slot)
        {
            foreach (var eq in _gameState.Equipped)
            {
                if (eq.slot == slot) return eq;
            }
            return null;
        }

        private static string FormatEquipment(EquipmentDTO eq)
        {
            string color = RarityColor(eq.rarity);
            string text = $"<color={color}>[{L10n.RarityName(eq.rarity)}]</color> {eq.name} +{eq.upgrade} ({L10n.SlotName(eq.slot)})";
            if (eq.affixes != null && eq.affixes.Length > 0)
            {
                text += "  ";
                int max = Mathf.Min(2, eq.affixes.Length);
                for (int i = 0; i < max; i++)
                {
                    if (i > 0) text += ", ";
                    text += $"{eq.affixes[i].type}+{eq.affixes[i].value:F0}";
                }
                if (eq.affixes.Length > max) text += "...";
            }
            return text;
        }

        private void DrawToasts()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = _toasts.Count - 1; i >= 0; i--) { if (now >= _toasts[i].expireAt) _toasts.RemoveAt(i); }
            if (_toasts.Count == 0) return;
            float toastW = 280f, toastH = 28f;
            float tx = Screen.width - toastW - 10f, ty = 10f;
            var richStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 };
            for (int i = 0; i < _toasts.Count; i++)
            {
                var t = _toasts[i];
                float alpha = Mathf.Clamp01((t.expireAt - now) / 1f);
                Color c = t.color; c.a *= alpha;
                GUI.color = c;
                GUI.Box(new Rect(tx, ty + i * (toastH + 4), toastW, toastH), t.text, GUI.skin.box);
                GUI.color = Color.white;
            }
        }

        private void DrawOfflinePopup()
        {
            float pw = 300f, ph = 220f;
            float px = (Screen.width - pw) / 2f, py = (Screen.height - ph) / 2f;
            GUI.Box(new Rect(px, py, pw, ph), "");
            GUILayout.BeginArea(new Rect(px, py, pw, ph), GUI.skin.box);
            GUILayout.Label(_offlineText);
            GUILayout.Space(12);
            if (GUILayout.Button(L10n.UIOK, GUILayout.Height(28))) { _offlinePopup = false; }
            GUILayout.EndArea();
        }

        private static float MonsterPowerAtFloor(int floor)
        {
            float p = 3f + (floor - 1) * 5f;
            if (floor % 5 == 0) p *= 1.8f;
            return p;
        }

        private static string RarityColor(int r)
        {
            switch (r) { case 0: return "#aaa"; case 1: return "#4af"; case 2: return "#fd4"; case 3: return "#f80"; case 4: return "#f44"; default: return "#fff"; }
        }
    }
}
