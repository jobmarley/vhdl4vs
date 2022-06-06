using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace vhdl4vs
{
	public static class Extensions
    {
        public static Span Union(this Span s1, Span? s2)
        {
            if (s2 == null)
                return s1;
            return Span.FromBounds(Math.Min(s1.Start, s2.Value.Start), Math.Max(s1.End, s2.Value.End));
        }

        public static Span Offset(this Span s, int offset)
        {
            return new Span(s.Start + offset, s.Length);
        }

        public static Int32 BinarySearchIndexOf<T, U>(this SortedList<T, U> list, T value)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            IComparer<T> comparer = list.Comparer;

            Int32 lower = 0;
            Int32 upper = list.Count - 1;

            while (lower <= upper)
            {
                Int32 middle = lower + (upper - lower) / 2;
                Int32 comparisonResult = comparer.Compare(value, list.Keys[middle]);
                if (comparisonResult == 0)
                    return middle;
                else if (comparisonResult < 0)
                    upper = middle - 1;
                else
                    lower = middle + 1;
            }

            return ~lower;
        }

        public static int LowerBoundIndex<TKey, TValue>(this SortedList<TKey, TValue> d, TKey key)
        {
            int minNum = 0;
            int maxNum = d.Count - 1;

            int mid = -1;
            while (minNum <= maxNum)
            {
                mid = (minNum + maxNum) / 2;
                int comp = Comparer<TKey>.Default.Compare(key, d.Keys[mid]);
                if (comp == 0)
                    return mid;
                else if (comp < 0)
                    maxNum = mid - 1;
                else
                    minNum = mid + 1;
            }

            return maxNum;
        }
        public static TValue LowerBoundOrDefault<TKey, TValue>(this SortedList<TKey, TValue> d, TKey key)
        {
            int i = d.LowerBoundIndex(key);
            if (i > -1 && i < d.Count)
                return d.Values[i];
            return default(TValue);
        }
        public static int UpperBoundIndex<TKey, TValue>(this SortedList<TKey, TValue> d, TKey key)
        {
            if (d.Count == 0)
                return -1;

            int i = d.LowerBoundIndex(key);
            if (i == -1)
                return 0;

            int comp = Comparer<TKey>.Default.Compare(key, d.Keys[i]);
            if (comp <= 0)
                return i;
            else
                return (d.Keys.Count > i + 1) ? i + 1 : -1; // -1 if end of list, i + 1 otherwise
        }
        public static TValue UpperBoundOrDefault<TKey, TValue>(this SortedList<TKey, TValue> d, TKey key)
        {
            int i = d.UpperBoundIndex(key);
            if (i > -1 && i < d.Count)
                return d.Values[i];
            return default(TValue);
        }
    }
}
