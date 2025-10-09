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
    }

    private void LoadFilesFromLoader(FileSystemInfo workingTarget)
    {
        new Thread(() =>
        {
            try
            {
                ILoader loader = workingTarget switch
                {
                    FileInfo => PakLoader.Open(workingTarget),
                    DirectoryInfo => FolderLoader.Open(workingTarget),
                    _ => throw new ArgumentOutOfRangeException(nameof(workingTarget))
                };

                var files = loader.GetFiles().ToArray();
                var progress = new ProgressValue(0, 0, files.Length, 1);
                EventBus.Progress(progress);

                CloseLoader();
                Callable.From(() =>
                {
                    _loader = loader;
                    _fileBrowser.AddRoot(_loader.Root);
                    foreach (var path in _loader.GetFiles())
                    {
                        var icon = _loader.GetIcon(path, _icons);
                        _fileBrowser.AddFile(path, icon);
                        progress.Next();
                        EventBus.Progress(progress);
                    }

                    _fileBrowser.RefreshFiles();
                    _fileBrowser.CanConvert = _loader is FolderLoader;
                    EventBus.Success("Opened: {0}", workingTarget.FullName);
                }).CallDeferred();
            }
            catch (Exception exception)
            {
                EventBus.Error("Failed to open: {0}", workingTarget.Name);
                Logger.Error(exception, "Failed to open '{0}'", workingTarget.FullName);
            }
        }).Start();
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

                        editor.Value = @object;
                        AttachEditor(editor);
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