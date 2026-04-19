namespace DtoGenerator.Attributes;

public enum DtoMode
{
    /// <summary>All properties are included. Use [DtoIgnore] to exclude.</summary>
    OptOut = 0,

    /// <summary>No properties are included. Use [DtoInclude] to include.</summary>
    OptIn = 1,
}
