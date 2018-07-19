using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EPocalipse.Json.Viewer
{
    public enum JsonType { Object, Array, Value };

    internal class JsonParseError : ApplicationException
    {
        public JsonParseError(string message, Exception innerException) : base(message, innerException) { }
    }

    public class JsonObjectTree
    {
        public static JsonObjectTree Parse(string json)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject(json);
                return new JsonObjectTree(jsonObject);
            }
            catch (Exception e)
            {
                throw new JsonParseError(e.Message, e);
            }
        }

        public JsonObjectTree(object rootObject)
        {
            Root = ConvertToObject("JSON", rootObject);
        }

        private JsonObject ConvertToObject(string id, object jsonObject)
        {
            var obj = CreateJsonObject(jsonObject);
            obj.Id = id;
            AddChildren(jsonObject, obj);
            return obj;
        }

        private void AddChildren(object jsonObject, JsonObject obj)
        {
            if (jsonObject is JObject javaScriptObject)
            {
                foreach (var pair in javaScriptObject)
                {
                    obj.Fields.Add(ConvertToObject(pair.Key, pair.Value));
                }
            }
            else
            {
                if (jsonObject is JArray javaScriptArray)
                {
                    for (var i = 0; i < javaScriptArray.Count; i++)
                    {
                        obj.Fields.Add(ConvertToObject("[" + i + "]", javaScriptArray[i]));
                    }
                }
            }
        }

        private JsonObject CreateJsonObject(object jsonObject)
        {
            var obj = new JsonObject();
            switch (jsonObject)
            {
                case JArray _:
                    obj.JsonType = JsonType.Array;
                    break;
                case JObject _:
                    obj.JsonType = JsonType.Object;
                    break;
                default:
                    obj.JsonType = JsonType.Value;
                    obj.Value = jsonObject;
                    break;
            }
            return obj;
        }

        public JsonObject Root { get; }
    }
}
