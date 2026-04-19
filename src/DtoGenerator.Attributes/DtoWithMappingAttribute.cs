namespace DtoGenerator.Attributes;

/// <summary>
/// Signals that the generated mapper class should be <c>partial</c> and expose
/// a <c>static partial void MapCustom(TSource source, TDto dest)</c> hook per DTO,
/// allowing the developer to add custom mapping logic without touching generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DtoWithMappingAttribute : Attribute { }
