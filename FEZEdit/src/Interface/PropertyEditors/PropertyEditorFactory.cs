using System.Reflection;

namespace FEZEdit.Interface.PropertyEditors;

public static class PropertyEditorFactory
{
    private static readonly IPropertyEditor[] Editors =
    [
        new ArrayPropertyEditor(),
        new BooleanPropertyEditor(),
        new ColorPropertyEditor(),
        new DictionaryPropertyEditor(),
        new EnumPropertyEditor(),
        new FloatPropertyEditor(),
        new IntegerPropertyEditor(),
        new ListPropertyEditor(),
        new QuaternionPropertyEditor(),
        new StringPropertyEditor(),
        new TexturePropertyEditor(),
        new Vector2PropertyEditor(),
        new Vector3PropertyEditor(),
        new Vector4PropertyEditor()
    ];

    public static IPropertyEditor CreateEditor(PropertyInfo property, out bool notFound)
    {
        if (property.PropertyType.IsEnum)
        {
            notFound = false;
            return new EnumPropertyEditor();
        }
        foreach (var editor in Editors)
        {
            if (property.PropertyType.IsAssignableFrom(editor.PropertyType))
            {
                notFound = false;
                return editor;
            }
        }
        notFound = true;
        return new StringPropertyEditor();
    }
}