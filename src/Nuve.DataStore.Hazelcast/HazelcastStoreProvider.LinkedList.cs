using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore.Hazelcast
{
    public partial class HazelcastStoreProvider : ILinkedListStoreProvider
    {
        long ILinkedListStoreProvider.AddAfter(string listKey, string pivot, string value)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.AddAfterAsync(string listKey, string pivot, string value)
        {
            throw new NotImplementedException();
        }

        long ILinkedListStoreProvider.AddBefore(string listKey, string pivot, string value)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.AddBeforeAsync(string listKey, string pivot, string value)
        {
            throw new NotImplementedException();
        }

        long ILinkedListStoreProvider.AddFirst(string listKey, params string[] value)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.AddFirstAsync(string listKey, params string[] value)
        {
            throw new NotImplementedException();
        }

        long ILinkedListStoreProvider.AddLast(string listKey, params string[] value)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.AddLastAsync(string listKey, params string[] value)
        {
            throw new NotImplementedException();
        }

        long ILinkedListStoreProvider.Count(string listKey)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.CountAsync(string listKey)
        {
            throw new NotImplementedException();
        }

        string ILinkedListStoreProvider.Get(string listKey, long index)
        {
            throw new NotImplementedException();
        }

        Task<string> ILinkedListStoreProvider.GetAsync(string listKey, long index)
        {
            throw new NotImplementedException();
        }

        IList<string> ILinkedListStoreProvider.GetRange(string listKey, long start, long end)
        {
            throw new NotImplementedException();
        }

        Task<IList<string>> ILinkedListStoreProvider.GetRangeAsync(string listKey, long start, long end)
        {
            throw new NotImplementedException();
        }

        bool ILinkedListStoreProvider.IsExists(string listKey)
        {
            throw new NotImplementedException();
        }

        Task<bool> ILinkedListStoreProvider.IsExistsAsync(string listKey)
        {
            throw new NotImplementedException();
        }

        long ILinkedListStoreProvider.Remove(string listKey, string value)
        {
            throw new NotImplementedException();
        }

        Task<long> ILinkedListStoreProvider.RemoveAsync(string listKey, string value)
        {
            throw new NotImplementedException();
        }

        string ILinkedListStoreProvider.RemoveFirst(string listKey)
        {
            throw new NotImplementedException();
        }

        Task<string> ILinkedListStoreProvider.RemoveFirstAsync(string listKey)
        {
            throw new NotImplementedException();
        }

        string ILinkedListStoreProvider.RemoveLast(string listKey)
        {
            throw new NotImplementedException();
        }

        Task<string> ILinkedListStoreProvider.RemoveLastAsync(string listKey)
        {
            throw new NotImplementedException();
        }

        void ILinkedListStoreProvider.Set(string listKey, long index, string value)
        {
            throw new NotImplementedException();
        }

        Task ILinkedListStoreProvider.SetAsync(string listKey, long index, string value)
        {
            throw new NotImplementedException();
        }

        void ILinkedListStoreProvider.Trim(string listKey, long start, long end)
        {
            throw new NotImplementedException();
        }

        Task ILinkedListStoreProvider.TrimAsync(string listKey, long start, long end)
        {
            throw new NotImplementedException();
        }
    }
}
