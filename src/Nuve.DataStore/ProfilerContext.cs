namespace Nuve.DataStore;

internal readonly struct ProfilerContext : IEquatable<ProfilerContext>
{
    public readonly object? GlobalContext;
    public readonly object? LocalContext;

    public ProfilerContext(object? globalContext, object? localContext) : 
        this()
    {
        GlobalContext = globalContext;
        LocalContext = localContext;
    }

    public static bool operator ==(ProfilerContext left, ProfilerContext right)
    {
        return left.GlobalContext == right.GlobalContext && left.LocalContext == right.LocalContext;
    }

    public static bool operator !=(ProfilerContext left, ProfilerContext right)
    {
        return !(left == right);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is null) return false;
        return obj is ProfilerContext context && Equals(context);
    }

    public readonly bool Equals(ProfilerContext other)
    {
        return this == other;
    }

    public override readonly int GetHashCode()
    {
        unchecked
        {
            return (GlobalContext != null ? GlobalContext.GetHashCode() * 397 : 0) ^ (LocalContext != null ? LocalContext.GetHashCode() * 397 : 0);
        }
    }
}
