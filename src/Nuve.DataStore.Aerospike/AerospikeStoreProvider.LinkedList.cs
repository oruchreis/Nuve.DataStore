using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;

namespace Nuve.DataStore.Aerospike
{
    //public partial class AerospikeStoreProvider: ILinkedListStoreProvider
    //{
    //    async Task<bool> ILinkedListStoreProvider.IsExistsAsync(string listKey)
    //    {
    //        return await Client.Exists(null, CancellationToken.None, listKey.ToKey(Namespace));
    //    }

    //    async Task<string> ILinkedListStoreProvider.GetAsync(string listKey, long index)
    //    {
    //        return (await Client.Get(null, CancellationToken.None, listKey.ToKey(Namespace))).GetString(index.ToString());
    //    }

    //    async Task<IList<string>> ILinkedListStoreProvider.GetRangeAsync(string listKey, long start, long end)
    //    {
    //        return (await Client.Get(null, CancellationToken.None, listKey.ToKey(Namespace))).bins
    //            .Where(b =>
    //                   {
    //                       var ind = long.Parse(b.Key);
    //                       return ind >= start && ind <= end;
    //                   })
    //            .Select(b => b.Value.ToString())
    //            .ToList();
    //    }

    //    async Task ILinkedListStoreProvider.SetAsync(string listKey, long index, string value)
    //    {
    //        await Client.Put(new WritePolicy(ClientPolicy.writePolicyDefault) { recordExistsAction = RecordExistsAction.REPLACE },
    //             CancellationToken.None, listKey.ToKey(Namespace), value.ToBin(index.ToString()));
    //    }

    //    async Task<long> ILinkedListStoreProvider.AddFirstAsync(string listKey, params string[] value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<long> ILinkedListStoreProvider.AddLastAsync(string listKey, params string[] value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<long> ILinkedListStoreProvider.AddAfterAsync(string listKey, string pivot, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<long> ILinkedListStoreProvider.AddBeforeAsync(string listKey, string pivot, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<long> ILinkedListStoreProvider.CountAsync(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<string> ILinkedListStoreProvider.RemoveFirstAsync(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<string> ILinkedListStoreProvider.RemoveLastAsync(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task<long> ILinkedListStoreProvider.RemoveAsync(string listKey, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    Task ILinkedListStoreProvider.TrimAsync(string listKey, long start, long end)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    bool ILinkedListStoreProvider.IsExists(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    string ILinkedListStoreProvider.Get(string listKey, long index)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    IList<string> ILinkedListStoreProvider.GetRange(string listKey, long start, long end)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    void ILinkedListStoreProvider.Set(string listKey, long index, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.AddFirst(string listKey, params string[] value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.AddLast(string listKey, params string[] value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.AddAfter(string listKey, string pivot, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.AddBefore(string listKey, string pivot, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.Count(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    string ILinkedListStoreProvider.RemoveFirst(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    string ILinkedListStoreProvider.RemoveLast(string listKey)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    long ILinkedListStoreProvider.Remove(string listKey, string value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    void ILinkedListStoreProvider.Trim(string listKey, long start, long end)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
