namespace DtoGenerator.Attributes;

/// <summary>
/// Marks a class for DTO generation. Can be applied multiple times to generate
/// multiple independent DTOs from the same source class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateDtoAttribute : Attribute
{
    /// <summary>The name of the DTO class to generate.</summary>
    public string Name { get; }

    /// <summary>
    /// Controls which properties are included by default.
    /// Defaults to <see cref="DtoMode.OptOut"/> (all properties included).
    /// </summary>
    public DtoMode Mode { get; set; } = DtoMode.OptOut;

    /// <summary>
    /// Overrides the generated namespace. Defaults to <c>{source namespace}.DTOs</c>.
    /// </summary>
    public string? Namespace { get; set; }

    public GenerateDtoAttribute(string name) => Name = name;
}
