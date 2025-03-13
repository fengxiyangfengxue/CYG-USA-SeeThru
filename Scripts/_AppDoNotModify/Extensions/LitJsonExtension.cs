using LitJson;

namespace Test._ScriptExtensions
{
    public static class LitJsonExtension
    {
        public static string BeautifyJson(this JsonData json)
        {
            JsonWriter jw = new JsonWriter();
            jw.PrettyPrint = false;
            json.ToJson(jw);
            return json.ToString().ChineseJson();
        }


        public static string CompressJson(this JsonData json)
        {
            JsonWriter jw = new JsonWriter();
            jw.PrettyPrint = false;
            json.ToJson(jw);
            return jw.ToString().ChineseJson();
        }

        public static string GetString(this JsonData json, string key)
        {
            string result = null;
            if (json.ContainsKey(key) && json[key] != null)
                result = json[key].ToString();
            return result;
        }

    }
}
