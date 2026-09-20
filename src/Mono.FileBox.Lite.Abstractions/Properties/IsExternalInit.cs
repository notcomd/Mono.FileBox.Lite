// Provides the `init` accessor support required by modern C# on .NET Standard 2.0.
#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif