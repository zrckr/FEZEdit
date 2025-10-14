using System;
using System.IO;
using System.Linq;
using System.Threading;
using FEZEdit.Core;
using FEZEdit.Interface.Editors;
using FEZEdit.Loaders;
using Godot;
using Serilog;

namespace FEZEdit.Interface;

public partial class Main : Control
{
    private static readonly ILogger Logger = LoggerFactory.Create<PakLoader>();

    [Export] private IconsResource _icons;

    [Export] private EditorFactory _editors;

    private MainMenu _mainMenu;

    private FileBrowser _fileBrowser;

    private Editor _editor;

    private ILoader _loader;

    private bool _disabled;

    public override void _EnterTree()
    {
        GetWindow().FocusEntered += RefreshFileBrowser;
    }

    public override void _Ready()
    {
        _mainMenu = GetNode<MainMenu>("%MainMenu");
        _mainMenu.WorkingTargetOpened += LoadFilesFromLoader;
        _mainMenu.WorkingTargetClosed += CloseLoader;
        _mainMenu.ThemeSelected += ChangeTheme;

        _fileBrowser = GetNode<FileBrowser>("%FileBrowser");
        _fileBrowser.FileMaterialized += EditFile;
        _fileBrowser.FileOrDirectoryRepacked += RepackFileOrDirectories;

        _editor = GetNode<Editor>("%Editor");
    }

    private void AttachEditor(Editor editorInstance)
    {
        var parent = _editor.GetParent<Control>();
        var index = _editor.GetIndex();

        _editor.QueueFree();
        parent.AddChild(editorInstance);
        parent.MoveChild(editorInstance, index);

        _editor = editorInstance;
        _editor.History = new EditorHistory();
        _mainMenu.History = _editor.History;
    }

    private void LoadFilesFromLoader(FileSystemInfo workingTarget)
    {
        new Thread(() =>
        {
            try
            {
                ILoader loader;
                switch (workingTarget)
                {
                    case FileInfo fileInfo:
                        Settings.CurrentFolder = fileInfo.DirectoryName;
                        loader = PakLoader.Open(workingTarget);
                        _disabled = true;
                        break;
                    
                    case DirectoryInfo:
                        Settings.CurrentFolder = workingTarget.FullName;
                        loader = FolderLoader.Open(workingTarget);
                        _disabled = false;
                        break;
                    
                     default:
                        throw new ArgumentOutOfRangeException(nameof(workingTarget));
                }
                
                CloseLoader();
                Callable.From(() =>
                {
                    _loader = loader;
                    PopulateFileBrowser(loader);
                    EventBus.Success("Loaded: {0}", loader.Root);
                }).CallDeferred();
            }
            catch (Exception exception)
            {
                EventBus.Error("Failed to open: {0}", workingTarget.Name);
                Logger.Error(exception, "Failed to open '{0}'", workingTarget.FullName);
            }
        }).Start();
    }

    private void RefreshFileBrowser()
    {
        if (_loader == null)
        {
            return;
        }
        
        new Thread(() =>
        {
            try
            {
                Callable.From(() =>
                {
                    _loader.RefreshFiles();
                    PopulateFileBrowser(_loader);
                }).CallDeferred();
            }
            catch (Exception exception)
            {
                EventBus.Error("Failed to refresh: {0}", _loader.Root);
                Logger.Error(exception, "Failed to refresh '{0}'", _loader.Root);
            }
        }).Start();
    }
    
    private void PopulateFileBrowser(ILoader loader)
    {
        var count = loader.GetFiles().Count();
        var progress = new ProgressValue(0, 0, count, 1);
        EventBus.Progress(progress);

        _fileBrowser.ClearFiles();
        _fileBrowser.AddRoot(loader.Root);
        foreach (var path in loader.GetFiles())
        {
            var icon = loader.GetIcon(path, _icons);
            _fileBrowser.AddFile(path, icon);
            progress.Next();
            EventBus.Progress(progress);
        }

        _fileBrowser.RefreshFiles();
        _fileBrowser.CanConvert = loader is FolderLoader;
    }

    private void CloseLoader()
    {
        Callable.From(() =>
        {
            _loader = null;
            _fileBrowser.ClearFiles();
            AttachEditor(_editors.EmptyEditor);
        }).CallDeferred();
    }

    private void RepackFileOrDirectories(string source, string targetDirectory, RepackingMode mode)
    {
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            _loader.RepackAsset(source, targetDirectory, mode);
        }).Start();
    }

    private void EditFile(string file)
    {
        new Thread(() =>
        {
            try
            {
                EventBus.Progress(ProgressValue.Single);
                var @object = _loader.LoadAsset(file);

                Callable.From(() =>
                {
                    try
                    {
                        if (!_editors.TryGetEditor(@object.GetType(), out var editor))
                        {
                            AttachEditor(_editors.UnsupportedEditor);
                            return;
                        }
                        
                        editor.Loader = _loader;
                        editor.Value = @object;
                        AttachEditor(editor);
                        editor.Disabled = _disabled;
                        EventBus.Progress(ProgressValue.Complete);
                        EventBus.Success("Opened: {0}", file);
                    }
                    catch (Exception exception)
                    {
                        AttachEditor(_editors.EmptyEditor);
                        EventBus.Progress(ProgressValue.Complete);
                        EventBus.Error("Failed to open: {0}", file);
                        Logger.Error(exception, "Failed to open '{0}'", file);
                    }
                }).CallDeferred();
            }
            catch (Exception exception)
            {
                EventBus.Error("Failed to load asset: {0}", file);
                Logger.Error(exception, "Failed to load asset '{0}'", file);
            }
        }).Start();
    }

    private void ChangeTheme(Theme theme)
    {
        SetDeferred(Control.PropertyName.Theme, theme);
    }
}