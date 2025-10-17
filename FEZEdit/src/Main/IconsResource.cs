using Godot;

namespace FEZEdit.Main;

public partial class IconsResource : Resource
{
    [ExportGroup("Menu Bar")] [Export] public Texture2D About { get; set; }
   
    [Export] public Texture2D Theme { get; set; }
    
    [Export] public Texture2D Language { get; set; }
    
    [Export] public Texture2D Documentation { get; set; }
    
    [Export] public Texture2D Repository { get; set; }
    
    [ExportGroup("Status Bar")] [Export] public Texture2D Info { get; set; }

    [Export] public Texture2D Success { get; set; }

    [Export] public Texture2D Warning { get; set; }

    [Export] public Texture2D Error { get; set; }

    [ExportGroup("File Browser")] [Export] public Texture2D Folder { get; set; }

    [Export] public Texture2D File { get; set; }

    [Export] public Texture2D TextFile { get; set; }

    [Export] public Texture2D XnbFile { get; set; }

    [Export] public Texture2D AudioFile { get; set; }

    [Export] public Texture2D FxFile { get; set; }

    [Export] public Texture2D JsonFile { get; set; }

    [Export] public Texture2D MeshFile { get; set; }

    [Export] public Texture2D FontFile { get; set; }

    [Export] public Texture2D TextureFile { get; set; }

    [Export] public Texture2D AnimatedTextureFile { get; set; }

    [Export] public Texture2D LevelFile { get; set; }

    [Export] public Texture2D MapFile { get; set; }

    [Export] public Texture2D SkyFile { get; set; }
    
    [Export] public Texture2D CloseFile { get; set; }

    [ExportGroup("Actions")] [Export] public Texture2D ActionAdd { get; set; }
    
    [Export] public Texture2D ActionRemove { get; set; }
    
    [Export] public Texture2D ActionEdit { get; set; }
    
    [ExportGroup("Colors")] [Export] public Color FolderColor { get; set; }
}