namespace DtoGenerator.Attributes;

/// <summary>
/// Renames the C# property in the generated DTO. The new name must be a valid
/// C# identifier. For renaming only the JSON serialization key, use <see cref="DtoJsonNameAttribute"/>.
/// Both attributes can be combined on the same property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DtoNameAttribute : Attribute
{
    /// <summary>The C# identifier to use in the generated DTO.</summary>
    public string Name { get; }

    /// <summary>
    /// The DTO this rename applies to.
    /// When omitted the rename applies to all DTOs.
    /// </summary>
    public string? DtoName { get; }

    /// <summary>Renames the property in all generated DTOs.</summary>
    public DtoNameAttribute(string name) => Name = name;

    /// <summary>Renames the property in the specified DTO only.</summary>
    public DtoNameAttribute(string name, string dtoName)
    {
        Name = name;
        DtoName = dtoName;
    }
}
