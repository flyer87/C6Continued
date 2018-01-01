﻿// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using static System.Diagnostics.Contracts.Contract;

using static C6.Contracts.ContractHelperExtensions;
using static C6.Contracts.ContractMessage;

using SC = System.Collections;
using SCG = System.Collections.Generic;

namespace C6
{
    // TODO: Setup contracts to avoid exceptions?
    /// <summary>
    ///     Represents an indexed, sequenced generic collection where item order is determined by insertion and removal order.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the items in the collection.
    /// </typeparam>
    [ContractClass(typeof(IListContract<>))]
    public interface IList<T> : IIndexed<T>, SCG.IList<T>, SC.IList, IDisposable
    {
        /// <summary>
        ///     Gets the number of items contained in the collection.
        /// </summary>
        /// <value>
        ///     The number of items contained in the collection.
        /// </value>
        [Pure]
        new int Count { get; }

        /// <summary>
        ///     Gets the first item in the list.
        /// </summary>
        /// <value>
        ///     The first item in this list.
        /// </value>
        [Pure]
        T First { get; }

        /// <summary>
        ///     Gets a value indicating whether the list has a fixed size.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the list has a fixed size; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     <para>
        ///         A list with a fixed size does not allow operations that changes the list's size.
        ///     </para>
        ///     <para>
        ///         Any list that is read-only (<see cref="IsReadOnly"/> is <c>true</c>), has a fixed size; the opposite need not
        ///         be true.
        ///     </para>
        /// </remarks>
        [Pure]
        new bool IsFixedSize { get; }

        /// <summary>
        ///     Gets a value indicating whether the collection is read-only.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the collection is read-only; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        ///     A collection that is read-only does not allow the addition or removal of items after the collection is created.
        ///     Note that read-only in this context does not indicate whether individual items of the collection can be modified.
        /// </remarks>
        [Pure]
        new bool IsReadOnly { get; }

        /// <summary>
        ///     Gets the last item in the list.
        /// </summary>
        /// <value>
        ///     The last item in the list.
        /// </value>
        [Pure]
        T Last { get; }        

        /// <summary>
        ///     Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index of the item to get or set.
        /// </param>
        /// <value>
        ///     The item at the specified index. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </value>
        /// <remarks>
        ///     The setter raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemRemovedAt"/> with the removed item and the index.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsRemoved"/> with the removed item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemInserted"/> with the inserted item and the index.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsAdded"/> with the inserted item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        [IndexerName("Item")]
        new T this[int index]
        {
            [Pure]
            get;
            set;
        }

        /// <summary>
        ///     Removes all items from the collection.
        /// </summary>
        /// <remarks>
        ///     If the collection is non-empty, it raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionCleared"/> as full and with count equal to the collection count.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        new void Clear();  

        /// <summary> Gets the number of items in the underlyging list before the view; view's left endpoint</summary>
        /// <value> The index of the view’s beginning within the underlying list.</value>
        /// <remarks>Gets 0 if proper list</remarks>
        int Offset { get; }

        /// <summary>
        ///  Gets the underlying list if this is a view; <c>null</c> if this is a proper list (not a view).
        /// </summary>
        /// <value>The underlyig list of the view; <c>null</c> if not a view.</value>
        IList<T> Underlying { get; }

        /// <summary>
        ///     Returns a view of the specified range within list or view
        /// </summary>        
        /// <param name="index">
        ///     The offset relative to the given list or view.
        /// </param>
        /// <param name="count">
        ///     The lenght of the view.
        /// </param>
        /// <returns>
        ///     Type: <see cref="IList{T}"/>
        ///     Returns a view of the specified range within this list or view.
        /// </returns>  
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="IList{T}"/> has the same functionality as a list:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     You can use the returned <see cref="IList{T}"/> as a list and execute all list operations.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         The view remains valid as long as <see cref = "ICollectionValue{T}.IsValid" /> evaluates to <c> true </c>. 
        ///     </para>
        ///     <para>
        ///         Calling <see cref="Sort()"/>, <see cref="Reverse()"/> or <see cref="Shuffle()"/> on the underlying list or a view
        ///         might invalide other views.
        ///     </para>
        ///     <para>
        ///         A view created from a view is itself just a view of the underlying list.
        ///         Views are not nested inside each other; for instance, a view created from another view <c>w</c> is
        ///         not affected by subsequent sliding of <c>w</c>.        
        ///     </para>
        /// </remarks>
        IList<T> View(int index, int count);

        /// <summary>
        ///     Returns a new list view that points at the first occurrence of x, if any, in the list or view.
        /// </summary>        
        /// <param name="item">
        ///     The item of the view.
        /// </param>        
        /// <returns>
        ///     <para> Type: <see cref="IList{T}"/> </para>
        ///     <para> Returns a new list view that points at the first occurrence of x, if any, in the list or view;
        ///         otherwise <c>null</c>. 
        ///     </para>
        ///     <para>
        ///         The returned <see cref="IList{T}"/> has the same functionality as a list:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     You can use the returned <see cref="IList{T}"/> as a list and execute all list operations.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         The view remains valid as long as <see cref = "ICollectionValue{T}.IsValid" /> evaluates to <c> true </c>. 
        ///     </para>
        ///     <para>
        ///         Calling <see cref="Sort()"/>, <see cref="Reverse()"/> or <see cref="Shuffle()"/> on the underlying list or a view
        ///         might invalide other views.
        ///     </para>
        ///     <para>
        ///         A view created from a view is itself just a view of the underlying list.
        ///         Views are not nested inside each other; for instance, a view created from another view <c>w</c> is
        ///         not affected by subsequent sliding of <c>w</c>.        
        ///     </para>
        /// </returns>  
        IList<T> ViewOf(T item);

        /// <summary>
        ///     Returns a new list view that points at the last occurrence of x, if any, in the list or view.
        /// </summary>        
        /// <param name="item">
        ///     The item of the view.
        /// </param>        
        /// <returns>
        ///     Type: <see cref="IList{T}"/>
        ///     Returns a new list view that points at the last occurrence of x, if any, in the list or view;
        ///     otherwise <c>null</c>.
        /// </returns>  
        /// <remarks>
        ///     <para>
        ///         The returned <see cref="IList{T}"/> has the same functionality as a list:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     You can use the returned <see cref="IList{T}"/> as a list and execute all list operations.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         The view remains valid as long as <see cref = "ICollectionValue{T}.IsValid" /> evaluates to <c> true </c>. 
        ///     </para>
        ///     <para>
        ///         Calling <see cref="Sort()"/>, <see cref="Reverse()"/> or <see cref="Shuffle()"/> on the underlying list or a view
        ///         might invalide other views.
        ///     </para>
        ///     <para>
        ///         A view created from a view is itself just a view of the underlying list.
        ///         Views are not nested inside each other; for instance, a view created from another view <c>w</c> is
        ///         not affected by subsequent sliding of <c>w</c>.        
        ///     </para>
        /// </remarks>
        IList<T> LastViewOf(T item);

        /// <summary>
        ///     Slides the view with <c>offset</c> items.
        /// </summary>
        /// <param name="offset">
        ///     The number of the items to slide with.
        /// </param>
        /// <returns>
        ///     <list type="none">
        ///         <item>
        ///             Type: <see cref="IList{T}"/>
        ///         </item>
        ///         <item>
        ///             The same view, slided with <c>offset</c> items.
        ///         </item>        
        ///     </list>        
        /// </returns>
        /// <remarks>
        ///     The method doesn't create a new view; It returns the same view, but slided.
        /// </remarks>
        IList<T> Slide(int offset);

        /// <summary>
        ///     Slides the view with <c>offset</c> items and sets the lenght to <c>count</c>.
        /// </summary>
        /// <param name="offset">
        ///     The number of the items to slide with.
        /// </param>
        /// <param name="count">
        ///     The lenght of the view.
        /// </param>
        /// <returns>        
        ///     The method doesn't create a new view; It returns the same view, slided with <c>offset</c> items and length <c>count</c>.
        /// </returns>
        IList<T> Slide(int offset, int count);

        /// <summary>
        ///      Slides the view with <c>offset</c> items. The retun value indicates if the sliding suceeded.
        /// </summary>
        /// <param name="offset">
        ///     The number of the items to slide with.
        /// </param>
        /// <returns>
        ///     <c>True</c> if sliding suceeded; otherwise <c>false</c>.
        /// </returns>
        bool TrySlide(int offset);

        /// <summary>
        ///      Slides the view with <c>offset</c> items and sets it lenght to <c>count</c>. The retun value indicates if that suceeded.
        /// </summary>
        /// <param name="offset">
        ///     The number of the items to slide with.
        /// </param>
        /// <param name="count">
        ///     The length of the view.
        /// </param>
        /// <returns>
        ///     <c>True</c> if the sliding suceeded; otherwise <c>false</c>.
        /// </returns>
        bool TrySlide(int offset, int count);

        /// <summary>
        ///     Spans a view with another view.
        /// </summary>
        /// <param name="other">
        ///     The other view to span with.
        /// </param>
        /// <returns>
        ///     A new view as <see cref="IList{T}"/>.
        ///     If the right endpoint of <c>other</c> is strictly to the left of the left endpoint of this view, then <c>null</c> is returned.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         <list type="bullet">
        ///             <item>  
        ///                 The left endpoint of the new view is the left endpoint of this view; 
        ///                 the right endpoint - the right endpoint of <c>other</c> view.
        ///             </item>              
        ///         </list>
        ///     </para>
        /// </remarks>
        IList<T> Span(IList<T> other);        

        /// <summary>
        ///     Searches from the beginning of the collection for the specified item and returns the zero-based index of the first
        ///     occurrence within the collection.
        /// </summary>
        /// <param name="item">
        ///     The item to locate in the collection. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </param>
        /// <returns>
        ///     The zero-based index of the first occurrence of item within the entire collection, if found; otherwise, a negative
        ///     number that is the bitwise complement of the index at which <see cref="ICollection{T}.Add"/> would put the item.
        /// </returns>
        [Pure]
        new int IndexOf(T item);

        /// <summary>
        ///     Inserts an item into the list at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index at which value should be inserted.
        /// </param>
        /// <param name="item">
        ///     The item to insert into the list. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/> is
        ///     <c>true</c>.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         If <paramref name="index"/> equals the number of items in the list, then the value is appended to the end of
        ///         the list. This has the same effect as calling <see cref="ICollection{T}.Add"/>, though the events raised are
        ///         different.
        ///     </para>
        ///     <para>
        ///         When inserting, the items that follow the insertion point move down to accommodate the new item. The indices of
        ///         the items that are moved are also updated.
        ///     </para>
        ///     <para>
        ///         Raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemInserted"/> with the inserted item and the index.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsAdded"/> with the item and a count of one.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/>.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        new void Insert(int index, T item);

        /// <summary>
        ///     Inserts an item at the beginning of the list.
        /// </summary>
        /// <param name="item">
        ///     The item to insert at the beginning of the list. <c>null</c> is allowed, if
        ///     <see cref="ICollectionValue{T}.AllowsNull"/> is <c>true</c>.
        /// </param>
        /// <remarks>
        ///     Raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemInserted"/> with the item and an index of zero.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsAdded"/> with the item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        void InsertFirst(T item);

        /// <summary>
        ///     Inserts an item at the end of the list.
        /// </summary>
        /// <param name="item">
        ///     The item to insert at the end of the list. <c>null</c> is allowed, if <see cref="ICollectionValue{T}.AllowsNull"/>
        ///     is <c>true</c>.
        /// </param>
        /// <remarks>
        ///     Raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemInserted"/> with the item and an index of <c>Count</c>.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsAdded"/> with the item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        void InsertLast(T item);

        // TODO: Document that events are raise pairwise for each item!
        /// <summary>
        ///     Inserts the items into the list starting at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index at which the new items should be inserted.
        /// </param>
        /// <param name="items">
        ///     The enumerable whose items should be inserted into the list. The enumerable itself cannot be <c>null</c>, but its
        ///     items can, if <see cref="ICollectionValue{T}.AllowsNull"/> is <c>true</c>.
        ///     The items of the enumerable should be unique If <see cref="IExtensible{T}.AllowsDuplicates"/> is <c>false</c>.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         If the enumerable throws an exception during enumeration, the collection remains unchanged.
        ///     </para>
        ///     <para>
        ///         Raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemInserted"/> once for each item and the index at which is was
        ///                     inserted.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsAdded"/> once for each item added (using a count of one).
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/> once at the end.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        void InsertRange(int index, SCG.IEnumerable<T> items);

        /// <summary>
        ///     Determines whether the list is sorted in non-descending order according to the default comparer.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The default comparer <see cref="SCG.Comparer{T}.Default"/> cannot find an implementation of the
        ///     <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type
        ///     <typeparamref name="T"/>.
        /// </exception>
        /// <returns>
        ///     <c>true</c> if the list is sorted in non-descending order; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        bool IsSorted();

        /// <summary>
        ///     Determines whether the list is sorted in non-descending order according to the specified comparer.
        /// </summary>
        /// <param name="comparer">
        ///     The <see cref="SCG.IComparer{T}"/> implementation to use when comparing items, or <c>null</c> to use the default
        ///     comparer <see cref="SCG.Comparer{T}.Default"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="comparer"/> is <c>null</c>, and the default comparer <see cref="SCG.Comparer{T}.Default"/> cannot
        ///     find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/>
        ///     interface for type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The implementation of <paramref name="comparer"/> caused an error during the sort. For example,
        ///     <paramref name="comparer"/> might not return zero when comparing an item with itself.
        /// </exception>
        /// <returns>
        ///     <c>true</c> if the list is sorted in non-descending order; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        bool IsSorted(SCG.IComparer<T> comparer);

        /// <summary>
        ///     Determines whether the list is sorted in non-descending order according to the specified
        ///     <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">
        ///     The <see cref="Comparison{T}"/> to use when comparing elements.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The implementation of <paramref name="comparison"/> caused an error during the sort. For example,
        ///     <paramref name="comparison"/> might not return zero when comparing an item with itself.
        /// </exception>
        /// <returns>
        ///     <c>true</c> if the list is sorted in non-descending order; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        bool IsSorted(Comparison<T> comparison);

        /// <summary>
        ///     Removes and returns the item at the specified index of the collection.
        /// </summary>
        /// <param name="index">
        ///     The zero-based index of the item to remove.
        /// </param>
        /// <returns>
        ///     The item removed from the collection.
        /// </returns>
        /// <remarks>
        ///     Raises the following events (in that order) with the collection as sender:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemRemovedAt"/> with the removed item and the index.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.ItemsRemoved"/> with the removed item and a count of one.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <see cref="IListenable{T}.CollectionChanged"/>.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        new T RemoveAt(int index);

        /// <summary>
        ///     Removes and returns an item from the beginning of the list.
        /// </summary>
        /// <returns>
        ///     The item removed from the beginning of the list.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The methods <see cref="ICollection{T}.Add"/> and <see cref="RemoveFirst"/> together behave like a
        ///         first-in-first-out queue.
        ///     </para>
        ///     <para>
        ///         Raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemRemovedAt"/> with the item and an index of zero.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsRemoved"/> with the removed item and a count of one.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/>.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        T RemoveFirst();

        /// <summary>
        ///     Removes and returns an item from the end of the list.
        /// </summary>
        /// <returns>
        ///     The item removed from the end of the list.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         The methods <see cref="ICollection{T}.Add"/> and <see cref="RemoveLast"/> together behave like a
        ///         last-in-first-out stack.
        ///     </para>
        ///     <para>
        ///         Raises the following events (in that order) with the collection as sender:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemRemovedAt"/> with the item and an index of <c>Count</c> - 1.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.ItemsRemoved"/> with the removed item and a count of one.
        ///                 </description>
        ///             </item>
        ///             <item>
        ///                 <description>
        ///                     <see cref="IListenable{T}.CollectionChanged"/>.
        ///                 </description>
        ///             </item>
        ///         </list>
        ///     </para>
        /// </remarks>
        T RemoveLast();

        /// <summary>
        ///     Reverses the sequence order of the items in the list.
        /// </summary>
        /// <remarks>
        ///     If the collection contains more than one item, it raises the <see cref="IListenable{T}.CollectionChanged"/>.
        /// </remarks>
        void Reverse();

        /// <summary>
        ///     Randomly shuffles the items in the list.
        /// </summary>
        /// <remarks>
        ///     If the collection contains more than one item, it raises the <see cref="IListenable{T}.CollectionChanged"/>, even
        ///     if the item order was not changed by the shuffle.
        /// </remarks>
        void Shuffle();

        /// <summary>
        ///     Shuffles the items in the list according to the specified random source.
        /// </summary>
        /// <param name="random">The random source.</param>
        /// <remarks>
        ///     If the collection contains more than one item, it raises the <see cref="IListenable{T}.CollectionChanged"/>, even
        ///     if the item order was not changed by the shuffle.
        /// </remarks>
        void Shuffle(Random random);

        /// <summary>
        ///     Sorts the items in the list using the default comparer.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The default comparer <see cref="SCG.Comparer{T}.Default"/> cannot find an implementation of the
        ///     <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/> interface for type
        ///     <typeparamref name="T"/>.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         There is no requirement as to whether an implementation performs a stable or unstable sort; that is, if two
        ///         elements are equal, their order might or might not be preserved.
        ///     </para>
        ///     <para>
        ///         If the collection is not already sorted, it raises the <see cref="IListenable{T}.CollectionChanged"/>.
        ///     </para>
        /// </remarks>
        void Sort();

        /// <summary>
        ///     Sorts the items in the list using the specified comparer.
        /// </summary>
        /// <param name="comparer">
        ///     The <see cref="SCG.IComparer{T}"/> implementation to use when comparing items, or <c>null</c> to use the default
        ///     comparer <see cref="SCG.Comparer{T}.Default"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="comparer"/> is <c>null</c>, and the default comparer <see cref="SCG.Comparer{T}.Default"/> cannot
        ///     find an implementation of the <see cref="IComparable{T}"/> generic interface or the <see cref="IComparable"/>
        ///     interface for type <typeparamref name="T"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The implementation of <paramref name="comparer"/> caused an error during the sort. For example,
        ///     <paramref name="comparer"/> might not return zero when comparing an item with itself.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         There is no requirement as to whether an implementation performs a stable or unstable sort; that is, if two
        ///         elements are equal, their order might or might not be preserved.
        ///     </para>
        ///     <para>
        ///         If the collection is not already sorted, it raises the <see cref="IListenable{T}.CollectionChanged"/>.
        ///     </para>
        /// </remarks>
        void Sort(SCG.IComparer<T> comparer);

        /// <summary>
        ///     Sorts the items in the list using the specified <see cref="Comparison{T}"/>.
        /// </summary>
        /// <param name="comparison">
        ///     The <see cref="Comparison{T}"/> to use when comparing elements.
        /// </param>
        /// <exception cref="ArgumentException">
        ///     The implementation of <paramref name="comparison"/> caused an error during the sort. For example,
        ///     <paramref name="comparison"/> might not return zero when comparing an item with itself.
        /// </exception>
        /// <remarks>
        ///     <para>
        ///         There is no requirement as to whether an implementation performs a stable or unstable sort; that is, if two
        ///         elements are equal, their order might or might not be preserved.
        ///     </para>
        ///     <para>
        ///         If the collection is not already sorted, it raises the <see cref="IListenable{T}.CollectionChanged"/>.
        ///     </para>
        /// </remarks>
        void Sort(Comparison<T> comparison);
    }

    [ContractClassFor(typeof(IList<>))]
    internal abstract class IListContract<T> : IList<T>
    {
        // ReSharper disable InvocationIsSkipped        

        public int Count
        {
            get {
                // No additional preconditions allowed                                

                // No postconditions


                return default(int);
            }
        }        

        public T First
        {
            get {
                // Collection must be non-empty
                Requires(!IsEmpty, CollectionMustBeNonEmpty);

                // Equals first item
                Ensures(Result<T>().IsSameAs(this[0]));
                Ensures(Result<T>().IsSameAs(this.First()));

                return default(T);
            }
        }

        public bool IsFixedSize
        {
            get {
                // No additional preconditions allowed


                // No postconditions


                return default(bool);
            }
        }

        public bool IsReadOnly
        {
            get {
                // No additional preconditions allowed


                // No postconditions


                return default(bool);
            }
        }

        public T Last
        {
            get {
                // Collection must be non-empty
                Requires(!IsEmpty, CollectionMustBeNonEmpty);


                // Equals first item
                Ensures(Result<T>().IsSameAs(this[Count - 1]));
                Ensures(Result<T>().IsSameAs(this.Last()));


                return default(T);
            }
        }

        // View
        public int Offset
        {
            get {
                // this is Valid
                Requires(IsValid, MustBeValid);

                // If view, First is the same as skipping the first Result() items from Underlying and taking the first item.
                // If this is Empty, skip the postcondition
                Ensures(IsEmpty || Underlying == null || First.IsSameAs(Underlying.Skip(Result<int>()).First()));

                // If proper list, First is the same as skipping the first Result() items from this and taking the first item.
                // If this is Empty, skip the postcondition
                Ensures(IsEmpty || Underlying != null || First.IsSameAs(this.Skip(Result<int>()).First()));

                // If view, this is the same as skipping the first Result() items from Underlying and then taking Count items.
                Ensures(Underlying == null || this.IsSameSequenceAs(Underlying.Skip(Result<int>()).Take(Count)));                

                return default(int);
            }
        }

        public IList<T> Underlying
        {
            get {
                // this is Valid
                Requires(IsValid, MustBeValid);

                // if this is proper list then result is null, otherwise result's underlying is null
                Ensures(Result<IList<T>>() == null || Result<IList<T>>().Underlying == null);

                return default(IList<T>);
            }
        }

        public IList<T> View(int index, int count)
        {
            // this must be valid 
            Requires(IsValid, MustBeValid);

            // index must be non-negative
            Requires(index >= 0, ArgumentMustBeWithinBounds);

            // count must be non-negative
            Requires(count >= 0, ArgumentMustBeWithinBounds);

            // The end of the view must be less than or equal to Count
            Requires(index + count <= Count, ArgumentMustBeWithinBounds);



            // Result is not null
            Ensures(Result<IList<T>>() != null);

            // Result's underlying is the same as the underlying of this or this
            Ensures(Result<IList<T>>().Underlying == (Underlying ?? this));

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>().Offset == Offset + index);

            // Result's Count is equal to count
            Ensures(Result<IList<T>>().Count == count);

            // ToArray() is the same as taking count items starting from index
            Ensures(Result<IList<T>>().ToArray().IsSameSequenceAs(this.Skip(index).Take(count)));

            // Result's IsFixedSize is the same as this's
            Ensures(Result<IList<T>>().IsFixedSize == IsFixedSize);

            // Result's IsReadOnly is the same as this's
            Ensures(Result<IList<T>>().IsReadOnly == IsReadOnly);

            // Result's IndexingSpeed is the same as this's
            Ensures(Result<IList<T>>().IndexingSpeed == IndexingSpeed);

            // Result's ContainsSpeed is the same as this's
            Ensures(Result<IList<T>>().ContainsSpeed == ContainsSpeed);

            // Result's AllowsDuplicates is the same as this's
            Ensures(Result<IList<T>>().AllowsDuplicates == AllowsDuplicates);

            // Result's DuplicatesByCounting is the same as this's
            Ensures(Result<IList<T>>().DuplicatesByCounting == DuplicatesByCounting);

            // Result is empty if this is
            Ensures(Result<IList<T>>().IsEmpty == (count == 0));

            // Result's First is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().First.Equals(this.Skip(index).Take(count).First()));

            // Result's Last is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().Last.Equals(this.Skip(index).Take(count).Last()));            

            // Result's direction is the same as this's 
            Ensures(Result<IList<T>>().Direction == this.Direction);

            //Result is correct
            Ensures(Result<IList<T>>().IsSameSequenceAs(this.Skip(index).Take(count)));            

            return default(IList<T>);
        }

        public IList<T> ViewOf(T item)
        {
            // this must be valid 
            Requires(IsValid, MustBeValid);

            // Item must be non-null, If the list doesn't allow nulls
            Requires(AllowsNull || item != null, ItemMustBeNonNull);



            // Result is not null
            //no need Ensures(Result<IList<T>>() != null);

            // Result's underlying is the same as the underlying of this or this
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Underlying == (Underlying ?? this));

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Offset == Offset + this.TakeWhile(x => !x.Equals(item)).Count());

            // Result's Count is equal to count
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Count == 1);

            // ToArray() is the same as taking the first item equal to item(argument)
            Ensures(Result<IList<T>>() == null || 
                Result<IList<T>>().ToArray().First().IsSameAs(this.First(x => x.Equals(item))));

            // Result's IsFixedSize is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsFixedSize == IsFixedSize);

            // Result's IsReadOnly is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsReadOnly == IsReadOnly);

            // Result's IndexingSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IndexingSpeed == IndexingSpeed);

            // Result's ContainsSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().ContainsSpeed == ContainsSpeed);

            // Result's AllowsDuplicates is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().AllowsDuplicates == AllowsDuplicates);

            // Result's DuplicatesByCounting is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().DuplicatesByCounting == DuplicatesByCounting);

            // Result is empty if this is
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty == (Result<IList<T>>().Count == 0));

            // Result's First is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty || Result<IList<T>>().First.Equals(this.First(x => x.Equals(item))));

            // Result's Last is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty || Result<IList<T>>().Last.Equals(this.Last(x => x.Equals(item))));

            // Result's direction is the same as this's 
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Direction == Direction);

            // Result is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().First().IsSameAs(this.First(x => x.Equals(item)))); // ??? is it fine  
            //Ensures(Result<IList<T>>().IsSameAs(this.First(x => x.Equals(item)).ToList())); // ??? or what ?? 

            return default(IList<T>);
        }

        public IList<T> LastViewOf(T item)
        {
            // this must be valid 
            Requires(IsValid, MustBeValid);

            // Item must be non-null, If the list doesn't allow nulls
            Requires(AllowsNull || item != null, ItemMustBeNonNull);


            // Result is not null
            //Ensures(Result<IList<T>>() != null);

            // Result's underlying is the same as the underlying of this or this
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Underlying == (Underlying ?? this));

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Offset == Offset + this.ToList().LastIndexOf(item)); // ???

            // Result's Count is equal to count
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Count == 1);

            // ToArray is the same as skipping last inddex items and taking the next one.
            Ensures(Result<IList<T>>() == null ||  
                Result<IList<T>>().ToArray().IsSameSequenceAs(this.Skip(this.LastIndexOf(item)).Take(1).ToList()));

            // Result's IsFixedSize is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsFixedSize == IsFixedSize);

            // Result's IsReadOnly is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsReadOnly == IsReadOnly);

            // Result's IndexingSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IndexingSpeed == IndexingSpeed);

            // Result's ContainsSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().ContainsSpeed == ContainsSpeed);

            // Result's AllowsDuplicates is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().AllowsDuplicates == AllowsDuplicates);

            // Result's DuplicatesByCounting is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().DuplicatesByCounting == DuplicatesByCounting);

            // Result is empty if this is
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty == (Result<IList<T>>().Count == 0));

            // Result's First is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().First.Equals(this.First(x => x.Equals(item)))); 

            // Result's Last is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Last.Equals(this.Last(x => x.Equals(item))));

            // Result's direction is the same as this's 
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Direction == this.Direction);

            // Result is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().First().IsSameAs(this.Last(x => x.Equals(item)) ));

            return default(IList<T>);            
        }

        public IList<T> Slide(int offset)
        {
            // this must be valid 
            Requires(IsValid, MustBeValid);

            // The new offset must be non-negative
            Requires(Offset + offset >= 0, ArgumentMustBeWithinBounds);

            // The new end of the view must be less than or equal to the underlying count
            Requires(Offset + offset + Count <= Underlying.Count, ArgumentMustBeWithinBounds);



            // Result is not null
            Ensures(Result<IList<T>>() != null);

            // Result is the same as this
            Ensures(Result<IList<T>>() == this);

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>().Offset == OldValue(Offset) + offset);

            // Result's Count is equal to Count
            Ensures(Result<IList<T>>().Count == Count);

            // Result is empty if this is
            Ensures(Result<IList<T>>().IsEmpty == (Count == 0));

            // Result's First is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().First.Equals(Underlying.Skip(OldValue(Offset) + offset).Take(Count).First()));

            // Result's Last is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().Last.Equals(Underlying.Skip(OldValue(Offset) + offset).Take(Count).Last()));

            
            //Result is correct
            Ensures(Result<IList<T>>().IsSameSequenceAs(Underlying.Skip( OldValue(Offset) + offset).Take(Count)));


            return default(IList<T>);
        }

        public IList<T> Slide(int offset, int count)
        {
            // this must be valid 
            Requires(IsValid, MustBeValid);

            // count parameter must be non-negative
            Requires(count >= 0, ArgumentMustBeWithinBounds);
            
            // The new offset must be non-negative
            Requires(Offset + offset >= 0, ArgumentMustBeWithinBounds);

            // The end of the view must be less than or equal to the underlying count
            Requires(Offset + offset + count <= Underlying.Count, ArgumentMustBeWithinBounds);

            

            // Result is not null
            Ensures(Result<IList<T>>() != null);

            // Result is the same as this
            Ensures(Result<IList<T>>() == this);

            // Result's underlying is the same as the underlying of this 
            //Ensures(Result<IList<T>>().Underlying == Underlying);

            // Result's Offset is equal to index
            Ensures(Result<IList<T>>().Offset == OldValue(Offset) + offset);

            // Result's Count is equal to count
            Ensures(Result<IList<T>>().Count == count);

            // Result is empty if this is
            Ensures(Result<IList<T>>().IsEmpty == (count == 0));

            // Result's First is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().First.Equals(Underlying.Skip(OldValue(Offset) + offset).Take(count).First()));

            // Result's Last is correct
            Ensures(Result<IList<T>>().IsEmpty || Result<IList<T>>().Last.Equals(Underlying.Skip(OldValue(Offset) + offset).Take(count).Last()));

            // Result's direction is the same as this's 
            //Ensures(Result<IList<T>>().Direction == Direction);

            //Result is correct
            Ensures(Result<IList<T>>().IsSameSequenceAs(Underlying.Skip(OldValue(Offset) + offset).Take(count)));



            return default(IList<T>);
        }

        public bool TrySlide(int offset)
        {
            // this must be a view
            Requires(Underlying != null, NotAView);

            // this must be valid
            Requires(IsValid, MustBeValid);



            // Underlying is the same as the underlying of this or this
            Ensures(Underlying == OldValue(Underlying));

            // Offset is equal to old ofset + Ofset, if the Result is true            
            Ensures(!Result<bool>() || Offset == OldValue(Offset) + offset);

            // Offset is equal to the old Ofset, if the Result is false
            Ensures(Result<bool>() || Offset == OldValue(Offset));
            
            // Count is equal to the old Count
            Ensures(Result<bool>() || Count == OldValue(Count));

            // IsFixedSize is the same as this's IsFixedSize
            Ensures(IsFixedSize == OldValue(IsFixedSize));

            //IsReadOnly is the same as this's IsReadOnly
            Ensures(IsReadOnly == OldValue(IsReadOnly));

            //IndexingSpeed is the same as this's
            Ensures(IndexingSpeed == OldValue(IndexingSpeed));

            // Result's ContainsSpeed is the same as this's
            Ensures(ContainsSpeed == OldValue(ContainsSpeed));

            // Result's AllowsDuplicates is the same as this's
            Ensures(AllowsDuplicates == OldValue(AllowsDuplicates));

            // Result's DuplicatesByCounting is the same as this's
            Ensures(DuplicatesByCounting == OldValue(DuplicatesByCounting));

            // Result's direction is the same as this's 
            Ensures(Direction == OldValue(Direction));

            // this is empty, if Count is zero
            Ensures(!Result<bool>() || IsEmpty == (Count == 0));            

            // First is correct, if Result is true                        
            Ensures(!Result<bool>() || IsEmpty || First.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(Count).First()));

            // First is unchanged, if Result is false
            Ensures(Result<bool>() || First.IsSameAs(OldValue(First)));

            // Last is correct, if Result is true                                  
            Ensures(!Result<bool>() || IsEmpty || Last.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(Count).Last()));

            // Last is unchanged, if Result is false
            Ensures(Result<bool>() || Last.IsSameAs(OldValue(Last)));

            // The sliding is correct, if Result is true
            Ensures(!Result<bool>() || this.IsSameSequenceAs(Underlying.Skip(OldValue(Offset) + offset).Take(Count)));

            // this is unchanged, if Result is false
            Ensures(Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));



            return default(bool);
        }

        public bool TrySlide(int offset, int count)
        {
            // this must be a view
            Requires(Underlying != null, NotAView);

            // this must be valid
            Requires(IsValid, MustBeValid);
            


            // Underlying is the same as the underlying of this or this
            Ensures(Underlying == OldValue(Underlying));

            // Offset is equal to old offset + Offset, if the Result is true            
            Ensures(!Result<bool>() || Offset == OldValue(Offset) + offset);

            // Offset is equal to the old Offset, if the Result is false
            Ensures( Result<bool>() || Offset == OldValue(Offset));

            // Count is equal to count, if the Result is true                        
            Ensures(!Result<bool>() || Count == count);

            // Count is equal to the old Count, if the Result is false            
            Ensures( Result<bool>() || Count == OldValue(Count));

            // IsFixedSize is the same as this's IsFixedSize
            Ensures(IsFixedSize == OldValue(IsFixedSize));

            //IsReadOnly is the same as this's IsReadOnly
            Ensures(IsReadOnly == OldValue(IsReadOnly));

            //IndexingSpeed is the same as this's
            Ensures(IndexingSpeed == OldValue(IndexingSpeed));

            // Result's ContainsSpeed is the same as this's
            Ensures(ContainsSpeed == OldValue(ContainsSpeed));

            // Result's AllowsDuplicates is the same as this's
            Ensures(AllowsDuplicates == OldValue(AllowsDuplicates));

            // Result's DuplicatesByCounting is the same as this's
            Ensures(DuplicatesByCounting == OldValue(DuplicatesByCounting));

            // Result's direction is the same as this's 
            Ensures(Direction == OldValue(Direction));

            // this is empty, if Result is true
            Ensures(!Result<bool>() || IsEmpty == (count == 0));

            // IsEmpty is unchanged, if Result is false
            Ensures(Result<bool>() || IsEmpty == OldValue(IsEmpty));

            // First is correct, if Result is true                        
            Ensures(!Result<bool>() || IsEmpty || First.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(count).First()));

            // First is unchanged, if Result is false
            Ensures( Result<bool>() || First.IsSameAs(OldValue(First)));

            // Last is correct, if Result is true                                  
            Ensures(!Result<bool>() || IsEmpty || Last.IsSameAs(Underlying.Skip(OldValue(Offset) + offset).Take(count).Last()));

            // Last is unchanged, if Result is false
            Ensures( Result<bool>() || Last.IsSameAs(OldValue(Last)));

            // The sliding is correct, if Result is true
            Ensures(!Result<bool>() || this.IsSameSequenceAs(Underlying.Skip(OldValue(Offset) + offset).Take(count)));

            // this is unchanged, if Result is false
            Ensures( Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));

            return default(bool);
        }

        public IList<T> Span(IList<T> otherView)
        {
            // otherView must be non-null
            Requires(otherView != null, ArgumentMustBeNonNull);

            // this must be a view
            Requires(Underlying != null, NotAView);

            // otherView must be a view
            Requires(otherView.Underlying != null, NotAView);

            // this must be valid        
            Requires(IsValid, MustBeValid);

            // otherView must be valid
            Requires(otherView.IsValid, MustBeValid);         

            // Both views must have the same underlying list
            Requires(Underlying == otherView.Underlying, UnderlyingListMustBeTheSame);

            

            // Result's underlying is the same as the underlying of this
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Underlying == Underlying);

            // Result's Offset is the same as this's Offset
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Offset == Offset);

            // Result's Count is equal to the end of the other view minus this's Offset
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Count == otherView.Offset + otherView.Count - Offset);

            // ToArray() is the same as the items beteween the left endpoint of this and right endpoint of otherView.
            Ensures(Result<IList<T>>() == null ||
                    Result<IList<T>>().IsSameSequenceAs(Underlying.Skip(Offset).Take(otherView.Offset + otherView.Count - Offset)));

            // Result's IsFixedSize is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsFixedSize == IsFixedSize);

            // Result's IsReadOnly is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsReadOnly == IsReadOnly);

            // Result's IndexingSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IndexingSpeed == IndexingSpeed);

            // Result's ContainsSpeed is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().ContainsSpeed == ContainsSpeed);

            // Result's AllowsDuplicates is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().AllowsDuplicates == AllowsDuplicates);

            // Result's DuplicatesByCounting is the same as this's
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().DuplicatesByCounting == DuplicatesByCounting);

            // Result is empty if this is
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty == (Result<IList<T>>().Count == 0));
            
            // Result's First is the same as this's First
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().First.IsSameAs(First));

            // Result's Last is the same as otherView's Last
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsEmpty || Result<IList<T>>().Last.IsSameAs(otherView.Last));
            
            // Result's direction is the same as this's 
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().Direction == this.Direction);

            //Result is correct
            Ensures(Result<IList<T>>() == null || Result<IList<T>>().IsSameSequenceAs(Underlying.Skip(Offset).Take(otherView.Offset + otherView.Count - Offset)));


            return default(IList<T>);
        }
        // View ===========        

        public T this[int index]
        {
            get {
                // Argument must be within bounds
                Requires(0 <= index, ArgumentMustBeWithinBounds);
                Requires(index < Count, ArgumentMustBeWithinBounds);

                // Result is the same as skipping the first index items
                Ensures(Result<T>().IsSameAs(this.ElementAt(index)));

                return default(T);
            }

            set {
                // Collection must be non-read-only
                Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

                // Argument must be non-null if collection disallows null values
                Requires(AllowsNull || value != null, ItemMustBeNonNull);

                // Argument must be within bounds
                Requires(0 <= index, ArgumentMustBeWithinBounds);
                Requires(index < Count, ArgumentMustBeWithinBounds);

                // Collection must not already contain item if collection disallows duplicate values
                Requires(AllowsDuplicates || !Contains(value), CollectionMustAllowDuplicates);

                // Value is the same as skipping the first index items
                Ensures(value.IsSameAs(this[index]));

                return;
            }
        }

        public void Clear()
        {
            // No additional preconditions allowed


            // No postconditions


            return;
        }

        public int IndexOf(T item)
        {
            // No additional preconditions allowed

            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);

            return default(int);
        }

        public string Print()
        {
            return default(string);
        }

        public void Insert(int index, T item)
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);     

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            // Collection must not already contain item if collection disallows duplicate values
            Requires(AllowsDuplicates || !Contains(item), CollectionMustAllowDuplicates);

            // Item is inserted at index            
            Ensures(item.IsSameAs(this[index]));

            // The item is inserted into the list without replacing other items            
            Ensures(this.IsSameSequenceAs(OldValue(this.Take(index).Append(item).Concat(this.Skip(index)).ToList())));

            return;
        }

        public void InsertFirst(T item)
        {
            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must not already contain item if collection disallows duplicate values
            Requires(AllowsDuplicates || !Contains(item), CollectionMustAllowDuplicates);


            // The collection becomes non-empty
            Ensures(!IsEmpty);

            // Adding an item increases the count by one
            Ensures(Count == OldValue(Count) + 1);

            // The collection will contain the item added
            Ensures(Contains(item));

            // The number of equal items increase by one
            Ensures(CountDuplicates(item) == OldValue(CountDuplicates(item)) + 1);

            // The number of same items increase by one
            Ensures(this.CountSame(item) == OldValue(this.CountSame(item)) + 1);

            // The item is added to the beginning            
            Ensures(item.IsSameAs(First));


            return;
        }

        public void InsertLast(T item)
        {
            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || item != null, ItemMustBeNonNull);

            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must not already contain item if collection disallows duplicate values
            Requires(AllowsDuplicates || !Contains(item), CollectionMustAllowDuplicates);


            // The collection becomes non-empty
            Ensures(!IsEmpty);

            // Adding an item increases the count by one
            Ensures(Count == OldValue(Count) + 1);

            // The collection will contain the item added
            Ensures(Contains(item));

            // The number of equal items increase by one
            Ensures(CountDuplicates(item) == OldValue(CountDuplicates(item)) + 1);

            // The number of same items increase by one
            Ensures(this.CountSame(item) == OldValue(this.CountSame(item)) + 1);

            // The item is added to the end
            Ensures(item.IsSameAs(Last));

            return;
        }

        public void InsertRange(int index, SCG.IEnumerable<T> items)
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be within bounds
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);

            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);

            // Argument must be non-null if collection disallows null values
            Requires(AllowsNull || ForAll(items, item => item != null), ItemsMustBeNonNull);

            // Collection must not already contain the items if collection disallows duplicate values
            Requires(AllowsDuplicates || ForAll(items, item => !Contains(item)), CollectionMustAllowDuplicates);

            // enumrable to add must not contain duplicates
            Requires(AllowsDuplicates || items.ItemsAreUnique(), CollectionMustAllowDuplicates);

            // The items are inserted into the list without replacing other items
            Ensures(this.IsSameSequenceAs(OldValue(this.Take(index).Concat(items).Concat(this.Skip(index)).ToList())));

            // Collection doesn't change if enumerator throws an exception
            EnsuresOnThrow<Exception>(this.IsSameSequenceAs(OldValue(ToArray())));

            return;
        }

        public bool IsSorted()
        {
            // No preconditions
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // True if sorted
            Ensures(Result<bool>() == CollectionExtensions.IsSorted(this));


            return default(bool);
        }

        public bool IsSorted(SCG.IComparer<T> comparer)
        {
            // No preconditions
            Requires(IsValid, MustBeValid);

            // True if sorted
            Ensures(Result<bool>() == CollectionExtensions.IsSorted(this, comparer));


            return default(bool);
        }

        public bool IsSorted(Comparison<T> comparison)
        {
            Requires(IsValid, MustBeValid);
            // Argument must be non-null
            Requires(comparison != null, ArgumentMustBeNonNull);



            // True if sorted
            Ensures(Result<bool>() == CollectionExtensions.IsSorted(this, comparison));


            return default(bool);
        }

        public T RemoveAt(int index) 
        {
            // is Valid, not disposed
            Requires(IsValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Argument must be within bounds (collection must be non-empty)
            Requires(0 <= index, ArgumentMustBeWithinBounds);
            Requires(index < Count, ArgumentMustBeWithinBounds);


            // Result is the item previously at the specified index            
            Ensures(Result<T>().IsSameAs(OldValue(this[index])));

            // Only the item at index is removed
            Ensures(this.IsSameSequenceAs(OldValue(this.SkipIndex(index).ToList())));

            // Result is non-null
            Ensures(AllowsNull || Result<T>() != null);

            // Removing an item decreases the count by one
            Ensures(Count == OldValue(Count) - 1);

            return default(T);
        }

        public T RemoveFirst()
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Collection must be non-empty
            Requires(!IsEmpty, CollectionMustBeNonEmpty);

            // Dequeuing an item decreases the count by one
            Ensures(Count == OldValue(Count) - 1);

            // Result is non-null
            Ensures(AllowsNull || Result<T>() != null);

            // Result is the same the first items            
            Ensures(Result<T>().IsSameAs(OldValue(First)));

            // Only the first item in the queue is removed
            Ensures(this.IsSameSequenceAs(OldValue(this.Skip(1).ToList())));


            return default(T);
        }

        public T RemoveLast()
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Collection must be non-fixed-sized
            Requires(!IsFixedSize, CollectionMustBeNonFixedSize);

            // Collection must be non-empty
            Requires(!IsEmpty, CollectionMustBeNonEmpty);


            // Dequeuing an item decreases the count by one
            Ensures(Count == OldValue(Count) - 1);

            // Result is non-null
            Ensures(AllowsNull || Result<T>() != null);

            // Result is the same the first items            
            Ensures(Result<T>().IsSameAs(OldValue(Last)));

            // Only the last item in the queue is removed
            Ensures(this.IsSameSequenceAs(OldValue(this.Take(Count - 1).ToList())));


            return default(T);
        }

        public void Reverse()
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // The collection is reversed                        
            Ensures(this.IsSameSequenceAs(OldValue(Enumerable.Reverse(this).ToList())));    
                    
            return;
        }

        public void Shuffle()
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // The collection remains the same            
            Ensures(this.HasSameAs(OldValue(ToArray())));


            return;
        }

        public void Shuffle(Random random)
        {
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Argument must be non-null
            Requires(random != null, ArgumentMustBeNonNull);


            // The collection remains the same
            // !@ 
            Ensures(this.HasSameAs(OldValue(ToArray())));


            return;
        }

        public void Sort()
        {
            // is Valid, not disposed
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);


            // List becomes sorted
            Ensures(IsSorted());

            // The collection remains the same
            Ensures(this.HasSameAs(OldValue(this.ToArray())));

            return;
        }

        public void Sort(Comparison<T> comparison)
        {
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);

            // Argument must be non-null
            Requires(comparison != null, ArgumentMustBeNonNull);


            // List becomes sorted
            Ensures(IsSorted(comparison));

            // The collection remains the same
            Ensures(this.HasSameAs(OldValue(ToArray())));


            return;
        }

        public void Sort(SCG.IComparer<T> comparer)
        {
            Requires(IsValid, MustBeValid);

            // Collection must be non-read-only
            Requires(!IsReadOnly, CollectionMustBeNonReadOnly);


            // List becomes sorted
            Ensures(IsSorted(comparer));

            // The collection remains the same
            Ensures(this.HasSameAs(OldValue(this.ToArray())));

            return;
        }

        #region Hardened Postconditions

        // Static checker shortcoming: https://github.com/Microsoft/CodeContracts/issues/331
        public bool Add(T item)
        {
            // No additional preconditions allowed


            // Item is placed at the end
            Ensures(Last.IsSameAs(item));


            return default(bool);
        }

        // Static checker shortcoming: https://github.com/Microsoft/CodeContracts/issues/331
        public bool IsSynchronized
        {
            get {
                // No preconditions


                // Always false
                Ensures(!Result<bool>());


                return default(bool);
            }
        }

        public int LastIndexOf(T item)
        {
            // No additional preconditions allowed


            // Result is a valid index
            Ensures(Contains(item)
                ? 0 <= Result<int>() && Result<int>() < Count
                : ~Result<int>() == Count);


            return default(int);
        }

        public bool Remove(T item)
        {
            // No extra preconditions allowed


            // Removes the last occurrence of the item
            Ensures(!Result<bool>() || this.IsSameSequenceAs(OldValue(this.SkipIndex(LastIndexOf(item)).ToList()))); // TODO: Is ToList needed?


            return default(bool);
        }

        public bool Remove(T item, out T removedItem)
        {
            // No extra preconditions allowed


            // If an item is removed, it is the last equal to item
            Ensures(!Result<bool>() || this.IsSameSequenceAs(OldValue(this.SkipIndex(LastIndexOf(item)).ToList()))); // TODO: Is ToList needed?

            // The item removed is the last equal to item
            Ensures(!Result<bool>() || ValueAtReturn(out removedItem).IsSameAs(OldValue(this[LastIndexOf(item)])));


            removedItem = default(T);
            return default(bool);
        }

        public bool RemoveDuplicates(T item)
        {
            // No extra preconditions allowed


            // The list is the same as the old collection without item
            Ensures(this.IsSameSequenceAs(OldValue(this.Where(x => !EqualityComparer.Equals(x, item)).ToList())));


            return default(bool);
        }

        public bool RemoveRange(SCG.IEnumerable<T> items)
        {
            // No extra preconditions allowed


            //  
            Ensures(false); // TODO: Write contract


            return default(bool);
        }

        public bool RetainRange(SCG.IEnumerable<T> items)
        {
            // No extra preconditions allowed


            //  
            Ensures(false); // TODO: Write contracts


            return default(bool);
        }

        public bool Update(T item)
        {
            // No extra preconditions allowed


            // Updates the last occurrence of the item
            Ensures(!Result<bool>() || this.IsSameSequenceAs(OldValue(this.Take(LastIndexOf(item)).Append(item).Concat(this.Skip(LastIndexOf(item) + 1)).ToList()))); // TODO: Is ToList needed?


            return default(bool);
        }

        public bool Update(T item, out T oldItem)
        {
            // No extra preconditions allowed


            // Updates the last occurrence of the item
            Ensures(!Result<bool>() || this.IsSameSequenceAs(OldValue(this.Take(LastIndexOf(item)).Append(item).Concat(this.Skip(LastIndexOf(item) + 1)).ToList()))); // TODO: Is ToList needed?

            // The item removed is the last equal to item
            Ensures(!Result<bool>() || ValueAtReturn(out oldItem).IsSameAs(OldValue(this[LastIndexOf(item)])));


            oldItem = default(T);
            return default(bool);
        }

        public bool UpdateOrAdd(T item)
        {
            // No extra preconditions allowed


            // If an item is updated, it is the last occurrence
            Ensures(this.IsSameSequenceAs(OldValue((Result<bool>() ? this.Take(LastIndexOf(item)).Append(item).Concat(this.Skip(LastIndexOf(item) + 1)) : this.Append(item)).ToList()))); // TODO: Is ToList needed?


            return default(bool);
        }

        public bool UpdateOrAdd(T item, out T oldItem)
        {
            // No extra preconditions allowed


            // If an item is updated, it is the last occurrence
            Ensures(this.IsSameSequenceAs(OldValue((Result<bool>() ? this.Take(LastIndexOf(item)).Append(item).Concat(this.Skip(LastIndexOf(item) + 1)) : this.Append(item)).ToList()))); // TODO: Is ToList needed?

            // The item removed is the last equal to item
            Ensures(!Result<bool>() || ValueAtReturn(out oldItem).IsSameAs(OldValue(this[LastIndexOf(item)])));


            oldItem = default(T);
            return default(bool);
        }

        #endregion

        // ReSharper restore InvocationIsSkipped

        #region Non-Contract Methods

        #region SCG.IEnumerable<T>

        public abstract SCG.IEnumerator<T> GetEnumerator();
        SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region IShowable

        public abstract string ToString(string format, IFormatProvider formatProvider);
        public abstract bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider);

        #endregion

        #region IDisposable
        public abstract void Dispose();                           
        #endregion

        #region ICollectionValue<T>

        public abstract bool IsValid { get; }
        public abstract bool AllowsNull { get; }
        public abstract Speed CountSpeed { get; }
        public abstract bool IsEmpty { get; }
        public abstract T Choose();
        public abstract T[] ToArray();

        #endregion

        #region IListenable<T>

        public abstract EventTypes ActiveEvents { get; }
        public abstract EventTypes ListenableEvents { get; }
        public abstract event EventHandler CollectionChanged;
        public abstract event EventHandler<ClearedEventArgs> CollectionCleared;
        public abstract event EventHandler<ItemAtEventArgs<T>> ItemInserted;
        public abstract event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt;
        public abstract event EventHandler<ItemCountEventArgs<T>> ItemsAdded;
        public abstract event EventHandler<ItemCountEventArgs<T>> ItemsRemoved;

        #endregion

        #region IDirectedCollectionValue<T>

        public abstract EnumerationDirection Direction { get; }
        public abstract IDirectedCollectionValue<T> Backwards();

        #endregion

        #region IExtensible

        public abstract bool AllowsDuplicates { get; }
        public abstract bool DuplicatesByCounting { get; }
        public abstract SCG.IEqualityComparer<T> EqualityComparer { get; }
        public abstract bool AddRange(SCG.IEnumerable<T> items);

        #endregion

        #region SC.ICollection

        public abstract object SyncRoot { get; }
        public abstract void CopyTo(Array array, int index);        

        #endregion

        #region SCG.ICollection<T>

        void SCG.ICollection<T>.Add(T item) {}
        void SCG.IList<T>.Insert(int index, T item) {}

        #endregion
        
        #region ICollection<T>

        public abstract Speed ContainsSpeed { get; }
        public abstract bool Contains(T item);
        public abstract bool ContainsRange(SCG.IEnumerable<T> items);
        public abstract void CopyTo(T[] array, int arrayIndex);
        public abstract int CountDuplicates(T item);
        public abstract bool Find(ref T item);
        public abstract ICollectionValue<T> FindDuplicates(T item);
        public abstract bool FindOrAdd(ref T item);
        public abstract int GetUnsequencedHashCode();
        public abstract ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities();
        public abstract ICollectionValue<T> UniqueItems();
        public abstract bool UnsequencedEquals(ICollection<T> otherCollection);

        #endregion

        #region ISequenced<T>

        public abstract int GetSequencedHashCode();
        public abstract bool SequencedEquals(ISequenced<T> otherCollection);

        #endregion

        #region IIndexed<T>

        public abstract Speed IndexingSpeed { get; }        
        public abstract IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count);
        public abstract void RemoveIndexRange(int startIndex, int count);

        #endregion

        #region SC.IList

        object SC.IList.this[int index]
        {
            get { return default(object); }
            set { }
        }

        public abstract int Add(object value);
        public abstract bool Contains(object value);
        public abstract int IndexOf(object value);
        void SC.IList.Insert(int index, object value) {}
        public abstract void Remove(object value);
        void SC.IList.RemoveAt(int index) {}

        #endregion

        #region SCG.IList<T>

        void SCG.IList<T>.RemoveAt(int index) {}

        #endregion

        #endregion
    }
}