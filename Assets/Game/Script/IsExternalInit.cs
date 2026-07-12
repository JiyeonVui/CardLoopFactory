// Polyfill required by Unity (.NET Standard 2.1 / Mono) so that C# 9 records and
// init-only setters compile. The runtime does not ship this type, so we declare it.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
