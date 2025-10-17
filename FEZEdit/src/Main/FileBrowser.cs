using System;
using System.Collections.Generic;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Extensions;
using FEZEdit.Singletons;
using Godot;

namespace FEZEdit.Main;

public partial class FileBrowser : Control
{
    public enum Options
    {
        FileOpen,
        UnpackRaw,
        UnpackDecompressed,
        UnpackConverted,
        ConvertFromXnb,
        ConvertToXnb,
        PackAssets
    }

    private static readonly StringName ClickedItem = "clicked_item";

    private const string EditedSymbol = "(*)";

    private static readonly Dictionary<Options, ContentSaver.RepackingMode> OptionModes = new()
    {
        [Options.UnpackRaw] = ContentSaver.RepackingMode.UnpackRaw,
        [Options.UnpackDecompressed] = ContentSaver.RepackingMode.UnpackDecompressXnb,
        [Options.UnpackConverted] = ContentSaver.RepackingMode.UnpackConverted,
        [Options.ConvertFromXnb] = ContentSaver.RepackingMode.ConvertFromXnb,
        [Options.ConvertToXnb] = ContentSaver.RepackingMode.ConvertToXnb,
        [Options.PackAssets] = ContentSaver.RepackingMode.PackAssets
    };

    public event Action<string> FileMaterialized;

    public event Action<string> FileClosed;

    public event Action<string> FileShowed;

    public event Action<string, string, ContentSaver.RepackingMode> FileOrDirectoryRepacked;

    [Export] private IconsResource _icons;

    private AddressBar _addressBar;

    private TreeFilter _treeFilter;

    private Tree _filesTree;

    private VBoxContainer _openFilesContainer;

    private PopupMenu _contextMenu;

    private TreeItem _rootItem;

    private FileDialog _saveToFolderDialog;

    private string _sourcePath;

    private ContentSaver.RepackingMode _repackMode;

    private readonly Dictionary<string, PanelContainer> _openItems = new();

    public override void _Ready()
    {
        _addressBar = GetNode<AddressBar>("%AddressBar");
        _addressBar.IsPathValid = IsPathValid;
        _addressBar.AddressChanged += UpdateTreeAgainstAddressBar;

        _treeFilter = GetNode<TreeFilter>("%TreeFilter");

        _filesTree = GetNode<Tree>("%FilesTree");
        _filesTree.SetColumnExpand(0, true);
        _filesTree.ItemSelected += UpdateAddressBar;
        _filesTree.ItemActivated += MaterializeAsset;
        _filesTree.GuiInput += ShowContextMenu;

        _openFilesContainer = GetNode<VBoxContainer>("%OpenFilesContainer");

        _contextMenu = GetNode<PopupMenu>("%ContextMenu");
        _contextMenu.IdPressed += SelectActionOnAsset;
        _contextMenu.Clear(true);

        _saveToFolderDialog = GetNode<FileDialog>("%SaveToFolderDialog");
        _saveToFolderDialog.DirSelected += OnSavingFolderSelected;
    }

    public void AddRoot(string root)
    {
        _rootItem = _filesTree.CreateItem();
        _rootItem.SetText(0, root);
        _rootItem.SetMetadata(0, true);
        _treeFilter.RootItem = _rootItem;
    }

    public void AddFile(string path, Texture2D icon)
    {
        var pathSegments = path.Replace("\\", "/")
            .Trim('/')
            .Split('/')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (pathSegments.Length == 0)
        {
            return;
        }

        var stack = new Stack<(TreeItem item, int depth)>();
        stack.Push((_rootItem, -1));

        var currentParent = _rootItem;
        var currentDepth = 0;

        for (int i = 0; i < pathSegments.Length; i++)
        {
            var segment = pathSegments[i];
            var isDirectory = i != pathSegments.Length - 1;

            while (stack.Count > 0 && stack.Peek().depth >= currentDepth)
            {
                stack.Pop();
            }

            if (stack.Count > 0)
            {
                currentParent = stack.Peek().item;
            }

            if (currentParent.TryFindChildByText(segment, out var existingItem))
            {
                stack.Push((existingItem, currentDepth));
                currentParent = existingItem;
                currentDepth++;
                continue;
            }

            var newItem = _filesTree.CreateItem(currentParent);
            newItem.SetText(0, segment);
            newItem.SetMetadata(0, isDirectory);
            newItem.SetCollapsed(true);
            newItem.SetIcon(0, icon ?? _icons.File);
            newItem.SetIconMaxWidth(0, 24);

            if (isDirectory)
            {
                newItem.SetIcon(0, _icons.Folder);
                newItem.SetIconModulate(0, _icons.FolderColor);
            }

            stack.Push((newItem, currentDepth));
            currentParent = newItem;
            currentDepth++;
        }
    }

    public void RefreshFiles()
    {
        _treeFilter.SortTreeItem(_rootItem!);
        _addressBar.SetCurrentDirectory(_rootItem.GetText(0), false);
    }

    public void ClearFiles()
    {
        _filesTree.Clear();
        _rootItem = null;
    }

    public void ShowOpenFile(string path, Texture2D icon)
    {
        if (_openItems.ContainsKey(path))
        {
            throw new ArgumentException($"The path is already open: {path}");
        }

        var panelContainer = new PanelContainer { Name = path };
        _openFilesContainer.AddChild(panelContainer);
        _openItems.Add(path, panelContainer);

        var hBoxContainer = new HBoxContainer();
        panelContainer.AddChild(hBoxContainer, true);

        var pathButton = new Button
        {
            Text = path,
            Icon = icon,
            IconAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = HorizontalAlignment.Left
        };
        pathButton.Pressed += () => FileShowed?.Invoke(path);
        hBoxContainer.AddChild(pathButton);

        var closeButton = new Button { Icon = _icons.CloseFile, IconAlignment = HorizontalAlignment.Center };
        closeButton.Pressed += () =>
        {
            if (_openItems.Remove(path))
            {
                panelContainer.QueueFree();
                FileClosed?.Invoke(path);
            }
        };
        hBoxContainer.AddChild(closeButton);
    }

    public void SetOpenFileAsEdited(string path, bool edited)
    {
        if (!_openItems.TryGetValue(path, out var panel))
        {
            throw new ArgumentException($"Open file not found: {path}");
        }

        var pathButton = panel.GetChild(0).GetChild<Button>(0);
        if (edited && !pathButton.Text.Contains(EditedSymbol))
        {
            pathButton.Text += EditedSymbol;
        }
        else if (!edited && pathButton.Text.Contains(EditedSymbol))
        {
            pathButton.Text = pathButton.Text.Replace(EditedSymbol, string.Empty);
        }
    }

    private bool IsPathValid(string path)
    {
        return string.IsNullOrEmpty(path) || _rootItem!.TryFindChildByText(path, out _);
    }

    private void UpdateTreeAgainstAddressBar(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!_rootItem.TryFindChildByText(path, out var targetItem))
        {
            return;
        }

        var current = targetItem;
        while (current != null && current != _rootItem)
        {
            current.SetCollapsed(false);
            current = current.GetParent();
        }

        _filesTree.SetSelected(targetItem, 0);
        _filesTree.EnsureCursorIsVisible();
    }

    private void MaterializeAsset()
    {
        var item = _filesTree.GetSelected();
        var path = item.GetFullPath(excludeRoot: true);
        FileMaterialized?.Invoke(path);
    }

    private void UpdateAddressBar()
    {
        var item = _filesTree.GetSelected();
        if (item.GetChildCount() == 0) item = item.GetParent();
        var path = item.GetFullPath();
        _addressBar.SetCurrentDirectory(path);
    }

    private void ShowContextMenu(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } mouseButton)
        {
            return;
        }

        var clickedItem = _filesTree.GetItemAtPosition(mouseButton.Position);
        if (clickedItem == null)
        {
            return;
        }

        var path = clickedItem.GetFullPath();
        (_, string extension) = path.SplitAtExtension();
        var isFile = clickedItem.GetChildCount() == 0;

        _contextMenu.Clear(true);

        if (isFile)
        {
            _contextMenu.AddItem(Tr("Open File"), (int)Options.FileOpen);
            _contextMenu.AddSeparator();
        }

        if (ContentSaver.CanConvert)
        {
            if (isFile)
            {
                if (extension == ".xnb")
                {
                    _contextMenu.AddItem(Tr("Convert from XNB"), (int)Options.ConvertFromXnb);
                }
                else
                {
                    _contextMenu.AddItem(Tr("Convert Back to XNB"), (int)Options.ConvertToXnb);
                }
            }
            else
            {
                _contextMenu.AddItem(Tr("Convert from XNB"), (int)Options.ConvertFromXnb);
                _contextMenu.AddItem(Tr("Convert Back to XNB"), (int)Options.ConvertToXnb);
                _contextMenu.AddItem(Tr("Pack Assets to PAK"), (int)Options.PackAssets);
            }
        }
        else
        {
            _contextMenu.AddItem(Tr("Unpack to Raw"), (int)Options.UnpackRaw);
            _contextMenu.AddItem(Tr("Unpack and Decompress"), (int)Options.UnpackDecompressed);
            _contextMenu.AddItem(Tr("Unpack to Converted"), (int)Options.UnpackConverted);
        }

        _contextMenu.SetMeta(ClickedItem, clickedItem);
        _contextMenu.Position = (Vector2I)GetGlobalMousePosition();
        _contextMenu.Popup();
    }

    private void SelectActionOnAsset(long id)
    {
        var clickedItem = _contextMenu.GetMeta(ClickedItem).As<TreeItem>();
        if (clickedItem == null)
        {
            return;
        }

        var path = clickedItem.GetFullPath(excludeRoot: true);
        var option = (Options)id;
        if (option == Options.FileOpen)
        {
            FileMaterialized?.Invoke(path);
            return;
        }

        _sourcePath = path;
        _repackMode = OptionModes[option];
        _saveToFolderDialog.CurrentDir = Settings.LastSaveFolder;
        _saveToFolderDialog.PopupCentered();
    }

    private void OnSavingFolderSelected(string dir)
    {
        FileOrDirectoryRepacked?.Invoke(_sourcePath, dir, _repackMode);
        Settings.LastSaveFolder = dir;
        Settings.Save();
    }
}