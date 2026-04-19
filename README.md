# DTO Generator 

## Motivation

In many backend systems, especially those exposing APIs, developers repeatedly create:

* DTO classes
* Mapping logic between domain models and DTOs

This leads to:

* Boilerplate code
* Inconsistent mappings
* Maintenance overhead when models evolve

Existing solutions either:

* Rely on runtime mapping (less transparent, harder to debug), or
* Require manual DTO definitions (verbose and repetitive)

---

## Goal

Provide a **compile-time DTO generation system** that:

* Eliminates boilerplate DTO definitions
* Generates mappings automatically
* Keeps behavior explicit and predictable
* Allows fine-grained control when needed

---

## Core Concept

Developers annotate domain models, and the system:

1. Generates DTO classes
2. Generates mapping functions
3. Applies transformation rules (ignore, include, rename, flatten)

All logic is resolved **at compile time**, ensuring:

* Type safety
* Performance
* Visibility of generated code

---

## Key Design Principles

* **Deterministic**: No hidden runtime behavior
* **Per-DTO isolation**: Each DTO is evaluated independently
* **Minimal API surface**: Only essential annotations
* **Explicit overrides**: Complex cases handled via code, not annotations
* **Safe by default**: Avoid accidental data exposure

---

## Proposed API

### 1. Class-Level Attributes

#### Generate a single DTO

```csharp
[GenerateDto("UserDto", Mode = DtoMode.OptOut)]
```

#### Generate multiple DTOs with independent modes

```csharp
[GenerateDto("UserListDto", Mode = DtoMode.OptOut)]
[GenerateDto("UserDetailDto", Mode = DtoMode.OptIn)]
```

---

### 2. Property-Level Attributes

#### Ignore property

```csharp
[DtoIgnore] // applies to all DTOs
[DtoIgnore("UserListDto")] // scoped
```

---

#### Include property (only meaningful in OptIn)

```csharp
[DtoInclude]
[DtoInclude("UserDetailDto")]
```

---

#### Rename property (C# identifier)

Renames the actual C# property name in the generated DTO. Must follow C# naming rules.

```csharp
[DtoName("FullAddress")]
[DtoName("FullAddress", "UserDetailDto")]
```

---

#### Rename property (JSON serialization only)

Keeps the C# property name unchanged, but emits `[JsonPropertyName(...)]` on the generated property. Use this when the rename is only for API wire format.

```csharp
[DtoJsonName("full_name")]
[DtoJsonName("full_name", "UserDetailDto")]
```

Generated:

```csharp
[JsonPropertyName("full_name")]
public string Name { get; set; }
```

`[DtoName]` and `[DtoJsonName]` can be combined on the same property: the C# name comes from `[DtoName]` and the JSON key from `[DtoJsonName]`.

---

#### Flatten nested object

```csharp
[DtoFlatten]
[DtoFlatten("UserDetailDto")]
```

---

### 3. Custom Mapping Hook

Enable custom logic:

```csharp
[DtoWithMapping]
```

Generated:

```csharp
static partial void MapCustom(User source, UserDto dest);
```

User implementation:

```csharp
static partial void MapCustom(User source, UserDto dest)
{
    dest.FullName = source.FirstName + " " + source.LastName;
}
```

---

## Modes

### OptOut (default)

* All properties included
* Use `[DtoIgnore]` to exclude

### OptIn

* No properties included by default
* Use `[DtoInclude]` to include

---

## Internal Algorithm (Summary)

For each DTO:

1. Determine mode (OptIn / OptOut)
2. Initialize property set:

   * OptOut → all properties
   * OptIn → empty set
3. Apply `[DtoInclude]` -> warning in case of being OptOut 
4. Apply `[DtoIgnore]` (overrides include) -> warning in case of being OptIn
5. Apply global attributes
6. Apply transformations:
   * Rename
   * Flatten
   * Computed fields (optional)
7. Generate DTO class
8. Generate mapping method

---

## Rules & Constraints

* **Per-DTO evaluation**: rules do not interfere across DTOs
* **Conflict resolution**:

  * `DtoIgnore` overrides `DtoInclude`
* **Invalid usage**:

  * Using `[DtoInclude]` in OptOut → discouraged
  * Using `[DtoIgnore]` in OptIn → discouraged
* **Compile-time errors**:

  * Duplicate property names after transformation
  * Invalid DTO references

---

## Example

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

    // C# property renamed to "FullName", JSON key set to "full_name"
    [DtoName("FullName", "UserDetailDto")]
    [DtoJsonName("full_name", "UserDetailDto")]
    public string Name { get; set; }
}
```

---

### Generated DTOs

```csharp
// Namespace: {source namespace}.DTOs  (default)
public class UserListDto
{
    public int Id { get; set; }
    public string Email { get; set; }
}
```

```csharp
public class UserDetailDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; }
}
```

---

## Technical Decisions

### Target Framework
* **Minimum:** .NET 8
* **Primary test target:** .NET 10+
* Only adopt newer API if it has no breaking impact on .NET 8 compatibility.

### Generated Namespace
* **Default:** `{source class namespace}.DTOs`
* **Configurable** via a parameter on `[GenerateDto]`:

```csharp
[GenerateDto("UserDto", Namespace = "MyApp.Contracts")]
```

### Implementation
* Uses **Roslyn Incremental Source Generators** (`IIncrementalGenerator`)
* The generator runs at compile time — no runtime reflection
* Generated files are visible in IDEs and fully type-safe

---

## Future Extensions (Optional)

* Type-safe DTO identifiers (`typeof(UserDto)` instead of strings)
* Nested mapping configuration
* Validation rules
* Integration with API schema generators

---

## Open Questions

1. Should DTO identifiers be string-based or strongly typed?
    firstly string-based only so we are a DTO generator, later on ALSO strongly typed so became ALSO a DTO enhancer
2. Should flattening support deep recursion or just one level?
    should be configurable
