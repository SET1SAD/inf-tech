namespace QuadcopterLibrary;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MechanicInfoAttribute : Attribute
{
    public MechanicInfoAttribute(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }

    public string Description { get; }
}
