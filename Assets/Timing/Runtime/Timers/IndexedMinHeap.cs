using System;
using System.Collections.Generic;

namespace Timing.Timers
{
    /// <summary>
    /// Min-heap with O(log n) remove/update by item Id.
    /// Requires items to expose Id and DueMs.
    /// </summary>
    internal sealed class IndexedMinHeap<T> where T : class
    {
        private readonly List<T> _heap = new();
        private readonly Dictionary<int, int> _indexById = new();
        private readonly Func<T, int> _getId;
        private readonly Func<T, long> _getDueMs;
        private readonly Action<T, long> _setDueMs;

        public int Count => _heap.Count;

        public IndexedMinHeap(Func<T, int> getId, Func<T, long> getDueMs, Action<T, long> setDueMs)
        {
            _getId = getId;
            _getDueMs = getDueMs;
            _setDueMs = setDueMs;
        }

        public bool ContainsId(int id) => _indexById.ContainsKey(id);

        public void Clear()
        {
            _heap.Clear();
            _indexById.Clear();
        }

        public void Push(T item)
        {
            int id = _getId(item);
            if (_indexById.ContainsKey(id))
                throw new InvalidOperationException($"Heap already contains id={id}");

            _heap.Add(item);
            int i = _heap.Count - 1;
            _indexById[id] = i;
            SiftUp(i);
        }

        public T Peek()
        {
            if (_heap.Count == 0) return null;
            return _heap[0];
        }

        public long PeekDueMs()
        {
            if (_heap.Count == 0) return long.MaxValue;
            return _getDueMs(_heap[0]);
        }

        public T Pop()
        {
            if (_heap.Count == 0) return null;

            var root = _heap[0];
            RemoveByIndex(0);
            return root;
        }

        public bool Remove(int id)
        {
            if (!_indexById.TryGetValue(id, out int idx)) return false;
            RemoveByIndex(idx);
            return true;
        }

        public bool UpdateDueMs(int id, long newDueMs)
        {
            if (!_indexById.TryGetValue(id, out int idx)) return false;

            var item = _heap[idx];
            long old = _getDueMs(item);
            if (old == newDueMs) return true;

            _setDueMs(item, newDueMs);

            // Re-heapify based on direction
            if (newDueMs < old) SiftUp(idx);
            else SiftDown(idx);

            return true;
        }

        private void RemoveByIndex(int idx)
        {
            int last = _heap.Count - 1;
            var removed = _heap[idx];
            int removedId = _getId(removed);

            _indexById.Remove(removedId);

            if (idx == last)
            {
                _heap.RemoveAt(last);
                return;
            }

            var moved = _heap[last];
            _heap[idx] = moved;
            _heap.RemoveAt(last);

            _indexById[_getId(moved)] = idx;

            // restore heap property
            SiftDown(idx);
            SiftUp(idx);
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_getDueMs(_heap[i]) >= _getDueMs(_heap[p])) break;
                Swap(i, p);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int smallest = i;

                if (l < _heap.Count && _getDueMs(_heap[l]) < _getDueMs(_heap[smallest])) smallest = l;
                if (r < _heap.Count && _getDueMs(_heap[r]) < _getDueMs(_heap[smallest])) smallest = r;

                if (smallest == i) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var tmp = _heap[a];
            _heap[a] = _heap[b];
            _heap[b] = tmp;

            _indexById[_getId(_heap[a])] = a;
            _indexById[_getId(_heap[b])] = b;
        }
    }
}
