namespace DtoGenerator.Attributes;

/// <summary>
/// Excludes a property from one or all generated DTOs.
/// In OptOut mode this is the primary exclusion mechanism.
/// Using this in OptIn mode produces a compiler warning (the property wouldn't be included anyway).
/// DtoIgnore always overrides DtoInclude.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DtoIgnoreAttribute : Attribute
{
    /// <summary>
    /// The DTO to exclude this property from.
    /// When omitted the property is excluded from all DTOs.
    /// </summary>
    public string? DtoName { get; }

    /// <summary>Excludes the property from all generated DTOs.</summary>
    public DtoIgnoreAttribute() { }

    /// <summary>Excludes the property from the specified DTO only.</summary>
    public DtoIgnoreAttribute(string dtoName) => DtoName = dtoName;
}
