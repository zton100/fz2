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

        /// <summary>编码登录请求为信封 JSON 字符串。</summary>
        public static string EncodeLogin(string id, string account)
        {
            // 手动拼装，避免 JsonUtility 对嵌套 data 的额外包装类
            string dataJson = "{\"account\":\"" + Escape(account) + "\"}";
            return "{\"t\":\"" + TypeLogin + "\",\"id\":\"" + Escape(id) + "\",\"data\":" + dataJson + "}";
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
}
