using Godot;

namespace FEZEdit.Editors.Properties;

public partial class EditorPropertyNullable : EditorProperty
{
    public override bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            _checkBox.Disabled = value;
            if (_editorProperty != null)
            {
                _editorProperty.Disabled = value;
            }
        }
    }
    
    private CheckBox _checkBox;
    
    private Container _propertyContainer;

    private EditorProperty _editorProperty;

    private bool _disabled;

    protected override object GetValue()
    {
        return _editorProperty?.Value;
    }

    protected override void SetValue(object value)
    {
        if (_editorProperty != null)
        {
            _editorProperty.Value = value;
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        _checkBox = GetNode<CheckBox>("%CheckBox");
        _checkBox.Toggled += OnHasValueToggled;
        _propertyContainer = GetNode<Container>("%PropertyContainer");
    }

    private void OnHasValueToggled(bool hasValue)
    {
        if (!hasValue)
        {
            _editorProperty?.QueueFree();
            return;
        }
        
        var type = Type.GetElementType() ?? typeof(object);
        var editor = PropertyFactory.GetEditorProperty(type);
        editor.UndoRedo = UndoRedo;
        editor.ValueChanged += _ => OnValueChanged();
        _propertyContainer.AddChild(editor);
        editor.Value = PropertyInfo?.GetValue(Target);
        editor.Label = string.Empty;
        _editorProperty = editor;
    }

    private void OnValueChanged()
    {
        var oldValue = PropertyInfo?.GetValue(Target);
        var newValue = GetValue();
        if (!newValue.Equals(oldValue))
        {
            RecordValueChange(oldValue, newValue);
            NotifyValueChanged(newValue);
        }
    }
}