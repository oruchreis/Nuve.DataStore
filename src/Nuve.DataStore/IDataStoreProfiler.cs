using System.Collections.Generic;

namespace Nuve.DataStore;

/// <summary>
/// Used to profile DataStore methods.
/// </summary>
public interface IDataStoreProfiler
{
    /// <summary>
    /// This method is called at the beginning of profiling. The object returned as output will be passed as a parameter in the <see cref="Finish"/> method.
    /// </summary>
    /// <param name="method">The long name of the executed method along with the class name</param>
    /// <param name="key">The key passed to the method</param>
    /// <returns>The profile information is grouped based on the object provided as output. This object will be passed as a parameter in the <see cref="Finish"/> method.</returns>
    object? Begin(string method, string? key);

    /// <summary>
    /// This method is called at the end of profiling.
    /// </summary>
    /// <param name="context">The object returned in the <see cref="Begin"/> method</param>
    /// <param name="results">The profiling results</param>
    /// <returns></returns>
    void Finish(object? context, params DataStoreProfileResult[] results);

    object? GetContext();
}
