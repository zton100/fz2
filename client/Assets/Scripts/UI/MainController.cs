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
            _powerText = $"power: {power:F1}";
        }

        private void OnLoot(EquipmentDTO eq)
        {
            // 掉落即时提示（背包会在 bag 推送时刷新）
        }

        private void OnFloor(int newFloor)
        {
            _syncText = $"account={_gameState.Account} floor={newFloor} souls={_gameState.Souls}";
        }

        private void OnGUI()
        {
            float w = 480f, h = 460f;
            float x = (Screen.width - w) / 2f;
            float y = (Screen.height - h) / 2f;
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);

            GUILayout.Label("EquipmentIdle - Stage 3");
            GUILayout.Space(4);
            GUILayout.Label("Status: " + _statusText);
            GUILayout.Label(_syncText);
            GUILayout.Label(_powerText);
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
            GUILayout.Label("Backpack (" + _gameState.Bag.Count + " items):");

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

            GUILayout.EndArea();
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

        private static string SlotName(int s)
        {
            string[] names = { "武器", "头盔", "护甲", "手套", "靴子", "戒指1", "戒指2", "项链" };
            if (s >= 0 && s < names.Length) return names[s];
            return "?";
        }
    }
}
