using System.Windows.Forms;

namespace EPocalipse.Json.Viewer
{
    public partial class JsonObjectVisualizer : UserControl, IJsonVisualizer
    {
        public JsonObjectVisualizer()
        {
            InitializeComponent();
        }

        public string DisplayName => "Property Grid";

        public Control GetControl(JsonObject jsonObject)
        {
            return this;
        }

        public void Visualize(JsonObject jsonObject)
        {
            pgJsonObject.SelectedObject = jsonObject;
        }

        public bool CanVisualize(JsonObject jsonObject)
        {
            return true;
        }
    }
}
