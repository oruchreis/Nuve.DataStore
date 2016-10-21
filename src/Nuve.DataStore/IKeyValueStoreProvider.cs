using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuve.DataStore
{
    public interface IKeyValueStoreProvider
    {
        Task<string> GetAsync(string key);
        Task<IDictionary<string, string>> GetAllAsync(params string[] keys);
        Task<bool> SetAsync(string key, string entity, bool overwrite);
        Task<bool> SetAllAsync(IDictionary<string, string> keyValues, bool overwrite);
        Task<string> ExchangeAsync(string key, string value);
        Task<long> AppendStringAsync(string key, string value);
        Task<string> SubStringAsync(string key, long start, long end);
        Task<long> OverwriteStringAsync(string key, long offset, string value);
        Task<long> SizeInBytesAsync(string key);
        Task<bool> ContainsAsync(string key);
        Task<bool> RenameAsync(string oldKey, string newKey);
        Task<long> IncrementAsync(string key, long amount);
        Task<long> DecrementAsync(string key, long amount);

        string Get(string key);
        IDictionary<string, string> GetAll(params string[] keys);
        bool Set(string key, string entity, bool overwrite);
        bool SetAll(IDictionary<string, string> keyValues, bool overwrite);
        string Exchange(string key, string value);
        long AppendString(string key, string value);
        string SubString(string key, long start, long end);
        long OverwriteString(string key, long offset, string value);
        long SizeInBytes(string key);
        bool Contains(string key);
        bool Rename(string oldKey, string newKey);
        long Increment(string key, long amount);
        long Decrement(string key, long amount);
    }
}
