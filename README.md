# DtoGenerator

Annotate your models, get DTOs at build time.

```csharp
[GenerateDto("ProductDto")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }

    [DtoIgnore]
    public string InternalCode { get; set; }
}
```

The generator produces `ProductDto` and a `ToProductDto()` extension method during compilation. No runtime reflection — it's plain generated C# code that shows up in your IDE like anything else.

## Install

```
dotnet add package DtoGenerator
```

> Still working toward v1.0.0. Things work but the API may shift.

## A fuller example

You can generate multiple independent DTOs from one class and control which properties go where:

```csharp
[GenerateDto("UserListDto", Mode = DtoMode.OptOut)]
[GenerateDto("UserDetailDto", Mode = DtoMode.OptIn)]
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }

    [DtoIgnore("UserListDto")]
    [DtoInclude("UserDetailDto")]
    public string Address { get; set; }

    [DtoName("FullName", "UserDetailDto")]
    [DtoJsonName("full_name", "UserDetailDto")]
    public string Name { get; set; }
}
```

`UserListDto` gets `Id` and `Email`. `UserDetailDto` gets everything, with `Name` renamed to `FullName` in C# and serialized as `full_name` in JSON.

## Attributes

| Attribute | Target | What it does |
|---|---|---|
| `[GenerateDto(name)]` | Class | Generate a DTO with this name. Apply multiple times for multiple DTOs. |
| `[DtoIgnore]` | Property | Exclude from all DTOs, or pass a name to scope it. |
| `[DtoInclude]` | Property | Include in all DTOs, or pass a name to scope it. Used in OptIn mode. |
| `[DtoName(name)]` | Property | Rename the C# property in the generated DTO. |
| `[DtoJsonName(key)]` | Property | Emit `[JsonPropertyName]` without renaming the C# property. |
| `[DtoFlatten]` | Property | Inline the nested object's properties instead of the object itself. |
| `[DtoWithMapping]` | Class | Expose a `partial void MapCustom(...)` hook for custom mapping logic. |

## Modes

`Mode = DtoMode.OptOut` (the default) includes all properties — use `[DtoIgnore]` to exclude.

`Mode = DtoMode.OptIn` starts empty — use `[DtoInclude]` to pull in what you need.

Both modes can coexist across DTOs on the same class.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
