using System;
using System.Collections.Generic;

public class MinHeap<T> where T : IHeapItem<T>
{
    private readonly List<T> items = new();

    public int Count => items.Count;

    public void Add(T item)
    {
        item.HeapIndex = items.Count;
        items.Add(item);
        HeapifyUp(item.HeapIndex);
    }

    public T Pop()
    {
        if (items.Count == 0) throw new InvalidOperationException("Heap is empty");

        T root = items[0];
        items[0] = items[^1];
        items[0].HeapIndex = 0;
        items.RemoveAt(items.Count - 1);
        HeapifyDown(0);

        return root;
    }

    public void UpdateItem(T item)
    {
        int index = item.HeapIndex;
        int parent = (index - 1) / 2;

        if (index > 0 && items[index].CompareTo(items[parent]) < 0)
            HeapifyUp(index);
        else
            HeapifyDown(index);
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (items[index].CompareTo(items[parent]) >= 0) break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = items.Count - 1;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left <= lastIndex && items[left].CompareTo(items[smallest]) < 0)
                smallest = left;

            if (right <= lastIndex && items[right].CompareTo(items[smallest]) < 0)
                smallest = right;

            if (smallest == index) break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (items[b], items[a]) = (items[a], items[b]);
        items[a].HeapIndex = a;
        items[b].HeapIndex = b;
    }
}

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}
