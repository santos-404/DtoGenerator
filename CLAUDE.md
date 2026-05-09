# DtoGenerator — Claude Context

## What this project is

A Roslyn incremental source generator that produces DTO classes and mapper extension methods from annotated domain models. Ships as a single NuGet package: `DtoGenerator`.

## Repository layout

```
src/
  DtoGenerator.Attributes/   attribute definitions only — never published independently (IsPackable=false)
  DtoGenerator/              the generator — the only published NuGet package
tests/
  DtoGenerator.Tests/        xUnit tests, runs the generator in-process against synthetic source
```

Two projects exist because the generator references Roslyn APIs (`Microsoft.CodeAnalysis.*`) which must not bleed into the attributes project. The NuGet package bundles both DLLs:

- `analyzers/dotnet/cs/DtoGenerator.dll` — compiler loads this as a plugin at build time
- `lib/netstandard2.0/DtoGenerator.Attributes.dll` — referenced normally by consuming projects

Both target `netstandard2.0` (Roslyn requires it for generators; attributes follow for maximum compatibility).

## Full API

### `[GenerateDto(name)]` — class level, AllowMultiple

| Parameter | Type | Default | Description |
|---|---|---|---|
| `name` | string (required) | — | Name of the DTO class to generate |
| `Mode` | DtoMode | OptOut | OptOut = all properties included; OptIn = none included |
| `Namespace` | string? | `{source namespace}.DTOs` | Override the generated namespace |

### `[DtoIgnore]` — property level, AllowMultiple

- No args → exclude from all DTOs
- `(string dtoName)` → exclude from specific DTO only
- Always wins over `[DtoInclude]`
- Using in OptIn mode produces a compiler warning (property wouldn't be included anyway)

### `[DtoInclude]` — property level, AllowMultiple

- No args → include in all DTOs
- `(string dtoName)` → include in specific DTO only
- Only meaningful in OptIn mode
- Using in OptOut mode produces a compiler warning

### `[DtoName(name)]` — property level, AllowMultiple

- Renames the C# property identifier in the generated DTO
- `(string name)` → apply to all DTOs
- `(string name, string dtoName)` → apply to specific DTO
- Can combine with `[DtoJsonName]` on the same property

### `[DtoJsonName(jsonName)]` — property level, AllowMultiple

- Emits `[JsonPropertyName("...")]` on the generated property
- Does not change the C# identifier
- `(string jsonName)` → apply to all DTOs
- `(string jsonName, string dtoName)` → apply to specific DTO

### `[DtoFlatten]` — property level, AllowMultiple

- Inlines the nested object's properties into the DTO instead of exposing the nested type
- One level by default; deeper nesting handled by placing `[DtoFlatten]` on the inner type's properties
- No args → flatten in all DTOs
- `(string dtoName)` → flatten in specific DTO

### `[DtoWithMapping]` — class level, not AllowMultiple

- Makes the generated mapper class `partial`
- Exposes a `static partial void MapCustom(TSource source, TDto dest)` hook per generated DTO

## Conflict resolution

- `[DtoIgnore]` always overrides `[DtoInclude]` on the same property
- Scoped attribute (with dtoName) takes precedence over global (no dtoName)
- Duplicate property names after transformation → compile-time error
- Invalid DTO name reference → compile-time error

## Decisions made

- **String-based DTO identifiers**: currently names are strings (`"UserDto"`). Strongly typed identifiers (`typeof`) are planned after v1.0.0, once string-based is fully stable.
- **Flattening depth**: one level by default, deeper nesting handled by chaining `[DtoFlatten]` down through inner types. Configurable depth is a future consideration, not yet designed.
- **IIncrementalGenerator**: mandatory. `ISourceGenerator` is deprecated by Roslyn.
- **No runtime dependency**: generator runs entirely at compile time. Consuming projects get zero runtime overhead.

## Versioning and release workflow

- MinVer derives the version from git tags automatically — version numbers are never edited manually in files
- Development happens on `main` until v1.0.0; after that, features go on branches and merge via PRs
- **Pre-v1.0.0**: only GitHub Releases are created; NuGet is not published
- **v1.0.0+**: both GitHub Release and NuGet package are published
- Releases are triggered with the Makefile:
  - `make release` → patch bump
  - `make release BUMP=minor` → minor bump
  - `make release BUMP=major` → major bump

## Coding conventions

- No comments unless the WHY is non-obvious to a reader who has never seen the task
- No defensive error handling for internal invariants — trust compiler and framework guarantees
- No abstractions beyond what the current task needs
- No new features or refactors bundled into a bug fix
