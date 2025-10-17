using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FEZEdit.Core;
using FEZEdit.Editors;
using FEZEdit.Extensions;
using FEZEdit.Loaders;
using Godot;
using Serilog;

namespace FEZEdit.Main;

public partial class Main : Control
{
    private static readonly ILogger Logger = LoggerFactory.Create<PakLoader>();

    [Export] private IconsResource _icons;

    [Export] private EditorFactory _editors;

    private MainMenu _mainMenu;

    private FileBrowser _fileBrowser;

    private Editor _currentEditor;

    private ILoader _loader;

    private bool _disabled;

    private string _currentFilePath;

    private Control _editorContainer;

    private readonly Dictionary<string, Editor> _openEditors = new();

    public override void _EnterTree()
    {
        GetWindow().FocusEntered += RefreshFileBrowser;
    }

    public override void _Ready()
    {
        _mainMenu = GetNode<MainMenu>("%MainMenu");
        _mainMenu.WorkingTargetOpened += LoadFilesFromLoader;
        _mainMenu.WorkingTargetClosed += CloseLoader;
        _mainMenu.WorkingFilePathRequested += RequestFilePath;
        _mainMenu.WorkingFileSaved += SaveFile;
        _mainMenu.ThemeSelected += ChangeTheme;
        _mainMenu.UndoRedoRequested += () => _currentEditor?.UndoRedo;

        _fileBrowser = GetNode<FileBrowser>("%FileBrowser");
        _fileBrowser.FileMaterialized += EditFile;
        _fileBrowser.FileShowed += ShowFile;
        _fileBrowser.FileClosed += CloseFile;
        _fileBrowser.FileOrDirectoryRepacked += RepackFileOrDirectories;

        _editorContainer = GetNode<Control>("%EditorContainer");
        AttachEditor(_editors.EmptyEditor);
    }

    private void AttachEditor(Editor editor)
    {
        if (_editorContainer.GetChildCount() > 0)
        {
            var currentChild = _editorContainer.GetChild(0);
            _editorContainer.RemoveChild(currentChild);
            if (!_openEditors.ContainsValue(currentChild as Editor))
            {
                currentChild.QueueFree();
            }
        }

        _editorContainer.AddChild(editor);
        _currentEditor = editor;
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
            var extension = loader.GetFileExtension(path);
            var icon = loader.GetIcon(path, _icons);
            _fileBrowser.AddFile(path + extension, icon);
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
            foreach (var editor in _openEditors.Values)
            {
                if (editor.GetParent() == _editorContainer)
                {
                    _editorContainer.RemoveChild(editor);
                }

                editor.QueueFree();
            }

            _openEditors.Clear();

            _loader = null;
            _currentFilePath = null;
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
                (file, string _) = file.SplitAtExtension();
                var @object = _loader.LoadAsset(file);
                var icon = _loader.GetIcon(file, _icons);

                Callable.From(() =>
                {
                    if (_openEditors.ContainsKey(file))
                    {
                        SwitchToEditor(file);
                        EventBus.Progress(ProgressValue.Complete);
                        return;
                    }

                    try
                    {
                        if (!_editors.TryGetEditor(@object.GetType(), out var editor))
                        {
                            AttachEditor(_editors.UnsupportedEditor);
                            return;
                        }

                        editor.Loader = _loader;
                        editor.Value = @object;
                        editor.ValueChanged += OnEditorValueChanged;
                        _openEditors[file] = editor;
                        SwitchToEditor(file);
                        _fileBrowser.ShowOpenFile(file, icon);

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

    private void SwitchToEditor(string filePath)
    {
        if (!_openEditors.TryGetValue(filePath, out var editor))
        {
            return;
        }

        if (_currentFilePath != null &&
            _openEditors.TryGetValue(_currentFilePath, out Editor currentEditor) &&
            currentEditor.GetParent() == _editorContainer)
        {
            _editorContainer.RemoveChild(currentEditor);
        }
        else if (_currentFilePath == null && _editorContainer.GetChildCount() > 0)
        {
            var currentChild = _editorContainer.GetChild(0);
            _editorContainer.RemoveChild(currentChild);
            if (!_openEditors.ContainsValue(currentChild as Editor))
            {
                currentChild.QueueFree();
            }
        }

        _editorContainer.AddChild(editor);
        _currentFilePath = filePath;
        _currentEditor = editor;
        editor.Loader = _loader;
        editor.Disabled = _disabled;
    }

    private void ShowFile(string file)
    {
        Callable.From(() =>
        {
            if (_openEditors.ContainsKey(file))
            {
                SwitchToEditor(file);
            }
        }).CallDeferred();
    }

    private void CloseFile(string file)
    {
        Callable.From(() =>
        {
            if (_openEditors.Remove(file, out var editor))
            {
                editor.ValueChanged -= OnEditorValueChanged;
                if (file == _currentFilePath)
                {
                    _editorContainer.RemoveChild(editor);
                    SwitchToNextAvailableEditor(file);
                }

                editor.QueueFree();
            }
        }).CallDeferred();
    }

    private void SwitchToNextAvailableEditor(string closingFilePath)
    {
        var remainingEditors = _openEditors.Where(kvp => kvp.Key != closingFilePath).ToList();
        if (remainingEditors.Count > 0)
        {
            var nextFile = remainingEditors[0].Key;
            SwitchToEditor(nextFile);
        }
        else
        {
            AttachEditor(_editors.EmptyEditor);
            _currentFilePath = null;
        }
    }
    
    private void OnEditorValueChanged()
    {
        Callable.From(() =>
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                // Only mark the currently active file as edited
                _fileBrowser.SetOpenFileAsEdited(_currentFilePath, true);
            }
        }).CallDeferred();
    }

    private string RequestFilePath()
    {
        return _loader != null
            ? _loader.GetFilePath(_currentFilePath)
            : System.Environment.CurrentDirectory;
    }

    private void SaveFile(string path)
    {
        if (_currentEditor?.Value == null)
        {
            return;
        }

        new Thread(() =>
        {
            _loader.SaveAsset(_currentEditor.Value, path);
            Callable.From(() =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    _fileBrowser.SetOpenFileAsEdited(_currentFilePath, false);
                }
                RefreshFileBrowser();
            }).CallDeferred();
        }).Start();
    }

    private void ChangeTheme(Theme theme)
    {
        SetDeferred(Control.PropertyName.Theme, theme);
    }
}