///////////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006-2015 Esper Team. All rights reserved.                           /
// http://esper.codehaus.org                                                          /
// ---------------------------------------------------------------------------------- /
// The software in this package is published under the terms of the GPL license       /
// a copy of which has been included with this distribution in the license.txt file.  /
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using Castle.Core.Internal;

using com.espertech.esper.collection;
using com.espertech.esper.compat.attributes;
using com.espertech.esper.compat.magic;
using com.espertech.esper.util;

namespace com.espertech.esper.compat.collections
{
    public static class CompatExtensions
    {
        public static LookaheadEnumerator<T> WithLookahead<T>(this IEnumerator<T> en)
        {
            return new LookaheadEnumerator<T>(en);
        }

        public static LookaheadEnumerator<T> EnumerateWithLookahead<T>(this IEnumerable<T> en)
        {
            return new LookaheadEnumerator<T>(en);
        }

        public static ICollection<TExt> TransformInto<TInt, TExt>(this ICollection<TInt> collection)
            where TExt : TInt
        {
            return TransformInto(
                collection,
                e => (TInt) e,
                i => (TExt) i);
        }

        public static ICollection<TExt> TransformInto<TInt, TExt>(this ICollection<TInt> collection,
                                                           Func<TExt, TInt> transformExtInt,
                                                           Func<TInt, TExt> transformIntExt)
        {
            return new TransformCollection<TInt, TExt>(collection, transformExtInt, transformIntExt);
        }

        public static ReadOnlyCollection<T> AsReadOnlyCollection<T>(this ICollection<T> enumerable)
        {
            return new ReadOnlyCollection<T>(enumerable);
        }

        public static ReadOnlyList<T> AsReadOnlyList<T>(this IList<T> enumerable)
        {
            return new ReadOnlyList<T>(enumerable);
        }

        public static T[] ToArrayOrNull<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null) {
                return null;
            }

            var tempArray = enumerable.ToArray();
            if (tempArray.Length == 0) {
                return null;
            }

            return tempArray;
        }

        public static bool IsGreaterThan<T>(this IComparable<T> self, T that)
        {
            return self.CompareTo(that) > 0;
        }

        public static bool IsGreaterThanOrEqual<T>(this IComparable<T> self, T that)
        {
            return self.CompareTo(that) >= 0;
        }

        public static bool IsLessThan<T>(this IComparable<T> self, T that)
        {
            return self.CompareTo(that) < 0;
        }

        public static bool IsLessThanOrEqual<T>(this IComparable<T> self, T that)
        {
            return self.CompareTo(that) <= 0;
        }

        public static T[] ToPopArray<T>(this Stack<T> stack)
        {
            T[] array = new T[stack.Count];
            for( int ii = 0 ; stack.Count != 0 ; ii++) {
                array[ii] = stack.Pop();
            }

            return array;
        }

        public static bool Contains<T>(this IList<T> list, Predicate<T> predicate)
        {
            for( int ii = 0 ; ii < list.Count ; ii++ ) {
                if ( predicate.Invoke(list[ii] ) ) {
                    return true;
                }
            }
            return false;
        }

        public static bool Contains<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value) != -1;
        }

        public static HashSet<T> AsHashSet<T>(params T[] values)
        {
            return new HashSet<T>(values);
        } 

        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return null;
            if (enumerable is HashSet<T>)
                return (HashSet<T>) enumerable;
            return new HashSet<T>(enumerable);
        }

        public static SynchronizedCollection<T> AsSyncCollection<T>(this ICollection<T> unsyncCollection)
        {
            return new SynchronizedCollection<T>(unsyncCollection);
        }

        public static SynchronizedList<T> AsSyncList<T>( this IList<T> unsyncList )
        {
            return new SynchronizedList<T>(unsyncList);
        }

        public static SynchronizedSet<T> AsSyncSet<T>(this ISet<T> unsyncSet)
        {
            return new SynchronizedSet<T>(unsyncSet);
        }

        public static LinkedList<T> AsLinkedList<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable is LinkedList<T>)
                return (LinkedList<T>) enumerable;
            return new LinkedList<T>(enumerable);
        } 

        public static IList<T> AsList<T>( this IEnumerable<T> enumerable )
        {
            if ( enumerable is IList<T> )
                return (IList<T>) enumerable;
            return new List<T>(enumerable);
        }

        public static IList<T> AsMutableList<T>( this IEnumerable<T> enumerable )
        {
            return new List<T>(enumerable);
        }

        public static IList<T> AsSortedList<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return null;

            var asList = enumerable as List<T>;
            if ( asList == null ) {
                asList = new List<T>(enumerable);
            }

            asList.Sort();

            return asList;
        }

        public static void ForEach<T>(this LinkedList<T> listThis, Action<T> action)
        {
            if (listThis != null)
            {
                var node = listThis.First;
                while (node != null)
                {
                    action.Invoke(node.Value);
                    node = node.Next;
                }
            }
        }

        public static void ForEach<T>(this IList<T> arrayThis, Action<T> action)
        {
            if (arrayThis != null)
            {
                int length = arrayThis.Count;
                for (int ii = 0; ii < length; ii++)
                {
                    action.Invoke(arrayThis[ii]);
                }
            }
        }

        public static void ForEvery<T>(this T[] arrayThis, Action<T> action)
        {
            if (arrayThis != null)
            {
                int length = arrayThis.Length;
                for (int ii = 0; ii < length; ii++)
                {
                    action.Invoke(arrayThis[ii]);
                }
            }
        }

        public static void For<T>(this IEnumerable<T> enumThis, Action<T> action)
        {
            unchecked
            {
                if (enumThis == null)
                {
                    return;
                }
                if (enumThis is ChainedArrayList<T>)
                {
                    var arrayThis = (ChainedArrayList<T>) enumThis;
                    arrayThis.ForEach(action);
                }
                else if (enumThis is List<T>)
                {
                    var arrayThis = (List<T>)enumThis;
                    arrayThis.ForEach(action);
                }
                else if (enumThis is IList<T>)
                {
                    var arrayThis = (IList<T>) enumThis;
                    var length = arrayThis.Count;
                    for (int ii = 0; ii < length; ii++)
                    {
                        action.Invoke(arrayThis[ii]);
                    }
                }
                else
                {
                    foreach (T item in enumThis)
                    {
                        action.Invoke(item);
                    }
                }
            }
        }

        public static IEnumerable Is<T, TX>(this IEnumerable<T> enumThis)
        {
            return enumThis.Where(item => item is TX);
        }

        public static void Fill<T>(this T[] arrayThis, T value)
        {
            for (int ii = 0; ii < arrayThis.Length; ii++)
            {
                arrayThis[ii] = value;
            }
        }

        public static void Fill<T>(this T[] arrayThis, Func<T> generator)
        {
            for (int ii = 0; ii < arrayThis.Length; ii++)
            {
                arrayThis[ii] = generator.Invoke();
            }
        }

        public static void Fill<T>(this T[] arrayThis, Func<int, T> generator)
        {
            for (int ii = 0; ii < arrayThis.Length; ii++)
            {
                arrayThis[ii] = generator.Invoke(ii);
            }
        }

        public static bool IsEqual<T>( this IEnumerator<T> enumThis, IEnumerator<T> enumThat )
        {
            while (true)
            {
                var testThis = enumThis.MoveNext();
                var testThat = enumThat.MoveNext();
                if (testThis && testThat)
                {
                    if (!Equals(enumThis.Current, enumThat.Current))
                    {
                        return false;
                    }
                }
                else if (!testThis && !testThat)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the item at the nth index of the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static Object AtIndex( this IEnumerable enumerable, int index )
        {
            return AtIndex(enumerable, index, failIndex => null);
        }

        /// <summary>
        /// Gets the item at the nth index of the enumerable.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="index">The index.</param>
        /// <param name="itemNotFound">The item not found.</param>
        /// <returns></returns>
        public static Object AtIndex( this IEnumerable enumerable, int index, Func<int, Object> itemNotFound )
        {
            if ( enumerable is IList ) {
                var asList = (IList) enumerable;
                return index < asList.Count ? asList[index] : itemNotFound(index);
            }

            var enumerator = enumerable.GetEnumerator();
            if (index == 0)
            {
                return enumerator.MoveNext() ? enumerator.Current : itemNotFound(index) ;
            }

            for (var myIndex = 1; myIndex <= index; myIndex++)
            {
                if (!enumerator.MoveNext())
                {
                    return itemNotFound(index);
                }
            }

            return enumerator.Current;
        }

        /// <summary>
        /// Returns true if all items in the itemEnum are contained in referenceCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="referenceCollection"></param>
        /// <param name="itemEnum"></param>
        /// <returns></returns>

        public static bool ContainsAll<T>( this ICollection<T> referenceCollection, IEnumerable<T> itemEnum)
        {
            foreach (T item in itemEnum)
            {
                if (!referenceCollection.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Advances the specified enumerator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerator">The enumerator.</param>
        /// <returns></returns>
        public static T Advance<T>( this IEnumerator<T> enumerator )
        {
            enumerator.MoveNext();
            return enumerator.Current;
        }

        public static bool DeepUnorderedEquals<T>( this IList<T> pthis, IList<T> pthat )
        {
            if (pthis.Count != pthat.Count)
            {
                return false;
            }

            for( int ii = pthis.Count - 1 ; ii >= 0 ; ii-- ) {
                if (!pthat.Contains(pthis[ii])) return false;
            }

            return true;
        }


        /// <summary>
        /// Does a deep equality test on a set of lists.  Order is assumed to be the same.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pthis">The pthis.</param>
        /// <param name="pthat">The pthat.</param>
        /// <returns></returns>
        public static bool DeepEquals<T>( this IList<T> pthis, IList<T> pthat )
        {
            if (pthis == null && pthat == null)
                return true;
            if (pthis == null)
                return pthat.Count == 0;
            if (pthat == null)
                return pthis.Count == 0;

            if (pthis.Count != pthat.Count)
            {
                return false;
            }

            IEnumerator<T> thisEnum = pthis.GetEnumerator();
            IEnumerator<T> thatEnum = pthat.GetEnumerator();

            while( true )
            {
                var thisHasMore = thisEnum.MoveNext();
                var thatHasMore = thatEnum.MoveNext();
                if (!thisHasMore || !thatHasMore)
                {
                    return !thisHasMore && !thatHasMore;
                }

                if (! Equals(thisEnum.Current, thatEnum.Current))
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified collection in the parameter is null or
        /// empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// 	<c>true</c> if [is empty or null] [the specified parameter]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmptyOrNull<T>(this ICollection<T> parameter)
        {
            return
                (parameter == null) ||
                (parameter.Count == 0);
        }

        /// <summary>
        /// Determines whether the specified collection in the parameter is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// 	<c>true</c> if the specified parameter is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty<T>(this ICollection<T> parameter)
        {
            return parameter.Count == 0;
        }

        /// <summary>
        /// Determines whether the specified collection in the parameter is empty.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// 	<c>true</c> if the specified parameter is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmptyCollection(this ICollection parameter)
        {
            return parameter.Count == 0;
        }

        /// <summary>
        /// Determines whether the specified collection in the parameter is not empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameter">The parameter.</param>
        /// <returns>
        /// 	<c>true</c> if the specified parameter is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNotEmpty<T>(this ICollection<T> parameter)
        {
            return parameter.Count != 0;
        }

        /// <summary>
        /// Adds all of the items in the source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pthis">The pthis.</param>
        /// <param name="source">The source.</param>

        public static void AddAll<T>(this ICollection<T> pthis, IEnumerable<T> source)
        {
            if (source is IList<T>)
            {
                var asList = (IList<T>)source;
                var asListCount = asList.Count;
                for (int ii = 0; ii < asListCount; ii++)
                {
                    pthis.Add(asList[ii]);
                }
            }
            else if (source != null)
            {
                foreach (T value in source)
                {
                    pthis.Add(value);
                }
            }
        }

        public static IEnumerable<Tuple<TA,TB>> Merge<TA,TB>(this IEnumerable<TA> enumA, IEnumerable<TB> enumB)
        {
            if ((enumA != null) && (enumB != null))
            {
                var enA = enumA.GetEnumerator();
                var enB = enumB.GetEnumerator();
                while (enA.MoveNext() && enB.MoveNext())
                {
                    yield return new Tuple<TA, TB>
                    {
                        A = enA.Current, B = enB.Current
                    };
                }
            }
        }

        public static bool HasFirst<T>(this IEnumerable<T> pthis)
        {
            IEnumerator<T> tableEnum = pthis.GetEnumerator();
            return tableEnum != null && tableEnum.MoveNext();
        }

        /// <summary>
        /// Returns the second item in the set
        /// </summary>
        /// <returns></returns>

        public static T Second<T>(this IEnumerable<T> pthis)
        {
            IEnumerator<T> tableEnum = pthis.GetEnumerator();
            tableEnum.MoveNext();
            tableEnum.MoveNext();
            return tableEnum.Current;
        }

        public static void RemoveWhere<T>(this LinkedList<T> list, Func<T, bool> where)
        {
            for (var curr = list.First; curr != null; )
            {
                var next = curr.Next;
                if (where.Invoke(curr.Value))
                {
                    list.Remove(curr);
                }

                curr = next;
            }
        }

        public static void RemoveWhere<T>(this IList<T> list, Func<T, bool> where)
        {
            for (int ii = 0; ii < list.Count;)
            {
                var testItem = where.Invoke(list[ii]);
                if (testItem)
                {
                    list.RemoveAt(ii);
                }
                else
                {
                    ii++;
                }
            }
        }

        public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> where, Action<T> collector)
        {
            var count = 0;

            for (int ii = 0; ii < list.Count; )
            {
                var testItem = where.Invoke(list[ii]);
                if (testItem)
                {
                    count++;
                    collector.Invoke(list[ii]);
                    list.RemoveAt(ii);
                }
                else
                {
                    ii++;
                }
            }

            return count;
        }

        /// <summary>
        /// Removes all items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="items">The items.</param>

        public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                while (collection.Remove(item))
                {
                }
            }
        }

        /// <summary>
        /// Retains all items in the passed enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="items">The items.</param>
        public static void RetainAll<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items.Where(item => !collection.Contains(item)))
            {
                collection.Remove(item);
            }
        }

        /// <summary>
        /// Primitive reversal of a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>

        public static void Reverse<T>(this ICollection<T> collection)
        {
            List<T> tempList = collection as List<T>;
            if (tempList != null)
            {
                tempList.Reverse();
                return;
            }

            tempList = new List<T>();
            tempList.AddRange(collection);
            tempList.Reverse();

            collection.Clear();

            foreach (T item in tempList)
            {
                collection.Add(item);
            }
        }

        public static string Render<K, V>(this IEnumerable<KeyValuePair<K, V>> source)
        {
            string fieldDelimiter = String.Empty;

            var builder = new StringBuilder();
            builder.Append('[');

            if (source != null)
            {
                foreach(var current in source)
                {
                    builder.Append(fieldDelimiter);
                    builder.Append(Render(current.Key));
                    builder.Append('=');
                    if (ReferenceEquals(current.Value, null))
                        builder.Append("null");
                    else if (current.Value.GetType().IsGenericDictionary())
                        builder.Append(MagicMarker.NewMagicDictionary<object, object>(current.Value));
                    else if (current.Value is IEnumerable)
                        builder.Append(Render((IEnumerable) current.Value));
                    else
                        builder.Append(Render(current.Value));
                    fieldDelimiter = ", ";
                }
            }

            builder.Append(']');
            return builder.ToString();
        }

        /// <summary>
        /// Renders an enumerable source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>

        public static string Render(this IEnumerable source)
        {
            string fieldDelimiter = String.Empty;

            var builder = new StringBuilder();
            if (source != null)
            {
                builder.Append('[');

                IEnumerator sourceEnum = source.GetEnumerator();
                while (sourceEnum.MoveNext()) {
                    builder.Append(fieldDelimiter);
                    builder.Append(Render(sourceEnum.Current));
                    fieldDelimiter = ", ";
                }

                builder.Append(']');
            }
            else
            {
                builder.Append("null");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Renders an enumerable source
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="renderEngine">The render engine.</param>
        /// <returns></returns>

        public static string Render(this IEnumerable source, Func<object, string> renderEngine)
        {
            string fieldDelimiter = String.Empty;

            StringBuilder builder = new StringBuilder();
            builder.Append('[');

            IEnumerator sourceEnum = source.GetEnumerator();
            while (sourceEnum.MoveNext())
            {
                builder.Append(fieldDelimiter);
                builder.Append(renderEngine(sourceEnum.Current));
                fieldDelimiter = ", ";
            }

            builder.Append(']');
            return builder.ToString();
        }

        /// <summary>
        /// Removes the item at the specified index from the list and
        /// returns the item.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>

        public static V Pluck<V>(this IList<V> list, int index)
        {
            V tempItem = list[index];
            list.RemoveAt(index);
            return tempItem;
        }

        /// <summary>
        /// Removes the item at the front of the list and returns it.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="list">The list.</param>
        /// <returns></returns>

        public static V PopFront<V>(this LinkedList<V> list)
        {
            V tempItem = list.First.Value;
            list.RemoveFirst();
            return tempItem;
        }

        public static V Poll<V>(this LinkedList<V> list, V defaultValue)
        {
            if (list.First == null)
                return defaultValue;

            V tempItem = list.First.Value;
            list.RemoveFirst();
            return tempItem;
        }

        public static V Poll<V>(this LinkedList<V> list)
        {
            return Poll(list, default(V));
        }


        public static V Poll<V>(this IList<V> list, V defaultValue)
        {
            if (list.Count == 0)
                return defaultValue;

            V tempItem = list[0];
            list.RemoveAt(0);
            return tempItem;
        }

        public static V Poll<V>(this IList<V> list)
        {
            return Poll(list, default(V));
        }

        public static LinkedListNode<V> FirstNode<V>(this LinkedList<V> list, Func<V,bool> whereClause)
        {
            for(var curr = list.First; curr != null; curr = curr.Next)
            {
                if (whereClause(curr.Value))
                {
                    return curr;
                }
            }

            return null;
        }


        public static String[] ToUpper( this string[] inArray )
        {
            string[] outArray = new string[inArray.Length];
            for( int ii = 0 ; ii < inArray.Length; ii++ ) {
                var inItem = inArray[ii];
                if (inItem != null) {
                    outArray[ii] = inItem.ToUpper();
                }
            }

            return outArray;
        }

        public static Type[] GetParameterTypes(this MethodInfo method)
        {
            return method.GetParameters().Select(p => p.ParameterType).ToArray();
        }

        public static Type[] GetParameterTypes(this ConstructorInfo ctor)
        {
            return ctor.GetParameters().Select(p => p.ParameterType).ToArray();
        }

        public static IEnumerable<int> XRange(int lowerBound, int upperBound)
        {
            for( int ii = lowerBound ; ii < upperBound ; ii++ )
            {
                yield return ii;
            }
        }

        public static T[] UnwrapIntoArray<T>(this object value)
        {
            if (value == null)
                return null;
            if (value is T[])
                return (T[]) value;
            if (value is ICollection<T>)
                return ((ICollection<T>) value).ToArray();
            if (value is IEnumerable<object>)
                return ((IEnumerable<object>)value).OfType<T>().ToArray();
            if (value is IEnumerable)
                return ((IEnumerable)value).Cast<object>().OfType<T>().ToArray();

            throw new ArgumentException("invalid value");
        }

        public static ICollection<T> UnwrapWithNulls<T>(this object value)
        {
            if (value == null)
                return null;
            if (value is ICollection<T>)
                return ((ICollection<T>)value);
            if (value is IEnumerable<object>)
                return ((IEnumerable<object>)value)
                    .Where(o => o == null || o is T)
                    .Cast<T>()
                    .ToArray();
            if (value is IEnumerable)
                return ((IEnumerable)value)
                    .Cast<object>()
                    .Where(o => o == null || o is T)
                    .Cast<T>()
                    .ToArray();

            throw new ArgumentException("invalid value");
        }


        public static ICollection<T> Unwrap<T>(this object value)
        {
            if (value == null)
                return null;
            if (value is ICollection<T>)
                return ((ICollection<T>)value);
            if (value is IEnumerable<object>)
                return ((IEnumerable<object>) value)
                    .OfType<T>()
                    .ToArray();
            if (value is IEnumerable)
                return ((IEnumerable) value)
                    .Cast<object>()
                    .OfType<T>()
                    .ToArray();

            throw new ArgumentException("invalid value");
        }

        public static IEnumerable<T> UnwrapEnumerable<T>(this object value)
        {
            if (value == null)
                return null;
            if (value is ICollection<T>)
                return ((ICollection<T>)value);
            if (value is IEnumerable<object>)
                return ((IEnumerable<object>)value)
                    .OfType<T>();
            if (value is IEnumerable)
                return ((IEnumerable) value)
                    .Cast<object>()
                    .OfType<T>();

            throw new ArgumentException("invalid value");
        }

        public static IDictionary<string, object> UnwrapDictionary(this object value)
        {
            var valueDataMap = value as IDictionary<string, object>;
            if (valueDataMap == null)
            {
                var valueType = value.GetType();
                if (valueType.IsGenericStringDictionary())
                {
                    valueDataMap = MagicMarker.GetStringDictionaryFactory(valueType).Invoke(value);
                }
                else
                {
                    throw new ArgumentException("unable to convert input to string dictionary");
                }
            }

            return valueDataMap;
        }

        public static string Render(this object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string)
            {
                return (string) value;
            }

            if ((value is decimal) ||
                (value is double) ||
                (value is float))
            {
                var text = value.ToString();
                if (text.IndexOf('.') == -1)
                {
                    text += ".0";
                }

                return text;
            }

            if (value is DateTimeOffset)
            {
                var dateTime = (DateTimeOffset)value;
                var dateOnly = dateTime.Date;
                if (dateTime == dateOnly)
                {
                    return dateTime.ToString("yyyy-MM-dd z");
                }
                else if (dateTime.Millisecond == 0)
                {
                    return dateTime.ToString("yyyy-MM-dd hh:mm:ss z");
                }
                else
                {
                    return dateTime.ToString("yyyy-MM-dd hh:mm:ss.ffff z");
                }
            }

            if (value is DateTime)
            {
                var dateTime = (DateTime) value;
                var dateOnly = dateTime.Date;
                if (dateTime == dateOnly)
                {
                    return dateTime.ToString("yyyy-MM-dd");
                }
                else if (dateTime.Millisecond == 0)
                {
                    return dateTime.ToString("yyyy-MM-dd hh:mm:ss");
                }
                else
                {
                    return dateTime.ToString("yyyy-MM-dd hh:mm:ss.ffff");
                }
            }

            if (value is Array)
            {
                return Render(value as Array);
            }

            if (value.GetType().GetCustomAttributes(typeof (RenderWithToStringAttribute), true).Length > 0)
            {
                return value.ToString();
            }

            if (value is IEnumerable)
            {
                return Render((IEnumerable) value);
            }

            return value.ToString();
        }

        /// <summary>
        /// Renders the array as a string.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns></returns>
        public static String Render(this Array array)
        {
            return Render(array, ", ", "[]");
        }

        /// <summary>
        /// Renders the array as a string
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="itemSeparator">The item separator.</param>
        /// <param name="firstAndLast">The first and last.</param>
        /// <returns></returns>

        public static String Render(this Array array, String itemSeparator, String firstAndLast)
        {
            string fieldDelimiter = String.Empty;

            StringBuilder builder = new StringBuilder();
            builder.Append(firstAndLast[0]);

            if (array != null)
            {
                int length = array.Length;
                for (int ii = 0; ii < length; ii++)
                {
                    builder.Append(fieldDelimiter);
                    builder.Append(Render(array.GetValue(ii)));
                    fieldDelimiter = itemSeparator;
                }
            }

            builder.Append(firstAndLast[1]);
            return builder.ToString();
        }

        public static String FormatInt(this int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.CurrentCulture) : "null";
        }

        /// <summary>
        /// Returns a view into a subsection of the list.  Since the resulting list is a shallow view, care
        /// should be taken to ensure that the underlying list is not adversely modified while using the
        /// view.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="fromIndex">From index.</param>
        /// <param name="toIndex">To index.</param>
        /// <returns></returns>
        public static IList<T> SubList<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            if (toIndex >= list.Count)
                toIndex = list.Count;

            return new SubList<T>(list, fromIndex, toIndex);
        }

        /// <summary>
        /// Appends one or more items to the source.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="more">The more.</param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] more)
        {
            foreach (T item in source)
                yield return item;
            foreach (T item in more)
                yield return item;
        }
    }
}
