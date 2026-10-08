using System.Reflection;

namespace QuadcopterLibrary;

public static class MechanicCatalog
{
    public static IReadOnlyList<Type> FindMechanicTypes()
    {
        return typeof(IMechanic).Assembly
            .GetTypes()
            .Where(IsMechanicType)
            .OrderBy(type => type.Name)
            .ToList();
    }

    public static MechanicInfoAttribute GetInfo(Type mechanicType)
    {
        ArgumentNullException.ThrowIfNull(mechanicType);

        return mechanicType.GetCustomAttribute<MechanicInfoAttribute>()
            ?? new MechanicInfoAttribute(mechanicType.Name, string.Empty);
    }

    public static IMechanic Create(Type mechanicType, string name, double baseX, double baseY)
    {
        ArgumentNullException.ThrowIfNull(mechanicType);

        if (!IsMechanicType(mechanicType))
        {
            throw new ArgumentException($"Тип {mechanicType.Name} не является механиком.", nameof(mechanicType));
        }

        object? instance = Activator.CreateInstance(mechanicType, name, baseX, baseY);
        return instance as IMechanic
            ?? throw new InvalidOperationException($"Не удалось создать механика типа {mechanicType.Name}.");
    }

    private static bool IsMechanicType(Type type)
    {
        return type.IsClass && !type.IsAbstract && typeof(IMechanic).IsAssignableFrom(type);
    }
}
