#if NET47
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    internal class InternalProfileManager
    {
        private static readonly string _profilerSignatureKey = "__profiler__" + Guid.NewGuid().ToString("N");
        public static ProfileScope Current
        {
            get
            {
                var wrapper = (ProfileScopeWrapper)CallContext.LogicalGetData(_profilerSignatureKey);
                return wrapper != null && wrapper.Scope != null ? wrapper.Scope : null;
            }

            internal set
            {
                var wrapper = value == null ? null : new ProfileScopeWrapper(value);
                CallContext.LogicalSetData(_profilerSignatureKey, wrapper);
            }
        }
    }

}
#endif