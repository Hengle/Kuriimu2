﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kuriimu2_WinForms.Interfaces;
using Kore;
using Kontract.Interfaces.Common;
using System.IO;
using Kontract.Interfaces.Archive;
using Kontract.Interfaces.VirtualFS;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kontract.FileSystem;
using Kuriimu2_WinForms.Tools;
using Kuriimu2_WinForms.Properties;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;
using Kontract.Attributes;

namespace Kuriimu2_WinForms.FormatForms.Archive
{
    public partial class ArchiveForm : UserControl, IKuriimuForm
    {
        public KoreFileInfo Kfi { get; private set; }

        public bool HasChanges { get; set; }

        private Kore.Kore _kore;
        private TabControl _tabControl;
        private string _tempFolder;
        private string _subFolder;
        private IArchiveAdapter _archiveAdapter;

        private bool _openedAsSubStream;

        private bool _canExtractDirectories;
        private bool _canReplaceDirectories;
        private bool _canAddFiles;
        private bool _canExtractFiles;
        private bool _canReplaceFiles;
        private bool _canRenameFiles;
        private bool _canDeleteFiles;

        public ArchiveForm(KoreFileInfo kfi, TabControl tabControl, string tempFolder, string subFolder, bool openedAsSubStream)
        {
            InitializeComponent();

            // Overwrite window themes
            Win32.SetWindowTheme(treDirectories.Handle, "explorer", null);
            Win32.SetWindowTheme(lstFiles.Handle, "explorer", null);

            // Populate image list
            imlFiles.Images.Add("tree-directory", Resources.tree_directory);
            imlFiles.Images.Add("tree-directory-open", Resources.tree_directory_open);
            imlFiles.Images.Add("tree-text-file", Resources.tree_text_file);
            imlFiles.Images.Add("tree-image-file", Resources.tree_image_file);
            imlFiles.Images.Add("tree-archive-file", Resources.tree_archive_file);
            imlFilesLarge.Images.Add("tree-directory", Resources.tree_directory_32);
            imlFilesLarge.Images.Add("tree-directory-open", Resources.tree_directory_open);
            imlFilesLarge.Images.Add("tree-text-file", Resources.tree_text_file_32);
            imlFilesLarge.Images.Add("tree-image-file", Resources.tree_image_file_32);
            imlFilesLarge.Images.Add("tree-archive-file", Resources.tree_archive_file_32);
            treDirectories.ImageList = imlFiles;
            lstFiles.SmallImageList = imlFiles;
            lstFiles.LargeImageList = imlFilesLarge;

            Kfi = kfi;

            _kore = new Kore.Kore();
            _tabControl = tabControl;
            _tempFolder = tempFolder;
            _subFolder = subFolder;
            _archiveAdapter = kfi.Adapter as IArchiveAdapter;
            _openedAsSubStream = openedAsSubStream;

            if (!Directory.Exists(Path.Combine(tempFolder, subFolder)))
                Directory.CreateDirectory(Path.Combine(tempFolder, subFolder));

            LoadDirectories();
            UpdateForm();
        }

        private void ArchiveForm_Load(object sender, EventArgs e)
        {
            Dock = DockStyle.Fill;
            Padding = new Padding(3);
        }

        #region tlsMain
        private void tsbSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void tsbSaveAs_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
                Save(sfd.FileName);
            else
                MessageBox.Show("Something failed while choosing a save file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void tsbFind_Click(object sender, EventArgs e)
        {
            Stub();
        }

        private void tsbProperties_Click(object sender, EventArgs e)
        {
            Stub();
        }
        #endregion

        #region treDirectories
        private void treDirectories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            LoadFiles();
            UpdateForm();
        }

        private void treDirectories_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory";
                e.Node.SelectedImageKey = e.Node.ImageKey;
            }
        }

        private void treDirectories_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent != null)
            {
                e.Node.ImageKey = "tree-directory-open";
                e.Node.SelectedImageKey = e.Node.ImageKey;
            }
        }

        private void treDirectories_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            UpdateForm();
        }
        #endregion

        #region mnuDirectories
        private void mnuDirectories_Opening(object sender, CancelEventArgs e)
        {
            extractDirectoryToolStripMenuItem.Enabled = _canExtractDirectories;
            extractDirectoryToolStripMenuItem.Text = _canExtractDirectories ? $"E&xtract {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Extract is not supported";

            replaceDirectoryToolStripMenuItem.Enabled = _canReplaceDirectories;
            replaceDirectoryToolStripMenuItem.Text = _canReplaceDirectories ? $"&Replace {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Replace is not supported";

            addDirectoryToolStripMenuItem.Enabled = _canAddFiles;
            addDirectoryToolStripMenuItem.Text = _canAddFiles ? $"&Add to {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Add is not supported";

            deleteDirectoryToolStripMenuItem.Enabled = _canAddFiles;
            deleteDirectoryToolStripMenuItem.Text = _canAddFiles ? $"&Delete {Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_')}..." : "Delete is not supported";
        }

        private void extractDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treDirectories.SelectedNode;
            var selectedPath = string.Empty;

            while (node.Parent != null)
            {
                selectedPath = node.Text + "\\" + selectedPath;
                node = node.Parent;
            }

            ExtractFiles(CollectFiles(treDirectories.SelectedNode).ToList(), Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_'), selectedPath.TrimEnd('\\'));
        }

        private void replaceDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treDirectories.SelectedNode;
            var selectedPath = string.Empty;

            while (node.Parent != null)
            {
                selectedPath = node.Text + "\\" + selectedPath;
                node = node.Parent;
            }

            ReplaceFiles(CollectFiles(treDirectories.SelectedNode).ToList(), Path.GetFileName(treDirectories.SelectedNode.Text).Replace('.', '_'), selectedPath.TrimEnd('\\'));
        }

        private void addDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!treDirectories.Focused)
                return;

            AddFiles();

            LoadDirectories();
            UpdateForm();
        }

        private void deleteDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treDirectories.SelectedNode?.Tag is IEnumerable<ArchiveFileInfo>)
                DeleteFiles(treDirectories.SelectedNode?.Tag as IEnumerable<ArchiveFileInfo>);

            LoadDirectories();
            UpdateForm();
        }
        #endregion

        #region mnuFiles
        private void mnuFiles_Opening(object sender, CancelEventArgs e)
        {
            var selectedItem = lstFiles.SelectedItems.Count > 0 ? lstFiles.SelectedItems[0] : null;
            var afi = selectedItem?.Tag as ArchiveFileInfo;
            var ext = Path.GetExtension(afi?.FileName);

            extractFileToolStripMenuItem.Enabled = _canExtractFiles;
            extractFileToolStripMenuItem.Text = _canExtractFiles ? "E&xtract..." : "Extract is not supported";
            extractFileToolStripMenuItem.Tag = afi;

            replaceFileToolStripMenuItem.Enabled = _canReplaceFiles;
            replaceFileToolStripMenuItem.Text = _canReplaceFiles ? "&Replace..." : "Replace is not supported";
            replaceFileToolStripMenuItem.Tag = afi;

            renameFileToolStripMenuItem.Enabled = _canRenameFiles;
            renameFileToolStripMenuItem.Text = _canRenameFiles ? "Re&name..." : "Rename is not supported";
            renameFileToolStripMenuItem.Tag = afi;

            deleteFileToolStripMenuItem.Enabled = _canDeleteFiles;
            deleteFileToolStripMenuItem.Text = _canDeleteFiles ? "&Delete" : "Delete is not supported";
            deleteFileToolStripMenuItem.Tag = afi;

            // Generate supported application menu items
            var kuriimuVisible = ext?.Length > 0 && _kore.GetAdapters<ITextAdapter>().Select(x => _kore.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var kukkiiVisible = ext?.Length > 0 && _kore.GetAdapters<IImageAdapter>().Select(x => _kore.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());
            var karameruVisible = ext?.Length > 0 && _kore.GetAdapters<IArchiveAdapter>().Select(x => _kore.GetMetadata<PluginExtensionInfoAttribute>(x)).Any(x => x.Extension.ToLower().TrimStart('*') == ext.ToLower());

            openFileToolStripMenuItem.Enabled = kuriimuVisible || kukkiiVisible || karameruVisible;
            openFileToolStripMenuItem.Text = openFileToolStripMenuItem.Enabled ? "Open" : "No plugins support this file";
            openFileToolStripMenuItem.Tag = afi;

            //editInKuriimuToolStripMenuItem.Tag = new List<object> { afi, Applications.Kuriimu };
            //editInKukkiiToolStripMenuItem.Tag = new List<object> { afi, Applications.Kukkii };
            //editInKarameruToolStripMenuItem.Tag = new List<object> { afi, Applications.Karameru };

            //editInKuriimuToolStripMenuItem.Visible = kuriimuVisible;
            //editInKukkiiToolStripMenuItem.Visible = kukkiiVisible;
            //editInKarameruToolStripMenuItem.Visible = karameruVisible;

            //editListToolStripSeparator.Visible = kuriimuVisible || kukkiiVisible || karameruVisible;

        }

        private void extractFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            // TODO: Implement multi-selection of files
            var afi = menuItem.Tag as ArchiveFileInfo;
            var files = new List<ArchiveFileInfo> { afi };
            ExtractFiles(files);
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var afi = menuItem.Tag as ArchiveFileInfo;
            ReplaceFiles(new List<ArchiveFileInfo> { afi });
        }

        private void renameFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateForm();
            Stub();
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateForm();
            Stub();
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var afi = menuItem.Tag as ArchiveFileInfo;

            var bk = afi.FileData.Position;
            afi.FileData.Position = 0;
            var extractFile = File.Create(Path.Combine(_tempFolder, _subFolder, Path.GetFileName(afi.FileName)));
            afi.FileData.CopyTo(extractFile);
            extractFile.Close();
            afi.FileData.Position = bk;

            var kfi = _kore.LoadFile(new KoreLoadInfo(afi.FileData, afi.FileName) { FileSystem = new VirtualFileSystem(_archiveAdapter, Path.Combine(_tempFolder, _subFolder)) });

            AddTabPage(kfi);
        }
        #endregion

        #region lstFiles
        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateForm();
        }
        #endregion
    }
}
