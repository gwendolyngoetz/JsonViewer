using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Design;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading;
using EPocalipse.Json.Viewer.Properties;

namespace EPocalipse.Json.Viewer
{
    public partial class JsonViewer : UserControl
    {
        private string _json;
        private ErrorDetails _errorDetails;
        private readonly PluginsManager _pluginsManager = new PluginsManager();
        bool _updating;
        Control _lastVisualizerControl;
        private bool ignoreSelChange;

        public JsonViewer()
        {
            InitializeComponent();
            try
            {
                _pluginsManager.Initialize();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format(Resources.ConfigMessage, e.Message), "Json Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public string Json
        {
            get => _json;
            set
            {
                if (_json != value)
                {
                    _json = value.Trim();
                    txtJson.Text = _json;
                    Redraw();
                }
            }
        }

        [DefaultValue(25)]
        public int MaxErrorCount { get; set; } = 25;

        private void Redraw()
        {
            try
            {
                tvJson.BeginUpdate();
                try
                {
                    Reset();
                    if (!String.IsNullOrEmpty(_json))
                    {
                        JsonObjectTree tree = JsonObjectTree.Parse(_json);
                        VisualizeJsonTree(tree);
                    }
                }
                finally
                {
                    tvJson.EndUpdate();
                }
            }
            catch (JsonParseError e)
            {
                GetParseErrorDetails(e);
            }
            catch (Exception e)
            {
                ShowException(e);
            }
        }

        private void Reset()
        {
            ClearInfo();
            tvJson.Nodes.Clear();
            pnlVisualizer.Controls.Clear();
            _lastVisualizerControl = null;
            cbVisualizers.Items.Clear();
        }

        private void GetParseErrorDetails(Exception parserError)
        {
            UnbufferedStringReader strReader = new UnbufferedStringReader(_json);
            using (var reader = new JsonTextReader(strReader))
            {
                try
                {
                    while (reader.Read()) { };
                }
                catch (Exception e)
                {
                    _errorDetails._err = e.Message;
                    _errorDetails.Position = strReader.Position;
                }
            }
            if (_errorDetails.Error == null)
                _errorDetails._err = parserError.Message;
            if (_errorDetails.Position == 0)
                _errorDetails.Position = _json.Length;
            if (!txtJson.ContainsFocus)
                MarkError(_errorDetails);
            ShowInfo(_errorDetails);
        }

        private void MarkError(ErrorDetails _errorDetails)
        {
            ignoreSelChange = true;
            try
            {
                txtJson.Select(Math.Max(0, _errorDetails.Position - 1), 10);
                txtJson.ScrollToCaret();
            }
            finally
            {
                ignoreSelChange = false;
            }
        }

        private void VisualizeJsonTree(JsonObjectTree tree)
        {
            AddNode(tvJson.Nodes, tree.Root);
            JsonViewerTreeNode node = GetRootNode();
            InitVisualizers(node);
            node.Expand();
            tvJson.SelectedNode = node;
        }

        private void AddNode(TreeNodeCollection nodes, JsonObject jsonObject)
        {
            JsonViewerTreeNode newNode = new JsonViewerTreeNode(jsonObject);
            nodes.Add(newNode);
            newNode.Text = jsonObject.Text;
            newNode.Tag = jsonObject;
            newNode.ImageIndex = (int)jsonObject.JsonType;
            newNode.SelectedImageIndex = newNode.ImageIndex;

            foreach (JsonObject field in jsonObject.Fields)
            {
                AddNode(newNode.Nodes, field);
            }
        }

        [Browsable(false)]
        public ErrorDetails ErrorDetails => _errorDetails;

        public void Clear()
        {
            Json = String.Empty;
        }

        public void ShowInfo(string info)
        {
            lblError.Text = info;
            lblError.Tag = null;
            lblError.Enabled = false;
            tabControl.SelectedTab = pageTextView;
        }

        public void ShowInfo(ErrorDetails error)
        {
            ShowInfo(error.Error);
            lblError.Text = error.Error;
            lblError.Tag = error;
            lblError.Enabled = true;
            tabControl.SelectedTab = pageTextView;
        }

        public void ClearInfo()
        {
            lblError.Text = String.Empty;
        }

        [Browsable(false)]
        public bool HasErrors => _errorDetails._err != null;

        private void txtJson_TextChanged(object sender, EventArgs e)
        {
            Json = txtJson.Text;
            btnViewSelected.Checked = false;
        }

        private void txtFind_TextChanged(object sender, EventArgs e)
        {
            txtFind.BackColor = SystemColors.Window;
            FindNext(true, true);
        }

        public bool FindNext(bool includeSelected)
        {
            return FindNext(txtFind.Text, includeSelected);
        }

        public void FindNext(bool includeSelected, bool fromUI)
        {
            if (!FindNext(includeSelected) && fromUI)
                txtFind.BackColor = Color.LightCoral;
        }

        public bool FindNext(string text, bool includeSelected)
        {
            var startNode = tvJson.SelectedNode;
            if (startNode == null && HasNodes())
                startNode = GetRootNode();
            if (startNode != null)
            {
                startNode = FindNext(startNode, text, includeSelected);
                if (startNode != null)
                {
                    tvJson.SelectedNode = startNode;
                    return true;
                }
            }
            return false;
        }

        public TreeNode FindNext(TreeNode startNode, string text, bool includeSelected)
        {
            if (text == String.Empty)
                return startNode;

            if (includeSelected && IsMatchingNode(startNode, text))
                return startNode;

            var originalStartNode = startNode;
            startNode = GetNextNode(startNode);
            text = text.ToLower();
            while (startNode != originalStartNode)
            {
                if (IsMatchingNode(startNode, text))
                    return startNode;
                startNode = GetNextNode(startNode);
            }

            return null;
        }

        private TreeNode GetNextNode(TreeNode startNode)
        {
            var next = startNode.FirstNode ?? startNode.NextNode;
            if (next != null)
            {
                return next;
            }

            while (startNode != null && next == null)
            {
                startNode = startNode.Parent;
                if (startNode != null)
                {
                    next = startNode.NextNode;
                }
            }

            if (next != null)
            {
                return next;
            }

            next = GetRootNode();
            FlashControl(txtFind, Color.Cyan);

            return next;
        }

        private bool IsMatchingNode(TreeNode startNode, string text)
        {
            return startNode.Text.ToLower().Contains(text);
        }

        private JsonViewerTreeNode GetRootNode()
        {
            return tvJson.Nodes.Count > 0 ? (JsonViewerTreeNode) tvJson.Nodes[0] : null;
        }

        private bool HasNodes()
        {
            return tvJson.Nodes.Count > 0;
        }

        private void txtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FindNext(false, true);
            }
            if (e.KeyCode == Keys.Escape)
            {
                HideFind();
            }
        }

        private void FlashControl(Control control, Color color)
        {
            var prevColor = control.BackColor;
            try
            {
                control.BackColor = color;
                control.Refresh();
                Thread.Sleep(25);
            }
            finally
            {
                control.BackColor = prevColor;
                control.Refresh();
            }
        }

        public void ShowTab(Tabs tab)
        {
            tabControl.SelectedIndex = (int)tab;
        }
        
        private void btnFormat_Click(object sender, EventArgs e)
        {
            try
            {
                var json = txtJson.Text;
                var s = new JsonSerializer();
                var reader = new JsonTextReader(new StringReader(json));
                var jsonObject = s.Deserialize(reader);

                if (jsonObject != null)
                {
                    var sWriter = new StringWriter();
                    var writer = new JsonTextWriter(sWriter)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 4,
                        IndentChar = ' '
                    };
                    s.Serialize(writer, jsonObject);
                    txtJson.Text = sWriter.ToString();
                }
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void ShowException(Exception e)
        {
            MessageBox.Show(this, e.Message, "Json Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnStripToSqr_Click(object sender, EventArgs e)
        {
            StripTextTo('[', ']');
        }

        private void btnStripToCurly_Click(object sender, EventArgs e)
        {
            StripTextTo('{', '}');
        }

        private void StripTextTo(char sChr, char eChr)
        {
            var text = txtJson.Text;
            var start = text.IndexOf(sChr);
            var end = text.LastIndexOf(eChr);
            var newLen = end - start + 1;

            if (newLen > 1)
            {
                txtJson.Text = text.Substring(start, newLen);
            }
        }

        private void tvJson_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_pluginsManager.DefaultVisualizer == null)
                return;

            cbVisualizers.BeginUpdate();
            _updating = true;
            try
            {
                var node = (JsonViewerTreeNode)e.Node;
                var lastActive = (node.LastVisualizer ?? (IJsonVisualizer)cbVisualizers.SelectedItem) ?? _pluginsManager.DefaultVisualizer;

                cbVisualizers.Items.Clear();
                cbVisualizers.Items.AddRange(node.Visualizers.ToArray());

                var index = cbVisualizers.Items.IndexOf(lastActive);
                cbVisualizers.SelectedIndex = index != -1 ? index : cbVisualizers.Items.IndexOf(_pluginsManager.DefaultVisualizer);
            }
            finally
            {
                cbVisualizers.EndUpdate();
                _updating = false;
            }
            ActivateVisualizer();
        }

        private void ActivateVisualizer()
        {
            var visualizer = (IJsonVisualizer)cbVisualizers.SelectedItem;
            if (visualizer != null)
            {
                var jsonObject = GetSelectedTreeNode().JsonObject;
                var visualizerCtrl = visualizer.GetControl(jsonObject);
                if (_lastVisualizerControl != visualizerCtrl)
                {
                    pnlVisualizer.Controls.Remove(_lastVisualizerControl);
                    pnlVisualizer.Controls.Add(visualizerCtrl);
                    visualizerCtrl.Dock = DockStyle.Fill;
                    _lastVisualizerControl = visualizerCtrl;
                }
                visualizer.Visualize(jsonObject);
            }
        }

        private void cbVisualizers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_updating && GetSelectedTreeNode() != null)
            {
                ActivateVisualizer();
                GetSelectedTreeNode().LastVisualizer = (IJsonVisualizer)cbVisualizers.SelectedItem;
            }
        }

        private JsonViewerTreeNode GetSelectedTreeNode()
        {
            return (JsonViewerTreeNode)tvJson.SelectedNode;
        }

        private void tvJson_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            foreach (JsonViewerTreeNode node in e.Node.Nodes)
            {
                InitVisualizers(node);
            }
        }

        private void InitVisualizers(JsonViewerTreeNode node)
        {
            if (node.Initialized)
            {
                return;
            }

            node.Initialized = true;

            var jsonObject = node.JsonObject;

            foreach (var textVis in _pluginsManager.TextVisualizers)
            {
                if (textVis.CanVisualize(jsonObject))
                {
                    node.TextVisualizers.Add(textVis);
                }
            }

            node.RefreshText();

            foreach (var visualizer in _pluginsManager.Visualizers)
            {
                if (visualizer.CanVisualize(jsonObject))
                {
                    node.Visualizers.Add(visualizer);
                }
            }
        }

        private void btnCloseFind_Click(object sender, EventArgs e)
        {
            HideFind();
        }

        private void JsonViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F && e.Control)
            {
                ShowFind();
            }
        }

        private void HideFind()
        {
            pnlFind.Visible = false;
            tvJson.Focus();
        }

        private void ShowFind()
        {
            pnlFind.Visible = true;
            txtFind.Focus();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFind();
        }

        private void expandallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tvJson.BeginUpdate();
            try
            {
                if (tvJson.SelectedNode != null)
                {
                    var topNode = tvJson.TopNode;
                    tvJson.SelectedNode.ExpandAll();
                    tvJson.TopNode = topNode;
                }
            }
            finally
            {
                tvJson.EndUpdate();
            }
        }

        private void tvJson_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var node = tvJson.GetNodeAt(e.Location);
                if (node != null)
                {
                    tvJson.SelectedNode = node;
                }
            }
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == mnuShowOnBottom)
            {
                spcViewer.Orientation = Orientation.Horizontal;
                mnuShowOnRight.Checked = false;
            }
            else
            {
                spcViewer.Orientation = Orientation.Vertical;
                mnuShowOnBottom.Checked = false;
            }
        }

        private void cbVisualizers_Format(object sender, ListControlConvertEventArgs e)
        {
            e.Value = ((IJsonViewerPlugin)e.ListItem).DisplayName;
        }

        private void mnuTree_Opening(object sender, CancelEventArgs e)
        {
            mnuFind.Enabled = (GetRootNode() != null);
            mnuExpandAll.Enabled = (GetSelectedTreeNode() != null);

            mnuCopy.Enabled = mnuExpandAll.Enabled;
            mnuCopyValue.Enabled = mnuExpandAll.Enabled;
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            var text = txtJson.SelectionLength > 0 ? txtJson.SelectedText : txtJson.Text;
            Clipboard.SetText(text);
        }

        private void btnPaste_Click(object sender, EventArgs e)
        {
            txtJson.Text = Clipboard.GetText();
        }

        private void mnuCopy_Click(object sender, EventArgs e)
        {
            var node = GetSelectedTreeNode();
            if (node != null)
            {
                Clipboard.SetText(node.Text);
            }
        }

        private void mnuCopyName_Click(object sender, EventArgs e)
        {
            JsonViewerTreeNode node = GetSelectedTreeNode();

            if (node?.JsonObject.Id != null)
            {
                var obj = node.Tag as JsonObject;
                Clipboard.SetText(obj.Id);
            }
            else
            {
                Clipboard.SetText("");
            }

        }

        private void mnuCopyValue_Click(object sender, EventArgs e)
        {
            var node = GetSelectedTreeNode();
            if (node?.Tag != null)
            {
                var obj = node.Tag as JsonObject;
                Clipboard.SetText(obj.Value.ToString());
            }
            else
            {
                Clipboard.SetText("null");
            }
        }

        private void lblError_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lblError.Enabled && lblError.Tag != null)
            {
                var err = (ErrorDetails)lblError.Tag;
                MarkError(err);
            }
        }

        private void removeNewLineMenuItem_Click(object sender, EventArgs e)
        {
            StripFromText('\n', '\r');
        }

        private void removeSpecialCharsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var text = txtJson.Text;
            text = text.Replace(@"\""", @"""");
            txtJson.Text = text;
        }

        private void StripFromText(params char[] chars)
        {
            txtJson.Text = chars.Aggregate(txtJson.Text, (current, ch) => current.Replace(ch.ToString(), ""));
        }

        private void btnViewSelected_Click(object sender, EventArgs e)
        {
            _json = btnViewSelected.Checked ? txtJson.SelectedText.Trim() : txtJson.Text.Trim();
            Redraw();
        }

        private void txtJson_SelectionChanged(object sender, EventArgs e)
        {
            if (btnViewSelected.Checked && !ignoreSelChange)
            {
                _json = txtJson.SelectedText.Trim();
                Redraw();
            }
        }
    }

    public struct ErrorDetails
    {
        internal string _err;

        public string Error => _err;

        public int Position { get; internal set; }

        public void Clear()
        {
            _err = null;
            Position = 0;
        }
    }

    public class JsonViewerTreeNode : TreeNode
    {
        public JsonViewerTreeNode(JsonObject jsonObject)
        {
            JsonObject = jsonObject;
        }

        public List<ICustomTextProvider> TextVisualizers { get; } = new List<ICustomTextProvider>();

        public List<IJsonVisualizer> Visualizers { get; } = new List<IJsonVisualizer>();

        public JsonObject JsonObject { get; }

        internal bool Initialized { get; set; }

        internal void RefreshText()
        {
            var sb = new StringBuilder(JsonObject.Text);
            foreach (ICustomTextProvider textVisualizer in TextVisualizers)
            {
                try
                {
                    string customText = textVisualizer.GetText(JsonObject);
                    sb.Append(" (" + customText + ")");
                }
                catch
                {
                    //silently ignore
                }
            }

            var text = sb.ToString();

            if (text != Text)
            {
                Text = text;
            }
        }

        public IJsonVisualizer LastVisualizer { get; set; }
    }

    public enum Tabs { Viewer, Text };
}