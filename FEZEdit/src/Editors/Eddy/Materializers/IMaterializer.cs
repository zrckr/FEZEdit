using FEZEdit.Memento;
using Godot;

namespace FEZEdit.Editors.Eddy.Materializers;

public interface IMaterializer
{
    MementoManager Memento { set; }
    
    GeometryInstance3D GetCursorInstance(string path);

    void CreateInstance(string path, Transform3D emplacement);

    void RemoveInstance(Transform3D emplacement);
    
    string PickInstance(Transform3D emplacement);
}