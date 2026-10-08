using Avalonia.Media;

namespace QuadcopterApp.ViewModels;

public sealed record MechanicTypeOption(Type Type, string Title, string Description, IBrush Accent);

public sealed record InspectableObject(string Title, object Target);
