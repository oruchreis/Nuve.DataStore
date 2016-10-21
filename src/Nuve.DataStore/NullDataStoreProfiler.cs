using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    internal class NullDataStoreProfiler: IDataStoreProfiler
    {
        public object Begin(string method, string key)
        {
            return null;
        }

        public void Finish(object context, params DataStoreProfileResult[] result)
        {
        }

        public object GetContext()
        {
            return null;
        }
    }
}
