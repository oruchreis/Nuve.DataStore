using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    internal struct ProfilerContext : IEquatable<ProfilerContext>
    {
        public readonly object GlobalContext;
        public readonly object LocalContext;

        public ProfilerContext(object globalContext, object localContext) : 
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ProfilerContext && Equals((ProfilerContext)obj);
        }

        public bool Equals(ProfilerContext other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GlobalContext != null ? GlobalContext.GetHashCode() * 397 : 0) ^ (LocalContext != null ? LocalContext.GetHashCode() * 397 : 0);
            }
        }
    }
}
