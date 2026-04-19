namespace DtoGenerator.Attributes;

/// <summary>
/// Includes a property in one or all generated DTOs.
/// Only meaningful in OptIn mode — in OptOut mode this produces a compiler warning.
/// DtoIgnore always overrides DtoInclude.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DtoIncludeAttribute : Attribute
{
    /// <summary>
    /// The DTO to include this property in.
    /// When omitted the property is included in all DTOs.
    /// </summary>
    public string? DtoName { get; }

    /// <summary>Includes the property in all generated DTOs.</summary>
    public DtoIncludeAttribute() { }

    /// <summary>Includes the property in the specified DTO only.</summary>
    public DtoIncludeAttribute(string dtoName) => DtoName = dtoName;
}
