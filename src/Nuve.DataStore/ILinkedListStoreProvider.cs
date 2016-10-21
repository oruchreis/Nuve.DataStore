using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface ILinkedListStoreProvider
    {
        Task<bool> IsExistsAsync(string listKey);

        Task<string> GetAsync(string listKey, long index);

        Task<IList<string>> GetRangeAsync(string listKey, long start, long end);

        Task SetAsync(string listKey, long index, string value);

        Task<long> AddFirstAsync(string listKey, params string[] value);

        Task<long> AddLastAsync(string listKey, params string[] value);

        Task<long> AddAfterAsync(string listKey, string pivot, string value);

        Task<long> AddBeforeAsync(string listKey, string pivot, string value);

        Task<long> CountAsync(string listKey);

        Task<string> RemoveFirstAsync(string listKey);

        Task<string> RemoveLastAsync(string listKey);

        Task<long> RemoveAsync(string listKey, string value);

        Task TrimAsync(string listKey, long start, long end);

        bool IsExists(string listKey);
        
        string Get(string listKey, long index);
        
        IList<string> GetRange(string listKey, long start, long end);
        
        void Set(string listKey, long index, string value);
        
        long AddFirst(string listKey, params string[] value);
        
        long AddLast(string listKey, params string[] value);
        
        long AddAfter(string listKey, string pivot, string value);
        
        long AddBefore(string listKey, string pivot, string value);
        
        long Count(string listKey);
        
        string RemoveFirst(string listKey);
        
        string RemoveLast(string listKey);
        
        long Remove(string listKey, string value);
        
        void Trim(string listKey, long start, long end);
    }
}