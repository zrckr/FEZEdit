
using Godot;
using Serilog;

namespace FEZEdit.Main;

public partial class AboutDialog: AcceptDialog
{
    private static readonly ILogger Logger = LoggerFactory.Create<AboutDialog>();
    
    [Export(PropertyHint.File)] private string _programLicense;
    
    [Export(PropertyHint.File)] private string _godotLicense;
    
    [Export(PropertyHint.File)] private string _serilogLicense;

    private Label _versionLabel;

    private RichTextLabel _poweredByLabel;
    
    private RichTextLabel _developedByLabel;
    
    private Tree _groupsTree;

    private TextEdit _licenseText;

    public override void _Ready()
    {
        InitializeVersion();
        InitializeRichTextLabels();
        InitializeGroups();
        InitializeLicense();
        Canceled += QueueFree;
        Confirmed += QueueFree;
    }

    private void InitializeVersion()
    {
        var applicationName = ProjectSettings.GetSetting("application/config/name").AsString();
        var version = ProjectSettings.GetSetting("application/config/version").AsString();
        _versionLabel = GetNode<Label>("%VersionLabel");
        _versionLabel.Text = $"{applicationName} {version}";
    }
    
    private void InitializeRichTextLabels()
    {
        _poweredByLabel = GetNode<RichTextLabel>("%PoweredByLabel");
        _poweredByLabel.MetaClicked += OpenLink;
        
        _developedByLabel = GetNode<RichTextLabel>("%DevelopedByLabel");
        _developedByLabel.MetaClicked += OpenLink;
    }

    private void InitializeGroups()
    {
        _groupsTree = GetNode<Tree>("%GroupsTree");
        _groupsTree.ItemSelected += SelectLicenseText;
        _groupsTree.Clear();
        var rootItem = _groupsTree.CreateItem();

        var programItem = _groupsTree.CreateItem(rootItem);
        programItem.SetText(0, Tr("License"));
        programItem.SetMetadata(0, _programLicense);
        
        var godotItem = _groupsTree.CreateItem(rootItem);
        godotItem.SetText(0, Tr("Godot"));
        godotItem.SetMetadata(0, _godotLicense);
        
        var serilogItem = _groupsTree.CreateItem(rootItem);
        serilogItem.SetText(0, Tr("Serilog"));
        serilogItem.SetMetadata(0, _serilogLicense);
    }

    private void InitializeLicense()
    {
        _licenseText = GetNode<TextEdit>("%LicenseText");
        _licenseText.Text = GetTextFromFile(_programLicense);
    }
    
    private void SelectLicenseText()
    {
        var item = _groupsTree.GetSelected();
        var file = item.GetMetadata(0).AsString();
        _licenseText.Text = GetTextFromFile(file);
    }

    private static string GetTextFromFile(string file)
    {
        var access = FileAccess.Open(file, FileAccess.ModeFlags.Read);
        if (FileAccess.GetOpenError() != Error.Ok)
        {
            Logger.Error("Failed to open file {0}", file);
            return string.Empty;
        }

        return access.GetAsText();
    }
    
    private static void OpenLink(Variant meta)
    {
        OS.ShellOpen(meta.AsString());
    }
}