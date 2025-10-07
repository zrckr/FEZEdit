using System;
using System.IO;
using System.Linq;
using FEZEdit.Core;
using FEZEdit.Extensions;
using Godot;

namespace FEZEdit.Interface;

public partial class MainMenu : Control
{
    public enum Options
    {
        FileOpenPak,
        FileOpenFolder,
        FileOpenRecent,
        FileClose,
        FileQuit,

        ViewChangeLanguage,
        ViewChangeTheme,

        HelpAbout
    }

    public event Action<FileSystemInfo> WorkingTargetOpened;

    public event Action WorkingTargetClosed;

    public event Action<Theme> ThemeSelected;

    private const int ClearRecentFilesId = -2;

    [Export] private PackedScene _aboutDialogScene;
    
    [Export] private int _maxRecentFiles;

    [Export] private Godot.Collections.Dictionary<string, string> _themes;

    [Export] private Godot.Collections.Dictionary<string, string> _languages;

    [Export] private IconsResource _icons;

    private PopupMenu _fileMenu;

    private PopupMenu _fileRecentMenu;

    private PopupMenu _viewMenu;

    private PopupMenu _viewThemeMenu;

    private PopupMenu _viewLanguageMenu;

    private PopupMenu _helpMenu;

    private Node _dialogs;

    private FileDialog _pakFileDialog;

    private FileDialog _assetFolderDialog;

    private FileSystemInfo _workingTarget;

    private int _unpackMenuIndex;
    
    private Theme _theme;

    public override void _Ready()
    {
        InitializeFileMenu();
        InitializeViewMenu();
        InitializeHelpMenu();
        InitializeDialogs();
    }

    private void InitializeFileMenu()
    {
        _fileMenu ??= GetNode<PopupMenu>("%FileMenu");
        _fileMenu.IdPressed += OnMenuItemPressed;

        _fileRecentMenu = _fileMenu.GetNode<PopupMenu>("Recent");
        _fileRecentMenu.IndexPressed += OnRecentFileSelected;
        InitializeRecentFiles();

        _fileMenu.Clear(true);
        _fileMenu.AddItem(Tr("Open PAK File..."), (int)Options.FileOpenPak);
        _fileMenu.AddItem(Tr("Open Assets Folder..."), (int)Options.FileOpenFolder);
        _fileMenu.AddSubmenuNodeItem(Tr("Open Recent"), _fileRecentMenu, (int)Options.FileOpenRecent);
        _fileMenu.AddItem(Tr("Close"), (int)Options.FileClose);
        _fileMenu.AddSeparator();
        _fileMenu.AddItem(Tr("Quit"), (int)Options.FileQuit);

        _fileMenu.SetItemShortcut(_fileMenu.GetItemIndex((int)Options.FileOpenPak), Key.O.WithCtrl());
        _fileMenu.SetItemShortcut(_fileMenu.GetItemIndex((int)Options.FileOpenFolder), Key.O.WithCtrlShift());
        _fileMenu.SetItemShortcut(_fileMenu.GetItemIndex((int)Options.FileClose), Key.W.WithCtrlShift());
        _fileMenu.SetItemShortcut(_fileMenu.GetItemIndex((int)Options.FileQuit), Key.W.WithCtrl());
        _fileMenu.SetItemDisabled(_fileMenu.GetItemIndex((int)Options.FileClose), true);
    }

    private void InitializeViewMenu()
    {
        _viewMenu ??= GetNode<PopupMenu>("%ViewMenu");
        _viewThemeMenu ??= _viewMenu.GetNode<PopupMenu>("Theme");
        _viewLanguageMenu ??= _viewMenu.GetNode<PopupMenu>("Language");

        _viewThemeMenu.Clear(true);
        foreach (string theme in _themes.Keys)
        {
            _viewThemeMenu.AddItem(Tr(theme));
        }

        _viewLanguageMenu.Clear(true);
        foreach (string language in _languages.Keys)
        {
            _viewLanguageMenu.AddItem(Tr(language));
        }

        _viewMenu.AddSubmenuNodeItem(Tr("Change Theme"), _viewThemeMenu, (int)Options.ViewChangeTheme);
        _viewMenu.SetItemIcon(0, _icons.Theme);
        
        _viewMenu.AddSubmenuNodeItem(Tr("Change Language"), _viewLanguageMenu, (int)Options.ViewChangeLanguage);
        _viewMenu.SetItemIcon(1, _icons.Language);

        _viewMenu.IdPressed += OnMenuItemPressed;
        _viewThemeMenu.IndexPressed += OnThemeChanged;
        _viewLanguageMenu.IndexPressed += OnLanguageChanged;
    }

    private void InitializeHelpMenu()
    {
        _helpMenu ??= GetNode<PopupMenu>("%HelpMenu");
        _helpMenu.IdPressed += OnMenuItemPressed;

        _helpMenu.Clear(true);
        _helpMenu.AddItem(Tr("About FEZEdit..."), (int)Options.HelpAbout);
        _helpMenu.SetItemIcon(0, _icons.About);
    }

    private void InitializeDialogs()
    {
        _dialogs ??= GetNode<Node>("%Dialogs");
        _theme = ResourceLoader.Load<Theme>(_themes.Values.ElementAt(0));
        
        _pakFileDialog ??= GetNode<FileDialog>("%PakFileDialog");
        _pakFileDialog.FileSelected += OnPakFileSelected;

        _assetFolderDialog ??= GetNode<FileDialog>("%AssetFolderDialog");
        _assetFolderDialog.DirSelected += OnAssetFolderSelected;
    }

    private void InitializeRecentFiles()
    {
        _fileRecentMenu.Clear(true);
        var recentFiles = Settings.RecentFiles;
        if (recentFiles.Count == 0)
        {
            _fileRecentMenu.AddItem(Tr("No recent files"));
            _fileRecentMenu.SetItemDisabled(0, true);
            return;
        }

        for (int i = 0; i < recentFiles.Count; i++)
        {
            _fileRecentMenu.AddItem(recentFiles[i], i);
        }

        _fileRecentMenu.AddSeparator();
        _fileRecentMenu.AddItem("Clear Recent Files", ClearRecentFilesId);
    }

    private void AddPathToRecent(string path)
    {
        var recentFiles = Settings.RecentFiles;
        recentFiles.Remove(path);
        recentFiles.Insert(0, path);

        if (recentFiles.Count > _maxRecentFiles)
        {
            recentFiles.RemoveAt(recentFiles.Count - 1);
        }

        SaveRecent(recentFiles);
    }

    private void SaveRecent(Godot.Collections.Array<string> recentFiles)
    {
        Settings.RecentFiles = recentFiles;
        Settings.Save();
        InitializeRecentFiles();
    }

    private void OnMenuItemPressed(long id)
    {
        switch ((Options)id)
        {
            case Options.FileOpenPak:
                _pakFileDialog.PopupCentered();
                break;

            case Options.FileOpenFolder:
                _assetFolderDialog.PopupCentered();
                break;

            case Options.FileClose:
                _workingTarget = null;
                WorkingTargetClosed?.Invoke();
                break;

            case Options.FileQuit:
                GetTree().Quit();
                break;

            case Options.HelpAbout:
                Callable.From(() =>
                {
                    var dialog = _aboutDialogScene.Instantiate<AboutDialog>();
                    _dialogs.AddChild(dialog);
                    dialog.Theme = _theme;
                    dialog.PopupCentered();
                }).CallDeferred();
                break;

            case Options.ViewChangeLanguage:
            case Options.ViewChangeTheme:
            case Options.FileOpenRecent:
                break;

            default:
                throw new ArgumentException($"Invalid menu option: {id}");
        }
    }

    private void OnPakFileSelected(string file)
    {
        _workingTarget = new FileInfo(file);
        WorkingTargetOpened?.Invoke(_workingTarget);
        AddPathToRecent(file);
        _fileMenu.SetItemDisabled(_fileMenu.GetItemIndex((int)Options.FileClose), false);
    }

    private void OnAssetFolderSelected(string folder)
    {
        _workingTarget = new DirectoryInfo(folder);
        WorkingTargetOpened?.Invoke(_workingTarget);
        AddPathToRecent(folder);
        _fileMenu.SetItemDisabled(_fileMenu.GetItemIndex((int)Options.FileClose), false);
    }

    private void OnRecentFileSelected(long index)
    {
        var id = _fileRecentMenu.GetItemId((int)index);
        if (id == ClearRecentFilesId)
        {
            SaveRecent([]);
            return;
        }

        var recentFiles = Settings.RecentFiles;
        if (index < 0 || index >= recentFiles.Count)
        {
            return;
        }

        var path = recentFiles[(int)index];
        FileSystemInfo info = File.GetAttributes(path).HasFlag(FileAttributes.Directory)
            ? new DirectoryInfo(path)
            : new FileInfo(path);

        if (!info.Exists)
        {
            recentFiles.RemoveAt((int)index);
            SaveRecent(recentFiles);
            return;
        }

        WorkingTargetOpened?.Invoke(info);
        _fileMenu.SetItemDisabled(_fileMenu.GetItemIndex((int)Options.FileClose), false);
    }

    private void OnThemeChanged(long index)
    {
        _theme = ResourceLoader.Load<Theme>(_themes.Values.ElementAt((int)index));
        ThemeSelected?.Invoke(_theme);
    }

    private void OnLanguageChanged(long index)
    {
        TranslationServer.SetLocale(_languages.Values.ElementAt((int)index));
    }
}