using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Singletons;
using Godot;
using Serilog;

namespace FEZEdit.Editors.Eddy;

public partial class AssetBrowser : Control
{
    private enum Option
    {
        TrileSet,
        ArtObject,
        BackgroundPlane,
        NonPlayableCharacter
    }
    
    public event Action<string> AssetSelected;

    public bool Disabled
    {
        set
        {
            _assetsOption.Disabled = value;
            _searchLine.Editable = !value;
            foreach (var buttons in _allPreviewButtons.Values)
            {
                buttons.Disabled = value;
            }
        }
    }
    
    public string CurrentTrileSet
    {
        get => _currentTrileSet;
        set
        {
            if (_currentTrileSet != value)
            {
                _currentTrileSet = value;
                LoadAssetPreviews(Option.TrileSet);
            }
        }
    }
    
    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt(_filteredPreviewButtons.Count / (float)ItemsPerPage));
    
    private int ItemsPerPage => Mathf.FloorToInt(_assetsContainer.Size.Y / ContentPreviewer.PreviewSize * 0.9f);

    private OptionButton _assetsOption;

    private LineEdit _searchLine;

    private VBoxContainer _assetsContainer;

    private Button _startPageButton;
    
    private Button _previousPageButton;

    private SpinBox _currentPageBox;
    
    private Button _nextPageButton;
    
    private Button _endPageButton;

    private Label _loadingLabel;

    private string _currentTrileSet;

    private int _currentPage = 1;

    private readonly SortedDictionary<string, Button> _allPreviewButtons = new();
    
    private readonly List<Button> _filteredPreviewButtons = [];

    public override void _Ready()
    {
        InitializeSearchLine();
        InitializeAssetsContainer();
        InitializePagination();
    }

    private void InitializeSearchLine()
    {
        _searchLine = GetNode<LineEdit>("%SearchLine");
        _searchLine.TextChanged += ApplySearchFilter;
    }

    private void InitializeAssetsContainer()
    {
        _assetsContainer = GetNode<VBoxContainer>("%AssetsContainer");

        _loadingLabel = GetNode<Label>("%LoadingLabel");
        _loadingLabel.Hide();

        _assetsOption = GetNode<OptionButton>("%AssetsOption");
        _assetsOption.ItemSelected += option => LoadAssetPreviews((Option)option);
    }

    private void InitializePagination()
    {
        _startPageButton = GetNode<Button>("%StartPageButton");
        _startPageButton.Pressed += () => ShowPage(1);
        
        _previousPageButton = GetNode<Button>("%PreviousPageButton");
        _previousPageButton.Pressed += () => ShowPage(_currentPage - 1);

        _currentPageBox = GetNode<SpinBox>("%CurrentPageBox");
        _currentPageBox.ValueChanged += index => ShowPage((int)index);

        _nextPageButton = GetNode<Button>("%NextPageButton");
        _nextPageButton.Pressed += () => ShowPage(_currentPage + 1);
        
        _endPageButton = GetNode<Button>("%EndPageButton");
        _endPageButton.Pressed += () => ShowPage(TotalPages);
        
        UpdatePaginationControls();
        GetTree().Root.SizeChanged += () => ShowPage(_currentPage);
        VisibilityChanged += () => ShowPage(_currentPage);
    }
    
    private async void ShowPage(int page)
    {
        foreach (var child in _assetsContainer.GetChildren())
        {
            _assetsContainer.RemoveChild(child);
        }
        
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (TotalPages > 0)
        {
            _currentPage = Mathf.Clamp(page, 1, TotalPages); 
        }
        
        var skip = (_currentPage - 1) * ItemsPerPage;
        var previewButtons = _filteredPreviewButtons.Skip(skip).Take(ItemsPerPage);
        
        foreach (var button in previewButtons)
        {
            _assetsContainer.AddChild(button);
        }

        UpdatePaginationControls();
    }

    private void UpdatePaginationControls()
    {
        _currentPageBox.SetValueNoSignal(_currentPage);
        _currentPageBox.Suffix = $"of {TotalPages}";
        _currentPageBox.MinValue = 1;
        _currentPageBox.MaxValue = TotalPages;
        _startPageButton.Disabled = _currentPage == 1;
        _endPageButton.Disabled = _currentPage == TotalPages;
    }

    private void LoadAssetPreviews(Option option)
    {
        foreach (var button in _allPreviewButtons.Values)
        {
            button.QueueFree();
        }

        _allPreviewButtons.Clear();
        _filteredPreviewButtons.Clear();

        var folder = option switch
        {
            Option.TrileSet => Path.Combine("trile sets", _currentTrileSet),
            Option.ArtObject => "art objects",
            Option.BackgroundPlane => "background planes",
            Option.NonPlayableCharacter => "character animations",
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
        };

        var files = ContentLoader.GetFiles(folder).ToList();
        if (option == Option.NonPlayableCharacter)
        {
            var characters = new HashSet<string>();
            foreach (var file in files.ToList())
            {
                var character = file.Split('\\')[1];
                if (!characters.Add(character) || character == "gomez" || files.Contains("metadata"))
                {
                    files.Remove(file);
                }
            }
        }
        
        foreach (var file in files)
        {
            ContentPreviewer.QueueContentPreview(file, (path, preview, _) =>
            {
                var button = new Button
                {
                    Text = preview.ResourceName,
                    Icon = preview,
                    Alignment = HorizontalAlignment.Left,
                    IconAlignment = HorizontalAlignment.Left,
                    VerticalIconAlignment = VerticalAlignment.Center,
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    AutowrapMode = TextServer.AutowrapMode.Word
                };
                
                button.AddThemeConstantOverride("icon_max_width", ContentPreviewer.PreviewSize);
                button.Pressed += () => AssetSelected?.Invoke(path);
                
                _allPreviewButtons.Add(path, button);
            });
        }
        ContentPreviewer.FinishContentPreview(() =>
        {
            SetLoading(false);
            ApplySearchFilter(_searchLine.Text);
        });
        
        SetLoading(true);
    }

    private void ApplySearchFilter(string searchText)
    {
        _filteredPreviewButtons.Clear();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filteredPreviewButtons.AddRange(_allPreviewButtons.Values);
        }
        else
        {
            var searchTerms = searchText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var button in _allPreviewButtons.Values)
            {
                var buttonText = button.Text.ToLowerInvariant();
                if (searchTerms.All(term => buttonText.Contains(term)))
                {
                    _filteredPreviewButtons.Add(button);
                }
            }
        }
        
        _currentPage = 1;
        ShowPage(_currentPage);
    }

    private void SetLoading(bool loading)
    {
        _loadingLabel.Visible = loading;
        _assetsOption.Disabled = loading;
        _searchLine.Editable = !loading;
    }
}