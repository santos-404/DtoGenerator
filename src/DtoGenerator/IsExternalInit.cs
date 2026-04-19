// Required polyfill: 'record' types use init-only setters which depend on
// IsExternalInit, a type that only exists in .NET 5+. Since the generator
// targets netstandard2.0 we define it ourselves.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
