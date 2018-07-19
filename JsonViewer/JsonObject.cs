using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EPocalipse.Json.Viewer
{
    [DebuggerDisplay("Type={GetType().Name} Text = {Text}")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class JsonObject
    {
        static JsonObject()
        {
            TypeDescriptor.AddProvider(new JsonObjectTypeDescriptionProvider(), typeof(JsonObject));
        }

        private string _text;

        public JsonObject()
        {
            Fields = new JsonFields(this);
        }

        public string Id { get; set; }
        public object Value { get; set; }
        public JsonType JsonType { get; set; }
        public JsonObject Parent { get; set; }

        public string Text
        {
            get
            {
                if (_text != null)
                {
                    return _text;
                }

                if (JsonType == JsonType.Value && Value is JValue jvalue)
                {
                    var val = (jvalue.Value == null ? "<null>" : jvalue.Value.ToString());

                    if (jvalue.Value is string)
                    {
                        val = "\"" + val + "\"";
                    }
                    _text = $"{Id} : {val}";
                }
                else
                {
                    _text = Id;
                }

                return _text;
            }
        }

        public JsonFields Fields { get; }

        internal void Modified()
        {
            _text = null;
        }

        public bool ContainsFields(params string[] ids )
        {
            return ids.All(s => Fields.ContainId(s));
        }

        public bool ContainsField(string id, JsonType type)
        {
            var field = Fields[id];
            return (field != null && field.JsonType == type);
        }
    }
}
