using System.Collections;

namespace Nuve.DataStore;

internal sealed class DictionaryStoreEnumerator<TValue> : IEnumerator<KeyValuePair<string, TValue?>>
{
    private int _currentIndex = -1;
    private readonly long _containerCount = 0;
    private readonly DictionaryStore<TValue?> _container;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="container"></param>
    public DictionaryStoreEnumerator(DictionaryStore<TValue?> container)
    {
        _container = container;
        _containerCount = _container.Count();
    }

    public void Dispose()
    {
        //Intentionally left blank
    }

    public bool MoveNext()
    {
        _currentIndex++;
        return _currentIndex < _containerCount;
    }

    public void Reset()
    {
        _currentIndex = 0;
    }

    public KeyValuePair<string, TValue?> Current
    {
        get
        {
            var keys = _container.Keys();
            return new KeyValuePair<string, TValue?>(keys[_currentIndex], _container[keys[_currentIndex]]);
        }
    }

    object? IEnumerator.Current
    {
        get { return Current; }
    }
}
