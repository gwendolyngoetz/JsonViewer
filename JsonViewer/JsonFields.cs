using System.Collections.Generic;
using System.Collections;

namespace EPocalipse.Json.Viewer
{
    public class JsonFields : IEnumerable<JsonObject>
    {
        private readonly List<JsonObject> _fields = new List<JsonObject>();
        private readonly Dictionary<string, JsonObject> _fieldsById = new Dictionary<string, JsonObject>();

        public JsonFields(JsonObject parent)
        {
            Parent = parent;
        }

        public IEnumerator<JsonObject> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(JsonObject field)
        {
            field.Parent = Parent;
            _fields.Add(field);
            _fieldsById[field.Id] = field;
            Parent.Modified();
        }

        public int Count => _fields.Count;

        public JsonObject Parent { get; }

        public JsonObject this[int index] => _fields[index];

        public JsonObject this[string id] => _fieldsById.TryGetValue(id, out var result) ? result : null;

        public bool ContainId(string id)
        {
            return _fieldsById.ContainsKey(id);
        }
    }
}
