using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DtoGenerator;

[Generator]
public sealed class DtoSourceGenerator : IIncrementalGenerator
{
    // Fully-qualified attribute names – must match DtoGenerator.Attributes exactly.
    private const string GenerateDtoFqn        = "DtoGenerator.Attributes.GenerateDtoAttribute";
    private const string DtoWithMappingFqn     = "DtoGenerator.Attributes.DtoWithMappingAttribute";
    private const string DtoIgnoreFqn          = "DtoGenerator.Attributes.DtoIgnoreAttribute";
    private const string DtoIncludeFqn         = "DtoGenerator.Attributes.DtoIncludeAttribute";
    private const string DtoNameFqn            = "DtoGenerator.Attributes.DtoNameAttribute";
    private const string DtoJsonNameFqn        = "DtoGenerator.Attributes.DtoJsonNameAttribute";
    private const string DtoFlattenFqn         = "DtoGenerator.Attributes.DtoFlattenAttribute";

    private static readonly SymbolDisplayFormat TypeFormat = new(
        globalNamespaceStyle:    SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle:  SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions:         SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:    SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                               | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    // -------------------------------------------------------------------------
    // Pipeline
    // -------------------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ForAttributeWithMetadataName fires once per [GenerateDto] occurrence.
        // ctx.Attributes contains ALL [GenerateDto] attributes on the target class,
        // so each invocation already has the full picture for that class.
        // We collect everything and deduplicate by class identity.
        var allItems = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateDtoFqn,
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: Transform)
            .Where(x => x is not null)
            .Select((x, _) => x!);

        var deduplicated = allItems
            .Collect()
            .Select((items, _) => Deduplicate(items));

        context.RegisterSourceOutput(deduplicated, (spc, groups) =>
        {
            foreach (var classData in groups)
                Generate(spc, classData);
        });
    }

    // -------------------------------------------------------------------------
    // Transform
    // -------------------------------------------------------------------------

    private static ClassData? Transform(GeneratorAttributeSyntaxContext ctx, System.Threading.CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            return null;

        var className  = classSymbol.Name;
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        var isPartial = classSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(ct))
            .OfType<ClassDeclarationSyntax>()
            .Any(c => c.Modifiers.Any(SyntaxKind.PartialKeyword));

        var hasCustomMapping = classSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == DtoWithMappingFqn);

        // All [GenerateDto] attributes on this class (ctx.Attributes already has them all)
        var generateDtoAttrs = ctx.Attributes;

        // Source properties (instance, readable)
        var sourceProperties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.GetMethod != null)
            .ToArray();

        var dtos = new List<DtoData>(generateDtoAttrs.Length);

        foreach (var attr in generateDtoAttrs)
        {
            ct.ThrowIfCancellationRequested();

            if (attr.ConstructorArguments.Length == 0) continue;
            var dtoName = attr.ConstructorArguments[0].Value as string;
            if (string.IsNullOrWhiteSpace(dtoName)) continue;

            var mode            = DtoMode.OptOut;
            string? customNs    = null;

            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "Mode" && namedArg.Value.Value is int modeVal)
                    mode = (DtoMode)modeVal;
                else if (namedArg.Key == "Namespace" && namedArg.Value.Value is string ns)
                    customNs = ns;
            }

            var dtoNamespace = string.IsNullOrWhiteSpace(customNs)
                ? (string.IsNullOrEmpty(namespaceName) ? "DTOs" : $"{namespaceName}.DTOs")
                : customNs!;

            var properties = BuildProperties(sourceProperties, dtoName!, mode, ct);

            dtos.Add(new DtoData(dtoName!, dtoNamespace, mode, hasCustomMapping, properties));
        }

        return new ClassData(className, namespaceName, isPartial, dtos.ToImmutableArray());
    }

    private static ImmutableArray<PropertyData> BuildProperties(
        IPropertySymbol[] sourceProperties,
        string dtoName,
        DtoMode mode,
        System.Threading.CancellationToken ct)
    {
        var result = new List<PropertyData>();

        foreach (var prop in sourceProperties)
        {
            ct.ThrowIfCancellationRequested();

            var attrs = prop.GetAttributes();

            // --- [DtoIgnore] ---
            bool ignored = attrs.Any(a =>
                a.AttributeClass?.ToDisplayString() == DtoIgnoreFqn &&
                MatchesDtoName(a, dtoName));

            // --- [DtoInclude] ---
            bool included = attrs.Any(a =>
                a.AttributeClass?.ToDisplayString() == DtoIncludeFqn &&
                MatchesDtoName(a, dtoName));

            bool include = mode switch
            {
                DtoMode.OptOut => !ignored,
                DtoMode.OptIn  => included && !ignored,
                _              => false,
            };

            if (!include) continue;

            // --- [DtoName] ---
            var nameAttr = attrs.FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == DtoNameFqn &&
                MatchesDtoName(a, dtoName, argIndex: 1));

            var generatedName = nameAttr?.ConstructorArguments[0].Value as string ?? prop.Name;

            // --- [DtoJsonName] ---
            var jsonNameAttr = attrs.FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == DtoJsonNameFqn &&
                MatchesDtoName(a, dtoName, argIndex: 1));

            var jsonName = jsonNameAttr?.ConstructorArguments[0].Value as string;

            // --- [DtoFlatten] ---
            bool flatten = attrs.Any(a =>
                a.AttributeClass?.ToDisplayString() == DtoFlattenFqn &&
                MatchesDtoName(a, dtoName));

            var flattenedProps = ImmutableArray<PropertyData>.Empty;

            if (flatten && prop.Type is INamedTypeSymbol nestedType)
            {
                flattenedProps = nestedType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => !p.IsStatic && p.GetMethod != null)
                    .Select(p => new PropertyData(
                        SourceName:          p.Name,
                        GeneratedName:       p.Name,
                        TypeName:            p.Type.ToDisplayString(TypeFormat),
                        JsonName:            null,
                        Flatten:             false,
                        FlattenedProperties: ImmutableArray<PropertyData>.Empty))
                    .ToImmutableArray();
            }

            result.Add(new PropertyData(
                SourceName:          prop.Name,
                GeneratedName:       generatedName,
                TypeName:            prop.Type.ToDisplayString(TypeFormat),
                JsonName:            jsonName,
                Flatten:             flatten,
                FlattenedProperties: flattenedProps));
        }

        return result.ToImmutableArray();
    }

    /// <summary>
    /// Returns true when the attribute's optional DTO-name argument (at <paramref name="argIndex"/>)
    /// is absent (global) or matches <paramref name="dtoName"/>.
    /// </summary>
    private static bool MatchesDtoName(AttributeData attr, string dtoName, int argIndex = 0)
    {
        if (attr.ConstructorArguments.Length <= argIndex)
            return true; // no scoping arg → applies globally

        var val = attr.ConstructorArguments[argIndex].Value as string;
        return val is null || val == dtoName;
    }

    // -------------------------------------------------------------------------
    // Deduplication
    // -------------------------------------------------------------------------

    /// <summary>
    /// For a class with N [GenerateDto] attributes the pipeline fires N times,
    /// each time returning the same ClassData. Keep only the first occurrence.
    /// </summary>
    private static ImmutableArray<ClassData> Deduplicate(ImmutableArray<ClassData> items)
    {
        var seen  = new HashSet<string>();
        var result = new List<ClassData>(items.Length);

        foreach (var item in items)
        {
            var key = $"{item.SourceNamespace}.{item.SourceClassName}";
            if (seen.Add(key))
                result.Add(item);
        }

        return result.ToImmutableArray();
    }

    // -------------------------------------------------------------------------
    // Emission
    // -------------------------------------------------------------------------

    private static void Generate(SourceProductionContext spc, ClassData classData)
    {
        foreach (var dto in classData.Dtos)
        {
            spc.AddSource(
                $"{dto.DtoName}.g.cs",
                SourceText.From(EmitDtoClass(dto), Encoding.UTF8));
        }

        spc.AddSource(
            $"{classData.SourceClassName}Mapper.g.cs",
            SourceText.From(EmitMapper(classData), Encoding.UTF8));
    }

    // ---- DTO class ----------------------------------------------------------

    private static string EmitDtoClass(DtoData dto)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        bool needsJson = dto.Properties.Any(p => p.JsonName != null);
        if (needsJson)
        {
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine();
        }

        sb.AppendLine($"namespace {dto.DtoNamespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {dto.DtoName}");
        sb.AppendLine("    {");

        foreach (var prop in dto.Properties)
        {
            if (prop.Flatten)
            {
                foreach (var fp in prop.FlattenedProperties)
                    AppendProperty(sb, fp);
            }
            else
            {
                AppendProperty(sb, prop);
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendProperty(StringBuilder sb, PropertyData prop)
    {
        if (prop.JsonName is not null)
            sb.AppendLine($"        [JsonPropertyName(\"{prop.JsonName}\")]");

        sb.AppendLine($"        public {prop.TypeName} {prop.GeneratedName} {{ get; set; }}");
    }

    // ---- Mapper -------------------------------------------------------------

    private static string EmitMapper(ClassData classData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        var sourceFullName = string.IsNullOrEmpty(classData.SourceNamespace)
            ? classData.SourceClassName
            : $"{classData.SourceNamespace}.{classData.SourceClassName}";

        // Determine whether any DTO needs partial mapping
        var isPartialClass = classData.Dtos.Any(d => d.WithMapping);
        var classModifier  = isPartialClass ? "public static partial class" : "public static class";

        // Collect all unique DTO namespaces for usings
        var dtoNamespaces = classData.Dtos
            .Select(d => d.DtoNamespace)
            .Distinct()
            .Where(ns => !string.IsNullOrEmpty(ns))
            .ToArray();

        foreach (var ns in dtoNamespaces)
            sb.AppendLine($"using {ns};");

        if (dtoNamespaces.Length > 0) sb.AppendLine();

        var mapperNamespace = string.IsNullOrEmpty(classData.SourceNamespace)
            ? null
            : classData.SourceNamespace;

        if (mapperNamespace is not null)
        {
            sb.AppendLine($"namespace {mapperNamespace}");
            sb.AppendLine("{");
        }

        var indent = mapperNamespace is not null ? "    " : "";

        sb.AppendLine($"{indent}{classModifier} {classData.SourceClassName}Mapper");
        sb.AppendLine($"{indent}{{");

        foreach (var dto in classData.Dtos)
        {
            var dtoTypeName = dto.DtoName; // brought in via using

            sb.AppendLine($"{indent}    public static {dtoTypeName} To{dto.DtoName}(this {sourceFullName} source)");
            sb.AppendLine($"{indent}    {{");
            sb.AppendLine($"{indent}        var dest = new {dtoTypeName}");
            sb.AppendLine($"{indent}        {{");

            foreach (var prop in dto.Properties)
            {
                if (prop.Flatten)
                {
                    foreach (var fp in prop.FlattenedProperties)
                        sb.AppendLine($"{indent}            {fp.GeneratedName} = source.{prop.SourceName}.{fp.SourceName},");
                }
                else
                {
                    sb.AppendLine($"{indent}            {prop.GeneratedName} = source.{prop.SourceName},");
                }
            }

            sb.AppendLine($"{indent}        }};");

            if (dto.WithMapping)
                sb.AppendLine($"{indent}        MapCustom(source, dest);");

            sb.AppendLine($"{indent}        return dest;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine();

            if (dto.WithMapping)
            {
                sb.AppendLine($"{indent}    static partial void MapCustom({sourceFullName} source, {dtoTypeName} dest);");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");

        if (mapperNamespace is not null)
            sb.AppendLine("}");

        return sb.ToString();
    }
}
