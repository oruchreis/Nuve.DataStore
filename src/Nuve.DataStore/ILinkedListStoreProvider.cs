using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface ILinkedListStoreProvider
    {
        Task<bool> IsExistsAsync(string listKey);

        Task<byte[]> GetAsync(string listKey, long index);

        Task<IList<byte[]>> GetRangeAsync(string listKey, long start, long end);

        Task SetAsync(string listKey, long index, byte[] value);

        Task<long> AddFirstAsync(string listKey, params byte[][] value);

        Task<long> AddLastAsync(string listKey, params byte[][] value);

        Task<long> AddAfterAsync(string listKey, byte[] pivot, byte[] value);

        Task<long> AddBeforeAsync(string listKey, byte[] pivot, byte[] value);

        Task<long> CountAsync(string listKey);

        Task<byte[]> RemoveFirstAsync(string listKey);

        Task<byte[]> RemoveLastAsync(string listKey);

        Task<long> RemoveAsync(string listKey, byte[] value);

        Task TrimAsync(string listKey, long start, long end);

        bool IsExists(string listKey);

        byte[] Get(string listKey, long index);
        
        IList<byte[]> GetRange(string listKey, long start, long end);
        
        void Set(string listKey, long index, byte[] value);
        
        long AddFirst(string listKey, params byte[][] value);
        
        long AddLast(string listKey, params byte[][] value);
        
        long AddAfter(string listKey, byte[] pivot, byte[] value);
        
        long AddBefore(string listKey, byte[] pivot, byte[] value);
        
        long Count(string listKey);

        byte[] RemoveFirst(string listKey);

        byte[] RemoveLast(string listKey);
        
        long Remove(string listKey, byte[] value);
        
        void Trim(string listKey, long start, long end);
    }
}