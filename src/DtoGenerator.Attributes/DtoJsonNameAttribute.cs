namespace DtoGenerator.Attributes;

/// <summary>
/// Emits a <c>[JsonPropertyName("...")]</c> attribute on the generated DTO property,
/// keeping the C# property name unchanged. Use this when the rename is only for the
/// JSON wire format (e.g. snake_case API responses).
/// For renaming the C# identifier itself, use <see cref="DtoNameAttribute"/>.
/// Both attributes can be combined on the same property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DtoJsonNameAttribute : Attribute
{
    /// <summary>The JSON key to emit via <c>[JsonPropertyName]</c>.</summary>
    public string JsonName { get; }

    /// <summary>
    /// The DTO this JSON name applies to.
    /// When omitted it applies to all DTOs.
    /// </summary>
    public string? DtoName { get; }

    /// <summary>Sets the JSON name in all generated DTOs.</summary>
    public DtoJsonNameAttribute(string jsonName) => JsonName = jsonName;

    /// <summary>Sets the JSON name in the specified DTO only.</summary>
    public DtoJsonNameAttribute(string jsonName, string dtoName)
    {
        JsonName = jsonName;
        DtoName = dtoName;
    }
}
