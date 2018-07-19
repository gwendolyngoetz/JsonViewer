using System.Collections.Generic;

namespace EPocalipse.Json.Viewer
{
    internal class PluginsManager
    {
        private readonly List<IJsonViewerPlugin> _plugins = new List<IJsonViewerPlugin>();
        private readonly List<ICustomTextProvider> _textVisualizers = new List<ICustomTextProvider>();
        private readonly List<IJsonVisualizer> _visualizers = new List<IJsonVisualizer>();
        private IJsonVisualizer _defaultVisualizer;
        public void Initialize()
        {
            InitDefaults();
        }

        private void InitDefaults()
        {
            if (_defaultVisualizer != null)
            {
                return;
            }

            AddPlugin(new JsonObjectVisualizer());
            AddPlugin(new AjaxNetDateTime());
            AddPlugin(new CustomDate());
        }

        private void AddPlugin(IJsonViewerPlugin plugin)
        {
            _plugins.Add(plugin);
            switch (plugin)
            {
                case ICustomTextProvider provider:
                    _textVisualizers.Add(provider);
                    break;
                case IJsonVisualizer visualizer:
                    if (_defaultVisualizer == null)
                        _defaultVisualizer = visualizer;
                    _visualizers.Add(visualizer);
                    break;
            }
        }

        public IEnumerable<ICustomTextProvider> TextVisualizers => _textVisualizers;
        public IEnumerable<IJsonVisualizer> Visualizers => _visualizers;
        public IJsonVisualizer DefaultVisualizer => _defaultVisualizer;
    }
}
