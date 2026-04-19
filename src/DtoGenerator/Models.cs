using System.Collections.Immutable;

namespace DtoGenerator;

/// <summary>A single property as it will appear in the generated DTO.</summary>
internal record PropertyData(
    string SourceName,        // original property name on the source class
    string GeneratedName,     // C# name in the DTO (may differ via [DtoName])
    string TypeName,          // fully-qualified type string for the DTO property
    string? JsonName,         // if set, emit [JsonPropertyName("...")]
    bool Flatten,             // [DtoFlatten] was applied
    ImmutableArray<PropertyData> FlattenedProperties  // inner properties when Flatten=true
);

/// <summary>Everything needed to emit one DTO class and its mapper method.</summary>
internal record DtoData(
    string DtoName,
    string DtoNamespace,
    DtoMode Mode,
    bool WithMapping,
    ImmutableArray<PropertyData> Properties
);

/// <summary>All DTOs derived from a single source class.</summary>
internal record ClassData(
    string SourceClassName,
    string SourceNamespace,
    bool IsPartial,
    ImmutableArray<DtoData> Dtos
);

internal enum DtoMode { OptOut = 0, OptIn = 1 }
