using System.Collections;

namespace StackLibrary;

public class LinkedStack<T> : IEnumerable<T>
{
    private Node? _top;

    public LinkedStack()
    {
    }

    public LinkedStack(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (T item in items)
        {
            Push(item);
        }
    }

    public T Current
    {
        get
        {
            if (_top is null)
            {
                throw new InvalidOperationException("Стек пуст: текущего элемента нет.");
            }

            return _top.Value;
        }
    }

    public int Count { get; private set; }

    public bool IsEmpty => _top is null;

    public void Push(T item)
    {
        _top = new Node(item, _top);
        Count++;
    }

    public T Pop()
    {
        if (_top is null)
        {
            throw new InvalidOperationException("Стек пуст: извлекать нечего.");
        }

        T value = _top.Value;
        _top = _top.Next;
        Count--;
        return value;
    }

    public void Clear()
    {
        _top = null;
        Count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (Node? node = _top; node is not null; node = node.Next)
        {
            yield return node.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private sealed class Node
    {
        public Node(T value, Node? next)
        {
            Value = value;
            Next = next;
        }

        public T Value { get; }

        public Node? Next { get; }
    }
}
