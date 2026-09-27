using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RgssExtractor.Core;

namespace RgssExtractor
{
    public class MainForm : Form
    {
        private MainParser parser = new MainParser();

        private List<Entry> entries = new List<Entry>();

        private string archivePath = string.Empty;

        private IContainer components;

        private SplitContainer splitContainer;

        private TreeView explorerView;

        private OpenFileDialog openFileDialog;
        
        private FolderBrowserDialog writeExportDialog;

        private MenuStrip menuStrip;

        private ToolStripMenuItem fileToolStripMenuItem;

        private ToolStripMenuItem openToolStripMenuItem;

        private ToolStripMenuItem closeArchiveToolStripMenuItem;

        private ToolStripSeparator toolStripSeparator;

        private ToolStripMenuItem exitToolStripMenuItem;

        private PictureBox previewPictureBox;

        private ToolStripMenuItem exportArchiveToolStripMenuItem;

        private ContextMenuStrip explorerMenu;

        private ToolStripMenuItem exportToolStripMenuItem;

        public MainForm()
        {
            this.InitializeComponent();
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            string[] array = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.ReadArchive(array[0]);
        }

        private void ReadArchive(string path)
        {
            if (path == "")
            {
                return;
            }
            this.CloseArchive();
            this.entries = this.parser.ParseFile(path);
            if (this.entries == null)
            {
                MessageBox.Show(this, $"{path} is not a supported RGSS archive.", "Cannot open archive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.BuildFileList(this.entries);
            this.archivePath = path;
        }

        private void ExportArchive(string path)
        {
            parser?.ExportArchive(path);
        }

        private void CloseArchive()
        {
            if (this.archivePath != "")
            {
                explorerView.Nodes.Clear();
                previewPictureBox.Image = null;
                parser.CloseFile();
            }
        }

        private void BuildFileList(List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                var array = entry.Name.Split(Path.DirectorySeparatorChar);
                var node = GetRoot(array[0]);
                AddPath(node, array, entry);
            }
        }

        private void AddPath(TreeNode node, string[] pathBits, Entry e)
        {
            for (var i = 1; i < pathBits.Length; i++)
            {
                node = AddNode(node, pathBits[i]);
            }
            node.Tag = e;
        }

        private TreeNode GetRoot(string key)
        {
            if (explorerView.Nodes.ContainsKey(key))
            {
                return explorerView.Nodes[key];
            }
            return explorerView.Nodes.Add(key, key);
        }

        private TreeNode AddNode(TreeNode node, string key)
        {
            if (node.Nodes.ContainsKey(key))
            {
                return node.Nodes[key];
            }
            return node.Nodes.Add(key, key);
        }

        private void ShowImage(Entry entry)
        {
            byte[] buffer = this.parser.GetFileData(entry);
            MemoryStream stream = new MemoryStream(buffer);
            Image image = Image.FromStream(stream);
            this.previewPictureBox.Image = image;
        }

        private void DetermineAction(Entry entry)
        {
            if (entry.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                this.ShowImage(entry);
            }
        }

        private void OnExplorerViewAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this.explorerView.SelectedNode == null || this.explorerView.SelectedNode.Tag == null)
            {
                return;
            }
            Entry entry = (Entry)this.explorerView.SelectedNode.Tag;
            this.DetermineAction(entry);
        }

        internal static List<Entry> CollectEntries(TreeNode node)
        {
            var result = new List<Entry>();
            if (node.Tag is Entry entry)
            {
                result.Add(entry);
            }
            foreach (TreeNode child in node.Nodes)
            {
                result.AddRange(CollectEntries(child));
            }
            return result;
        }

        private void OnExportMenuItemClick(object sender, EventArgs e)
        {
            if (this.explorerView.SelectedNode == null || writeExportDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            string path = writeExportDialog.SelectedPath;
            foreach (var entry in CollectEntries(this.explorerView.SelectedNode))
            {
                this.parser.ExportFile(entry, path);
            }
            MessageBox.Show(this, $"Data exported to {path}", "Export finished", MessageBoxButtons.OK);
        }

        private void OnExplorerViewNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            this.explorerView.SelectedNode = e.Node;
        }

        private void OnOpenMenuItemClick(object sender, EventArgs e)
        {
            if (this.openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            this.ReadArchive(this.openFileDialog.FileName);
        }

        private void OnExportArchiveMenuItemClick(object sender, EventArgs e)
        {
            if (writeExportDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            this.ExportArchive(writeExportDialog.SelectedPath);
        }

        private void OnCloseArchiveMenuItemClick(object sender, EventArgs e)
        {
            this.CloseArchive();
        }

        private void OnExitMenuItemClick(object sender, EventArgs e)
        {
            this.CloseArchive();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.explorerView = new System.Windows.Forms.TreeView();
            this.explorerMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.previewPictureBox = new System.Windows.Forms.PictureBox();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.writeExportDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportArchiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeArchiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.explorerMenu.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.previewPictureBox)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 25);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.explorerView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer.Panel2.AutoScroll = true;
            this.splitContainer.Panel2.BackColor = System.Drawing.Color.Transparent;
            this.splitContainer.Panel2.Controls.Add(this.previewPictureBox);
            this.splitContainer.Size = new System.Drawing.Size(782, 372);
            this.splitContainer.SplitterDistance = 260;
            this.splitContainer.TabIndex = 0;
            // 
            // explorer_view
            // 
            this.explorerView.ContextMenuStrip = this.explorerMenu;
            this.explorerView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.explorerView.Location = new System.Drawing.Point(0, 0);
            this.explorerView.Name = "explorerView";
            this.explorerView.Size = new System.Drawing.Size(260, 372);
            this.explorerView.TabIndex = 0;
            this.explorerView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.OnExplorerViewAfterSelect);
            this.explorerView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.OnExplorerViewNodeMouseClick);
            // 
            // explorer_menu
            // 
            this.explorerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportToolStripMenuItem});
            this.explorerMenu.Name = "contextMenuStrip1";
            this.explorerMenu.Size = new System.Drawing.Size(115, 26);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(114, 22);
            this.exportToolStripMenuItem.Text = "Export";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.OnExportMenuItemClick);
            // 
            // pic_preview
            // 
            this.previewPictureBox.Location = new System.Drawing.Point(4, 4);
            this.previewPictureBox.Name = "previewPictureBox";
            this.previewPictureBox.Size = new System.Drawing.Size(138, 130);
            this.previewPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.previewPictureBox.TabIndex = 0;
            this.previewPictureBox.TabStop = false;
            // 
            // openFileDialog1
            // 
            this.openFileDialog.Filter = "RGSS archives|*.rgssad;*.rgss2a;*.rgss3a";
            // 
            // menuStrip1
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(782, 25);
            this.menuStrip.TabIndex = 1;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exportArchiveToolStripMenuItem,
            this.closeArchiveToolStripMenuItem,
            this.toolStripSeparator,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(39, 21);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.openToolStripMenuItem.Text = "Open Archive";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OnOpenMenuItemClick);
            // 
            // exportArchiveToolStripMenuItem
            // 
            this.exportArchiveToolStripMenuItem.Name = "exportArchiveToolStripMenuItem";
            this.exportArchiveToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.exportArchiveToolStripMenuItem.Text = "Export Archive";
            this.exportArchiveToolStripMenuItem.Click += new System.EventHandler(this.OnExportArchiveMenuItemClick);
            // 
            // closeArchiveToolStripMenuItem
            // 
            this.closeArchiveToolStripMenuItem.Name = "closeArchiveToolStripMenuItem";
            this.closeArchiveToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.closeArchiveToolStripMenuItem.Text = "Close Archive";
            this.closeArchiveToolStripMenuItem.Click += new System.EventHandler(this.OnCloseArchiveMenuItemClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator.Name = "toolStripSeparator";
            this.toolStripSeparator.Size = new System.Drawing.Size(157, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.OnExitMenuItemClick);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(782, 397);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "RGSS Extract";
            this.Load += new System.EventHandler(this.OnMainFormLoad);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.explorerMenu.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.previewPictureBox)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void OnMainFormLoad(object sender, EventArgs e)
        {

        }
    }
}
