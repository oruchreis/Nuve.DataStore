using System.Collections;

namespace Nuve.DataStore;

internal sealed class LinkedListStoreEnumerator<TValue> : IEnumerator<TValue?>
{
    private int _currentIndex = -1;
    private readonly long _containerCount = 0;
    private readonly LinkedListStore<TValue?> _container;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="container"></param>
    public LinkedListStoreEnumerator(LinkedListStore<TValue?> container)
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

    public TValue? Current
    {
        get
        {
            return _container[_currentIndex];
        }
    }

    object? IEnumerator.Current
    {
        get { return Current; }
    }
}
