using System;
using Newtonsoft.Json.Linq;

namespace EPocalipse.Json.Viewer
{
    internal class AjaxNetDateTime: ICustomTextProvider
    {
        private static readonly long epoch = new DateTime(1970, 1, 1).Ticks;

        public string GetText(JsonObject jsonObject)
        {
            var text = jsonObject.Value.ToString();
            return "Ajax.Net Date:" + ConvertJSTicksToDateTime(Convert.ToInt64(text.Substring(1, text.Length - 2)));
        }

        private DateTime ConvertJSTicksToDateTime(long ticks)
        {
            return new DateTime((ticks * 10000) + epoch);
        }

        public string DisplayName => "Ajax.Net DateTime";

        public bool CanVisualize(JsonObject jsonObject)
        {
            if (jsonObject.JsonType == JsonType.Value && jsonObject.Value is JValue jvalue)
            {
                var text = jvalue.Value.ToString();
                return text.Length > 2 && text[0] == '@' && text[text.Length - 1] == '@';
            }
            return false;
        }
    }

    internal class CustomDate : ICustomTextProvider
    {
        public string GetText(JsonObject jsonObject)
        {
            var year = GetValue(jsonObject.Fields, "y");
            var month = GetValue(jsonObject.Fields, "M");
            var day = GetValue(jsonObject.Fields, "d");
            var hour = GetValue(jsonObject.Fields, "h");
            var min =GetValue(jsonObject.Fields, "m");
            var second = GetValue(jsonObject.Fields, "s");
            var ms = GetValue(jsonObject.Fields, "ms");

            return new DateTime(year, month, day, hour, min, second, ms).ToString();
        }

        private int GetValue(JsonFields fields, string key)
        {
            if (fields[key].Value is JValue value)
            {
                return (int)(long)value.Value;
            }

            return 0;
        }

        public string DisplayName => "Date";

        public bool CanVisualize(JsonObject jsonObject)
        {
            return jsonObject.ContainsFields("y","M","d","h","m","s","ms");
        }
    }
}
