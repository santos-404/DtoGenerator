namespace DtoGenerator.Attributes;

/// <summary>
/// Flattens a nested object's properties into the generated DTO instead of
/// exposing the nested type directly.
/// By default only one level of flattening is applied; deeper nesting requires
/// additional [DtoFlatten] attributes on the inner type's properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DtoFlattenAttribute : Attribute
{
    /// <summary>
    /// The DTO to flatten this property into.
    /// When omitted flattening applies to all DTOs.
    /// </summary>
    public string? DtoName { get; }

    /// <summary>Flattens the property in all generated DTOs.</summary>
    public DtoFlattenAttribute() { }

    /// <summary>Flattens the property in the specified DTO only.</summary>
    public DtoFlattenAttribute(string dtoName) => DtoName = dtoName;
}
