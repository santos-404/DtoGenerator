using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DtoGenerator.Tests;

/// <summary>
/// Compiles a source string with the DtoSourceGenerator and returns the
/// generated source texts so tests can assert on them.
/// </summary>
internal static class GeneratorTestHelper
{
    private static readonly IReadOnlyList<MetadataReference> BaseReferences = BuildBaseReferences();

    public static GeneratorResult Run(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: BaseReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DtoSourceGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        var runResult = driver.GetRunResult();

        var generatedSources = runResult.GeneratedTrees
            .Select(t => (FileName: Path.GetFileName(t.FilePath), Source: t.GetText().ToString()))
            .ToImmutableArray();

        return new GeneratorResult(generatedSources, diagnostics, output.GetDiagnostics());
    }

    private static IReadOnlyList<MetadataReference> BuildBaseReferences()
    {
        // Load all assemblies already in the AppDomain (covers mscorlib / System.* / netstandard)
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => (MetadataReference)MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // Make sure the Attributes assembly is included
        refs.Add(MetadataReference.CreateFromFile(
            typeof(DtoGenerator.Attributes.GenerateDtoAttribute).Assembly.Location));

        // System.Text.Json may not be loaded into the AppDomain yet — add it explicitly
        // so generated code that uses [JsonPropertyName] compiles correctly.
        refs.Add(MetadataReference.CreateFromFile(
            typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly.Location));

        return refs;
    }
}

internal record GeneratorResult(
    ImmutableArray<(string FileName, string Source)> GeneratedSources,
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationDiagnostics)
{
    public string GetSource(string fileName) =>
        GeneratedSources
            .Where(s => s.FileName == fileName)
            .Select(s => s.Source)
            .FirstOrDefault()
        ?? throw new InvalidOperationException($"No generated file named '{fileName}'. Available: {string.Join(", ", GeneratedSources.Select(s => s.FileName))}");

    public bool HasErrors =>
        GeneratorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) ||
        CompilationDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}
