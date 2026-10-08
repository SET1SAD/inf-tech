namespace QuadcopterApp.ViewModels;

public class PropertyRowViewModel : ViewModelBase
{
    private string _value;

    public PropertyRowViewModel(string name, string typeName, string value)
    {
        Name = name;
        TypeName = typeName;
        _value = value;
    }

    public string Name { get; }

    public string TypeName { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
