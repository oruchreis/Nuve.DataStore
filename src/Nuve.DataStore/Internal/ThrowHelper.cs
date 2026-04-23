using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Nuve.DataStore.Redis")]

namespace Nuve.DataStore.Internal
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull(
            object? argument,
            [CallerArgumentExpression("argument")] string? paramName = null)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(argument, paramName);
#else
        if (argument is null)
            throw new ArgumentNullException(paramName);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrWhiteSpace(
            string? argument,
            [CallerArgumentExpression("argument")] string? paramName = null)
        {
#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
        if (argument is null)
            throw new ArgumentNullException(paramName);

        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("The argument cannot be null, empty, or whitespace.", paramName);
#endif
        }
    }
}


#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif