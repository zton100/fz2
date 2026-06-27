using System;

namespace EquipmentIdle.Net
{
    /// <summary>
    /// WebSocket 消息信封编解码。
    /// 信封格式：{ "t": "<type>", "id": "<reqId>", "data": {...} }
    /// 使用 Unity 内置 JsonUtility，零第三方依赖。
    /// 注意：JsonUtility 要求顶层对象可序列化，data 用具体类型类承载。
    /// </summary>
    public static class Message
    {
        public const string TypeLogin = "login";
        public const string TypeSync = "sync";
        public const string TypeLoot = "loot";
        public const string TypeFloor = "floor";
        public const string TypeEquip = "equip";
        public const string TypeUnequip = "unequip";
        public const string TypeBag = "bag";
        public const string TypePower = "power";
        public const string TypeDecompose = "decompose";
        public const string TypeCompose = "compose";
        public const string TypeReforge = "reforge";
        public const string TypeUpgrade = "upgrade";
        public const string TypeMaterials = "materials";
        public const string TypeCraftResult = "craft_result";
        public const string TypeReincarn = "reincarn";
        public const string TypeTalentUp = "talent_up";
        public const string TypeTalents = "talents";

        /// <summary>编码登录请求为信封 JSON 字符串。</summary>
        public static string EncodeLogin(string id, string account)
        {
            string dataJson = "{\"account\":\"" + Escape(account) + "\"}";
            return "{\"t\":\"" + TypeLogin + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码穿戴请求。</summary>
        public static string EncodeEquip(string id, string uid)
        {
            string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
            return "{\"t\":\"" + TypeEquip + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码卸下请求。</summary>
        public static string EncodeUnequip(string id, int slot)
        {
            string dataJson = "{\"slot\":" + slot + "}";
            return "{\"t\":\"" + TypeUnequip + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码分解请求。</summary>
        public static string EncodeDecompose(string id, string uid)
        {
            string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
            return "{\"t\":\"" + TypeDecompose + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码合成请求。</summary>
        public static string EncodeCompose(string id, int slot)
        {
            string dataJson = "{\"slot\":" + slot + "}";
            return "{\"t\":\"" + TypeCompose + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码重铸请求。</summary>
        public static string EncodeReforge(string id, string uid)
        {
            string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
            return "{\"t\":\"" + TypeReforge + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码强化请求。</summary>
        public static string EncodeUpgrade(string id, string uid)
        {
            string dataJson = "{\"uid\":\"" + Escape(uid) + "\"}";
            return "{\"t\":\"" + TypeUpgrade + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>编码转生请求。</summary>
        public static string EncodeReincarn(string id)
        {
            return "{\"t\":\"" + TypeReincarn + "\",\"id\":\"" + Escape(id) + "\"}";
        }

        /// <summary>编码天赋升级请求。</summary>
        public static string EncodeTalentUp(string id, string name)
        {
            string dataJson = "{\"name\":\"" + Escape(name) + "\"}";
            return "{\"t\":\"" + TypeTalentUp + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
        }

        /// <summary>解析 materials 推送的 JSON，返回 key->count 字典。</summary>
        public static System.Collections.Generic.Dictionary<string, int> ParseMaterials(string dataJson)
        {
            return ParseIntMap(dataJson, "materials");
        }

        /// <summary>解析 talents 推送里的 talents 子字段，返回 key->level 字典。</summary>
        public static System.Collections.Generic.Dictionary<string, int> ParseTalents(string dataJson)
        {
            return ParseIntMap(dataJson, "talents");
        }

        /// <summary>从 JSON 提取指定对象字段为 int 字典。</summary>
        private static System.Collections.Generic.Dictionary<string, int> ParseIntMap(string dataJson, string key)
        {
            var dict = new System.Collections.Generic.Dictionary<string, int>();
            // 定位 "key":{ 子对象
            string pattern = "\"" + key + "\":";
            int i = dataJson.IndexOf(pattern, System.StringComparison.Ordinal);
            if (i < 0) return dict;
            i += pattern.Length;
            while (i < dataJson.Length && (dataJson[i] == ' ' || dataJson[i] == '\t')) i++;
            if (i >= dataJson.Length || dataJson[i] != '{') return dict;
            int depth = 0;
            int start = i;
            for (; i < dataJson.Length; i++)
            {
                if (dataJson[i] == '{') depth++;
                else if (dataJson[i] == '}')
                {
                    depth--;
                    if (depth == 0) break;
                }
            }
            if (i >= dataJson.Length) return dict;
            string mapJson = dataJson.Substring(start, i - start + 1);
            // 解析 mapJson 里的 "k":v 对
            int j = 1; // 跳过开头 {
            while (j < mapJson.Length - 1)
            {
                int q1 = mapJson.IndexOf('"', j);
                if (q1 < 0) break;
                int q2 = mapJson.IndexOf('"', q1 + 1);
                if (q2 < 0) break;
                string k = mapJson.Substring(q1 + 1, q2 - q1 - 1);
                int colon = mapJson.IndexOf(':', q2);
                if (colon < 0) break;
                int comma = mapJson.IndexOf(',', colon);
                int brace = mapJson.IndexOf('}', colon);
                int end = comma;
                if (comma < 0 || (brace >= 0 && brace < comma)) end = brace;
                if (end < 0) end = mapJson.Length;
                string valStr = mapJson.Substring(colon + 1, end - colon - 1).Trim();
                int val;
                if (int.TryParse(valStr, out val)) dict[k] = val;
                j = end + 1;
            }
            return dict;
        }

        /// <summary>解析收到的信封文本。失败返回 null。</summary>
        public static ParsedMessage Parse(string text)
        {
            try
            {
                // JsonUtility 无法解析顶层含 data 为对象的混合结构，
                // 这里用轻量手写提取 t / id / data 三段。
                var pm = new ParsedMessage();
                pm.t = ExtractString(text, "t");
                pm.id = ExtractString(text, "id");

                // data 子对象：找到 "data": 之后到信封结尾的匹配大括号
                pm.dataJson = ExtractObject(text, "data");
                if (pm.t == null) return null;
                return pm;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>转义 JSON 字符串里的特殊字符。</summary>
        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        /// <summary>从 JSON 文本里提取某个字符串字段的值（仅匹配 "key":"value" 形式）。</summary>
        private static string ExtractString(string json, string key)
        {
            string pattern = "\"" + key + "\":\"";
            int i = json.IndexOf(pattern, StringComparison.Ordinal);
            if (i < 0) return "";
            i += pattern.Length;
            int end = json.IndexOf('"', i);
            if (end < 0) return "";
            return json.Substring(i, end - i);
        }

        /// <summary>从 JSON 文本里提取某个对象字段的 JSON 子串（匹配 "key":{...}）。</summary>
        private static string ExtractObject(string json, string key)
        {
            string pattern = "\"" + key + "\":";
            int i = json.IndexOf(pattern, StringComparison.Ordinal);
            if (i < 0) return "{}";
            i += pattern.Length;
            // 跳过空白
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t')) i++;
            if (i >= json.Length || json[i] != '{') return "{}";
            int depth = 0;
            int start = i;
            for (; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(start, i - start + 1);
                }
            }
            return "{}";
        }
    }

    /// <summary>解析后的消息。dataJson 是 data 字段的原始 JSON 子串，供具体类型反序列化。</summary>
    public class ParsedMessage
    {
        public string t;
        public string id;
        public string dataJson;
    }

    /// <summary>sync 消息的 data 结构，与服务端 protocol.SyncData 对应。</summary>
    [Serializable]
    public class SyncData
    {
        public string account;
        public int floor;
        public int souls;
        public string[] inventory;
    }

    /// <summary>login 消息的 data 结构。</summary>
    [Serializable]
    public class LoginData
    {
        public string account;
    }

    /// <summary>掉落推送的 data 结构。</summary>
    [Serializable]
    public class LootData
    {
        public string uid;
        public string base_id;
        public string name;
        public int slot;
        public int rarity;
        public int upgrade;
        public AffixData[] affixes;
    }

    /// <summary>层数推送的 data 结构。</summary>
    [Serializable]
    public class FloorData
    {
        public int floor;
    }

    /// <summary>装备传输对象（背包与已穿戴通用）。</summary>
    [Serializable]
    public class EquipmentDTO
    {
        public string uid;
        public string base_id;
        public string name;
        public int slot;
        public int rarity;
        public int upgrade;
        public AffixData[] affixes;
    }

    /// <summary>词缀传输对象。</summary>
    [Serializable]
    public class AffixData
    {
        public string type;
        public int tier;
        public float value;
    }

    /// <summary>背包全量推送。</summary>
    [Serializable]
    public class BagData
    {
        public EquipmentDTO[] items;
    }

    /// <summary>战力推送。</summary>
    [Serializable]
    public class PowerData
    {
        public float power;
    }

    /// <summary>养成操作结果推送。</summary>
    [Serializable]
    public class CraftResultData
    {
        public bool ok;
        public string msg;
        public string uid;
        public int upgrade;
    }
}
