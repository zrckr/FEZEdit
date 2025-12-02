using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FEZEdit.Content;
using Godot;
using Array = Godot.Collections.Array;

namespace FEZEdit.Editors.Eddy;

public partial class AssetBrowser : Control
{
    private enum DisplayMode
    {
        Thumbnail,
        List
    }

    public event Action<string, AssetType> AssetSelected;

    public bool Disabled
    {
        set
        {
            _assetsOption.Disabled = value;
            _searchBox.Editable = !value;
            for (int i = 0; i < _assetList.ItemCount; i++)
            {
                _assetList.SetItemDisabled(i, value);
            }
        }
    }

    public string CurrentTrileSetName
    {
        get => _currentTrileSetName;
        set
        {
            if (_currentTrileSetName != value)
            {
                _currentTrileSetName = value;
                LoadAssetPreviews(AssetType.TrileSet);
            }
        }
    }

    private OptionButton _assetsOption;

    private LineEdit _searchBox;
    
    private Button _thumbnailButton;
    
    private Button _listButton;

    private ItemList _assetList;

    private Label _infoLabel;

    private DisplayMode _currentDisplayMode;
    
    private string _currentTrileSetName;

    public override void _Ready()
    {
        InitializeSearchLine();
        InitializeModeButtons();
        InitializeAssetList();
    }

    private void InitializeSearchLine()
    {
        _searchBox = GetNode<LineEdit>("%SearchBox");
        _searchBox.TextChanged += _ => LoadAssetPreviews((AssetType)_assetsOption.Selected);
    }
    
    private void InitializeModeButtons()
    {
        _thumbnailButton = GetNode<Button>("%ThumbnailMode");
        _thumbnailButton.Pressed += () =>
        {
            _currentDisplayMode = DisplayMode.Thumbnail;
            LoadAssetPreviews((AssetType)_assetsOption.Selected);
        };
        
        _listButton = GetNode<Button>("%ListMode");
        _listButton.Pressed += () =>
        {
            _currentDisplayMode = DisplayMode.List;
            LoadAssetPreviews((AssetType)_assetsOption.Selected);
        };
    }

    private void InitializeAssetList()
    {
        _assetList = GetNode<ItemList>("%AssetList");
        _assetList.ItemSelected += index =>
        {
            var metadata = _assetList.GetItemMetadata((int)index).AsGodotArray();
            var assetPath = metadata[0].ToString();
            var assetType = (AssetType)metadata[1].AsInt32();
            AssetSelected?.Invoke(assetPath, assetType);
        };

        _infoLabel = GetNode<Label>("%InfoLabel");
        _infoLabel.Hide();

        _assetsOption = GetNode<OptionButton>("%AssetsOption");
        _assetsOption.ItemSelected += type => LoadAssetPreviews((AssetType)type);
    }

    private void LoadAssetPreviews(AssetType assetType)
    {
        _assetList.Clear();
        switch (_currentDisplayMode)
        {
            case DisplayMode.Thumbnail:
                _assetList.IconMode = ItemList.IconModeEnum.Top;
                _assetList.FixedColumnWidth = Mathf.RoundToInt(ContentPreviewer.PreviewSize * 1.5f);
                break;
            
            case DisplayMode.List:
                _assetList.IconMode = ItemList.IconModeEnum.Left;
                _assetList.FixedColumnWidth = 0;
                break;
        }

        var folder = assetType switch
        {
            AssetType.TrileSet => Path.Combine("trile sets", _currentTrileSetName),
            AssetType.ArtObject => "art objects",
            AssetType.BackgroundPlane => "background planes",
            AssetType.NonPlayableCharacter => "character animations",
            _ => throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null)
        };

        var files = ContentLoader.GetFiles(folder).ToList();
        if (assetType == AssetType.NonPlayableCharacter)
        {
            var characters = new HashSet<string>();
            foreach (var file in files.ToList())
            {
                var character = file.Split('\\')[1];
                if (!characters.Add(character) || character == "gomez" || file.Contains("metadata"))
                {
                    files.Remove(file);
                }
            }
        }

        foreach (var file in files)
        {
            var fileName = file.GetFile();
            if (string.IsNullOrEmpty(_searchBox.Text) ||
                 fileName.Contains(_searchBox.Text, StringComparison.InvariantCultureIgnoreCase))
            {
                ContentPreviewer.QueueContentPreview(file, (path, preview, _) =>
                {
                    var idx = _assetList.AddItem(preview.ResourceName);
                    _assetList.SetItemMetadata(idx, new Array { path, (int)assetType });
                    _assetList.SetItemIcon(idx, preview);
                });
            }
        }
        ContentPreviewer.FinishContentPreview(() =>
        {
            SetLoading(false);
        });
        
        SetLoading(true);
    }

    private void SetLoading(bool loading)
    {
        _infoLabel.Visible = loading;
        _assetsOption.Disabled = loading;
        _searchBox.Editable = !loading;
        _assetList.Visible = !loading;
    }
}