﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using C6.Contracts;

using static System.Diagnostics.Contracts.Contract;

using static C6.Collections.ExceptionMessages;
using static C6.Contracts.ContractMessage;
using static C6.EventTypes;
using static C6.Speed;

using SC = System.Collections;
using SCG = System.Collections.Generic;


namespace C6.Collections
{
    public class HashedArrayList<T> : IList<T>, IStack<T>
    {
        #region Fields

        private static readonly T[] EmptyArray = new T[0];
        private const int MinArrayLength = 0x00000004;
        private const int MaxArrayLength = 0x7FEFFFFF;

        private T[] _items;
        private SCG.Dictionary<T, int> _itemIndex; // TODO: use C5.HashTable, when implemented
        private WeakViewList<HashedArrayList<T>> _views;
        private WeakViewList<HashedArrayList<T>>.Node _myWeakReference;
        private HashedArrayList<T> _underlying;

        private event EventHandler _collectionChanged;
        private event EventHandler<ClearedEventArgs> _collectionCleared;
        private event EventHandler<ItemAtEventArgs<T>> _itemInserted, _itemRemovedAt;
        private event EventHandler<ItemCountEventArgs<T>> _itemsAdded, _itemsRemoved;

        private int _version, _sequencedHashCodeVersion = -1, _unsequencedHashCodeVersion = -1;
        private int _sequencedHashCode, _unsequencedHashCode;

        private int UnderlyingCount => (Underlying ?? this).Count;

        #endregion Fields

        #region Code Contracts

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            // ReSharper disable InvocationIsSkipped

            // Array is non-null
            Invariant(_items != null);

            // Count is not bigger than the capacity
            Invariant(Count <= Capacity);

            // UnderlyingCount is not bigger than the capacity
            Invariant(UnderlyingCount <= Capacity);

            // All items must be non-null if collection disallows null values
            Invariant(AllowsNull || ForAll(this, item => item != null));

            // The unused part of the array contains default values
            Invariant(ForAll(Count, Capacity, i => Equals(_items[i], default(T))));

            // Equality comparer is non-null
            Invariant(EqualityComparer != null);

            // Empty array is always empty
            Invariant(EmptyArray.IsEmpty());

            // The number of items is equal to the number of items in the dictionary 
            Invariant(UnderlyingCount == _itemIndex.Count);

            // _itemIndex contains the items of the backing array and each item is in the right index 
            Invariant(ForAll(0, Count, i => {
                int value;
                return _itemIndex.TryGetValue(_items[i], out value) && value == i;
            }));

            #region View invariants             

            // Offset is non-negative
            Invariant(Offset >= 0);

            // The list end is less than or equal to the count of the underlying list
            Invariant(Offset + Count <= UnderlyingCount);

            // Views have the same backing _items array
            Invariant((_underlying ?? this)._views == null ||
                      ForAll((_underlying ?? this)._views, view => view._items == (_underlying ?? this)._items)
            );

            #endregion

            // ReSharper restore InvocationIsSkipped
        }

        #endregion

        #region Constructors

        public HashedArrayList()
            : this(0) { }

        public HashedArrayList(SCG.IEnumerable<T> items, SCG.IEqualityComparer<T> equalityComparer = null)
        {
            #region Code Contracts

            // ReSharper disable InvocationIsSkipped
            // Argument must be non-null
            Requires(items != null, ArgumentMustBeNonNull);

            // not the same instance
            Ensures(!ReferenceEquals(_items, items));

            // ReSharper enable InvocationIsSkipped
            #endregion

            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default;

            _items = EmptyArray;
            _itemIndex = new SCG.Dictionary<T, int>(EqualityComparer);
            AddRange(items);
        }

        public HashedArrayList(int capacity = 0, SCG.IEqualityComparer<T> equalityComparer = null)
        {
            #region Code Contracts                       

            // ReSharper disable InvocationIsSkipped

            // Argument must be non-negative
            Requires(0 <= capacity, ArgumentMustBeNonNegative);

            // ReSharper restore InvocationIsSkipped            

            

            #endregion

            IsValid = true;
            EqualityComparer = equalityComparer ?? SCG.EqualityComparer<T>.Default; // !!! should be before Capacity = capacity
            Capacity = capacity;
        }

        public HashedArrayList(SCG.IEqualityComparer<T> equalityComparer = null)
            : this(0, equalityComparer) { }

        #endregion

        #region Properties

        public int Capacity
        {
            get { return _items.Length; }
            set {
                #region Code Contracts

                //Capacity should at least as big the number of items
                Requires(value >= Count);

                // Capacity is at least as big as the number of items
                Ensures(Capacity >= Count);

                Ensures(Capacity == value);

                #endregion

                if (value > 0) {
                    if (value == _items.Length) {
                        return;
                    }

                    Array.Resize(ref (_underlying ?? this)._items, value);

                    if (_views == null)
                        return;
                    foreach (var v in _views) {
                        v._items = (_underlying ?? this)._items;
                    }
                }
                else {
                    _items = EmptyArray;
                    _itemIndex = new SCG.Dictionary<T, int>(EqualityComparer); // EqualityComparer !!!
                }
            }
        } 

        #region ICollectionValue

        public bool IsValid { get; private set; }

        public int Count { get; protected set; }

        public bool AllowsNull => false; 

        public Speed CountSpeed => Constant;

        public bool IsEmpty => Count == 0; 

        public T Choose() => _items[Offset];

        #endregion

        #region IListenable

        public virtual EventTypes ActiveEvents { get; private set; }
        public virtual EventTypes ListenableEvents => All;

        #endregion

        #region IExtensible

        public bool AllowsDuplicates => false;

        public bool DuplicatesByCounting => true;

        public bool IsFixedSize => false; // can add and remove 

        public bool IsReadOnly => false;

        public virtual SCG.IEqualityComparer<T> EqualityComparer { get; }

        #endregion

        #region ICollection

        public Speed ContainsSpeed => Constant;

        #endregion

        #region ISequenced

        public virtual EnumerationDirection Direction => EnumerationDirection.Forwards;

        #endregion

        #region IIndexed

        public Speed IndexingSpeed => Constant;

        #endregion

        #region IList

        public T First => _items[Offset]; // View: _offset
        public T Last => _items[Offset + Count - 1];
        public virtual int Offset { get; protected set; }
        public virtual IList<T> Underlying => _underlying;

        #endregion

        #endregion

        #region Public methods 

        #region IDisposable

        public virtual void Dispose()
        {
            DisposePrivate(false);
        }

        #endregion

        #region ICollectionValue

        public SCG.IEnumerator<T> GetEnumerator() // overrides valuebase 
        {
            #region Code Contracts

            Ensures(_version == OldValue(_version));

            #endregion

            var version = _version;            
            for (var i = Offset; CheckVersion(version) && i < Offset + Count; i++) {
                yield return _items[i];
            }
        }

        public override string ToString() => ToString(null, null); // to_base(override, the Object's)

        public string ToString(string format, IFormatProvider formatProvider) // to_base, here: no
            => Showing.ShowString(this, format, formatProvider);

        public bool Show(StringBuilder stringBuilder, ref int rest, IFormatProvider formatProvider)
            => Showing.Show(this, stringBuilder, ref rest, formatProvider); // to_base(virtual), here: no       

        public virtual T[] ToArray() // to_base(virtual)
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return array;
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, Offset, array, arrayIndex, Count);
        } //to_base

        #endregion

        #region IExtensible

        public virtual bool Add(T item)
        {
            #region Code Contracts            

            // The version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (FindOrAddToHashPrivate(item, Offset + Count)) {
                return false;
            }

            InsertUnderlyingArrayPrivate(Count, item);
            ReindexPrivate(Offset + Count);
            (_underlying ?? this).RaiseForAdd(item); //*View: 
            return true;
        }

        public virtual bool AddRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // The version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            // TODO: insert range private
            // TODO: Handle ICollectionValue<T> and ICollection<T>
            var array = items.ToArray();
            if (array.IsEmpty()) {
                return false;
            }

            var countToAdd = array.Length;
            EnsureCapacity(UnderlyingCount + countToAdd); 

            var index = Offset + Count; 
            if (index < UnderlyingCount) 
            {
                Array.Copy(_items, index, _items, index + countToAdd, UnderlyingCount - index);
            }
            
            var oldIndex = index;            
            var countAdded = 0;
            foreach (var item in array) {
                if (FindOrAddToHashPrivate(item, index)) {
                    continue;
                }

                _items[index++] = item;
                countAdded++;
            }

            // shrink the space if too much space is allocated
            if (countAdded < countToAdd) {
                Array.Copy(_items, oldIndex + countToAdd, _items, index, UnderlyingCount - oldIndex);
                Array.Clear(_items, UnderlyingCount + countAdded, countToAdd - countAdded); 
            }

            if (countAdded <= 0) {
                return false;
            }

            UpdateVersion();

            Count += countAdded;
            if (_underlying != null) {
                _underlying.Count += countAdded;
            }

            ReindexPrivate(index);
            FixViewsAfterInsertPrivate(countAdded, index - countAdded);            
            (_underlying ?? this).RaiseForAddRange(index - countAdded, countAdded);

            return true;
        }

        #endregion

        #region ICollection

        public virtual ICollectionValue<KeyValuePair<T, int>> ItemMultiplicities()
        {            
            throw new NotImplementedException();
        }

        public virtual bool RetainRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty) {
                return false;
            }

            // proper list and empty
            if (items.IsEmpty() && Underlying == null) {
                // Optimize call, if no items should be retained
                UpdateVersion();

                var itemsRemoved = ToArray();
                ClearPrivate();

                RaiseForRemoveAllWhere(itemsRemoved);
                return true;
            }

            // TODO: Replace ArrayList<T> with more efficient data structure like HashBag<T>
            var itemsToRemove = new ArrayList<T>(items, EqualityComparer, AllowsNull);
            return RemoveAllWherePrivate(item => !itemsToRemove.Remove(item));
        }

        public virtual ICollectionValue<T> UniqueItems() => new ItemSet(this);

        public virtual bool UnsequencedEquals(ICollection<T> otherCollection) => this.UnsequencedEquals(otherCollection, EqualityComparer);

        public virtual bool Update(T item)
        {
            #region Code Contracts          

            // If collection changes, the version is updated
            Contract.Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            T oldItem;
            return Update(item, out oldItem);
        }

        public virtual bool Update(T item, out T oldItem)
        {
            #region Code Contracts            

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            int index;
            if ((index = IndexOf(item)) < 0) {
                oldItem = default(T);
                return false;
            }

            UpdateVersion();

            oldItem = _items[Offset + index]; // View: 
            _items[Offset + index] = item; // View:
            _itemIndex[item] = Offset + index; // View: 


            (_underlying ?? this).RaiseForUpdate(item, oldItem); // View:
            return true;
        }

        public virtual bool UpdateOrAdd(T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            T oldItem;
            return UpdateOrAdd(item, out oldItem);
        }

        public virtual bool UpdateOrAdd(T item, out T oldItem)
        {
            #region Code Contracts                                  

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Update(item, out oldItem)) {
                return true;
            }

            Add(item);
            return false;
        }

        public virtual bool Remove(T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            T removedItem;
            return Remove(item, out removedItem);
        }

        public virtual bool Remove(T item, out T removedItem)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion            
            
            var index = IndexOf(item);
            if (index < 0) {
                removedItem = default(T);
                return false;
            }

            removedItem = RemoveAtPrivate(index);
            (_underlying ?? this).RaiseForRemove(removedItem);
            return true;
        }

        public virtual bool RemoveDuplicates(T item)
            => item == null ? RemoveAllWherePrivate(x => x == null) : RemoveAllWherePrivate(x => Equals(item, x));

        public virtual bool RemoveRange(SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty || items.IsEmpty()) {
                return false;
            }

            // TODO: Replace ArrayList<T> with more efficient data structure like HashBag<T>            
            var itemsToRemove = new SCG.HashSet<T>(items, EqualityComparer);
            return RemoveAllWherePrivate(item => itemsToRemove.Remove(item));
        }

        public virtual bool ContainsRange(SCG.IEnumerable<T> items)
        {
            if (items.IsEmpty()) {
                return true;
            }

            if (IsEmpty) {
                return false;
            }
            
            return items.All(item => _itemIndex.ContainsKey(item));
        }

        public virtual void Clear()
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (IsEmpty) {
                return;
            }

            UpdateVersion(); 

            // a view is calling Clear()
            if (_underlying != null) {
                RemoveIndexRange(0, Count);
                return;
            }

            // a proper list is calling Clear()            
            var oldCount = Count;
            FixViewsBeforeRemovePrivate(0, Count);
            ClearPrivate();
            //(_underlying ?? this).RaiseForClear(oldCount);
            (_underlying ?? this).RaiseForRemoveIndexRange(Offset, oldCount);
        }

        public virtual bool Contains(T item) => IndexOf(item) >= 0;

        public virtual int CountDuplicates(T item) => IndexOf(item) >= 0 ? 1 : 0;

        public virtual bool Find(ref T item)
        {
            int index;
            if ((index = IndexOf(item)) < 0) {
                return false;
            }

            item = _items[Offset + index];
            return true;
        }

        public virtual ICollectionValue<T> FindDuplicates(T item)
        {
            #region Code Contract

            //RequireValidity();

            #endregion

            var duplicates = new Duplicates(this, item);
            //_collValues.Add(duplicates);

            return duplicates;
        }

        public virtual bool FindOrAdd(ref T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Find(ref item)) {
                return true;
            }

            Add(item);
            return false;
        }

        public virtual int GetUnsequencedHashCode()
        {
            if (_unsequencedHashCodeVersion == _version) {
                return _unsequencedHashCode;
            }

            _unsequencedHashCodeVersion = _version;
            _unsequencedHashCode = this.GetUnsequencedHashCode(EqualityComparer);
            return _unsequencedHashCode;
        }

        #endregion

        #region ISequenced

        public virtual IDirectedCollectionValue<T> Backwards()
            => new Range(this, Count - 1, Count, EnumerationDirection.Backwards);

        public virtual int GetSequencedHashCode()
        {
            if (_sequencedHashCodeVersion == _version) {
                return _sequencedHashCode;
            }

            _sequencedHashCodeVersion = _version;
            _sequencedHashCode = this.GetSequencedHashCode(EqualityComparer);
            return _sequencedHashCode;
        }

        public virtual bool SequencedEquals(ISequenced<T> otherCollection)
        {
            return this.SequencedEquals(otherCollection, EqualityComparer);
        }

        #endregion

        #region IIndexed

        public virtual T this[int index]
        {
            get { return _items[Offset + index]; } 
            set {
                UpdateVersion();

                index += Offset; 
                var oldItem = _items[index];

                if (EqualityComparer.Equals(value, oldItem)) {                    
                    _items[index] = value;
                    _itemIndex[value] = index;
                }
                else {                    
                    _items[index] = value;
                    _itemIndex.Remove(oldItem);
                    _itemIndex[value] = index;
                }                

                (_underlying ?? this).RaiseForIndexSetter(oldItem, value, index); 
            }
        }

        public virtual IDirectedCollectionValue<T> GetIndexRange(int startIndex, int count)
            => new Range(this, startIndex, count, EnumerationDirection.Forwards);

        [Pure]
        public virtual int IndexOf(T item)
        {
            int index;
            if (_itemIndex.TryGetValue(item, out index) && Offset <= index && index < Offset + Count)
                //if (_itemIndex.TryGetValue(item, out index))
                return index - Offset; // View

            return ~Count;
        }

        public virtual int LastIndexOf(T item) => IndexOf(item);

        public virtual T RemoveAt(int index)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            var item = RemoveAtPrivate(index);
            (_underlying ?? this).RaiseForRemovedAt(item, Offset + index); // View:
            return item;
        }

        public virtual void RemoveIndexRange(int startIndex, int count)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (count == 0 || IsEmpty) {
                return;
            }

            UpdateVersion();

            startIndex += Offset;
            FixViewsBeforeRemovePrivate(startIndex, count);

            // Alternative: View(start, count).Clear();

            // clean _itemIndex
            var end = startIndex + count;
            for (var i = startIndex; i < end; i++) {
                _itemIndex.Remove(_items[i]);
            }

            //copy; otherwise jump to Clear, no need of extra array operations
            if (startIndex < UnderlyingCount - count) {
                Array.Copy(_items, startIndex + count, _items, startIndex, UnderlyingCount - startIndex - count);
            }

            Count -= count;
            if (_underlying != null) {
                _underlying.Count -= count;
            }

            //clear
            Array.Clear(_items, UnderlyingCount, count);
            ReindexPrivate(startIndex);

            (_underlying ?? this).RaiseForRemoveIndexRange(startIndex, count);
        }

        #endregion

        #region IList

        public virtual void Insert(int index, T item)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            FindOrAddToHashPrivate(item, Offset + index);
            InsertUnderlyingArrayPrivate(index, item);

            ReindexPrivate(Offset + index);
            (_underlying ?? this).RaiseForInsert(Offset + index, item);
        }

        public virtual void InsertFirst(T item) => Insert(0, item);

        public virtual void InsertLast(T item) => Insert(Count, item);

        public virtual void InsertRange(int index, SCG.IEnumerable<T> items)
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            // TODO: Use InsertPrivate()

            // TODO: Handle ICollectionValue<T> and ICollection<T>
            // TODO: Avoid creating an array? Requires a lot of extra code, since we need to properly handle items already added from a bad enumerable
            // A bad enumerator will throw an exception here      

            var array = items.ToArray();
            if (array.IsEmpty()) {
                return;
            }
            var countToAdd = array.Length;

            var inIndex = index; // no need; only for RaiseFor
            index += Offset;
            EnsureCapacity(UnderlyingCount + countToAdd);

            if (index < UnderlyingCount)
                Array.Copy(_items, index, _items, index + countToAdd, UnderlyingCount - index);

            var oldIndex = index;
            var countAdded = 0;
            try {
                foreach (var item in array) {
                    if (FindOrAddToHashPrivate(item, index)) {
                        continue;
                    }
                    _items[index++] = item;
                    countAdded++;
                }

                // shrink the space if too much space is allocated
                if (countAdded < countToAdd) {
                    Array.Copy(_items, oldIndex + countToAdd, _items, index, UnderlyingCount - oldIndex);
                    Array.Clear(_items, UnderlyingCount + countAdded, countToAdd - countAdded); //#to_delete: kolkoto sa ostanali ne zapalnati
                }

                if (countAdded <= 0) {
                    return;
                }

                UpdateVersion();

                Count += countAdded;
                if (_underlying != null) {
                    _underlying.Count += countAdded;
                }

                ReindexPrivate(index);
                FixViewsAfterInsertPrivate(countAdded, index - countAdded); //View; index - countAdded == oldIndex
                (_underlying ?? this).RaiseForInsertRange(Offset + inIndex, array);
            }
            finally {                
            }            
        }

        public virtual bool IsSorted(Comparison<T> comparison) 
        {
            #region Code Contract

            //RequireValidity();

            #endregion

            // TODO: Can we check that comparison doesn't alter the collection?
            for (var i = Offset + 1; i < Offset + Count; i++) 
            {
                if (comparison(_items[i - 1], _items[i]) > 0) {
                    return false;
                }
            }

            return true;
        }

        public virtual bool IsSorted(SCG.IComparer<T> comparer) => IsSorted((comparer ?? SCG.Comparer<T>.Default).Compare);

        public virtual bool IsSorted() => IsSorted(SCG.Comparer<T>.Default);

        public virtual T RemoveFirst() => RemoveAt(0);

        public virtual T RemoveLast() => RemoveAt(Count - 1);

        public virtual void Reverse()
        {
            #region Code Contracts

            // If collection changes, the version is updated
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1) {
                return;
            }

            UpdateVersion();

            //Array.Reverse(_items, Offset, Count); 
            for (int i = 0, length = Count / 2, end = Offset + Count - 1; i < length; i++) {
                var swap = _items[Offset + i];

                _items[Offset + i] = _items[end - i];
                _items[end - i] = swap;
            }

            ReindexPrivate(Offset, Offset + Count);
            DisposeOverlappingViewsPrivate(true); 
            (_underlying ?? this).RaiseForReverse();
        }

        public virtual void Shuffle() => Shuffle(new Random());

        public virtual void Shuffle(Random random) // View:
        {
            #region Code Contracts

            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (Count <= 1) {
                return;
            }

            // Only update version if the collection is shuffled
            UpdateVersion();

            _items.Shuffle(Offset, Count, random);
            DisposeOverlappingViewsPrivate(false); //View:
            ReindexPrivate(Offset, Offset + Count);
            (_underlying ?? this).RaiseForShuffle();
        }

        public virtual void Sort(SCG.IComparer<T> comparer)
        {
            #region Code Contracts

            //RequireValidity();
            // If collection changes, the version is updated            
            Ensures(this.IsSameSequenceAs(OldValue(ToArray())) || _version != OldValue(_version));

            #endregion

            if (comparer == null) {
                comparer = SCG.Comparer<T>.Default;
            }

            if (IsSorted(comparer)) {
                return;
            }

            UpdateVersion();
            Array.Sort(_items, Offset, Count, comparer); // View:
            ReindexPrivate(Offset, Offset + Count);
            DisposeOverlappingViewsPrivate(false); //View: 
            (_underlying ?? this).RaiseForSort();
        }

        public virtual void Sort() => Sort((SCG.IComparer<T>) null);

        public virtual void Sort(Comparison<T> comparison) => Sort(comparison.ToComparer()); // Why ToComparer, but not comparison;

        public virtual bool TrySlide(int offset) => TrySlide(offset, Count);

        public virtual bool TrySlide(int offset, int newCount)
        {
            // check the indices
            var newOffset = Offset + offset;
            if (newOffset < 0 || newCount < 0 || newOffset + newCount > Underlying.Count) {
                return false;
            }

            UpdateVersion();

            // set the new values: offsetField, Count
            Offset = newOffset;
            Count = newCount;
            return true;
        }

        public virtual IList<T> Slide(int offset, int count)
        {
            // There are code contracts checking the offset, count 
            TrySlide(offset, count);
            return this;
        }

        public virtual IList<T> Slide(int offset) => Slide(offset, Count);

        public virtual IList<T> Span(IList<T> other)
        {
            if (other.Offset + other.Count - Offset < 0)
                return null;

            return (_underlying ?? this).View(Offset, other.Offset + other.Count - Offset);
        }

        public virtual IList<T> View(int index, int count)
        {
            #region Code Contracts            

            #endregion

            if (_views == null)
                _views = new WeakViewList<HashedArrayList<T>>();

            var view = (HashedArrayList<T>) MemberwiseClone();

            view.Offset += index;
            view.Count = count;
            view._underlying = _underlying ?? this;
            view._myWeakReference = _views.Add(view);
            return view;
        }

        public virtual IList<T> ViewOf(T item)
        {
            #region Code Contracts            

            #endregion

            int index;
            if ((index = IndexOf(item)) < 0) {
                return null;
            }

            return View(index, 1);
        }

        public virtual IList<T> LastViewOf(T item) => ViewOf(item);

        #endregion

        #region IStack<T>

        public virtual T Pop() => RemoveLast();

        public virtual void Push(T item) => InsertLast(item);

        #endregion

        #endregion

        #region Explicit implementations

        SC.IEnumerator SC.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void SC.ICollection.CopyTo(Array array, int index)
        {
            try {
                Array.Copy(_items, 0, array, index, Count);
            }
            catch (ArrayTypeMismatchException) {
                throw new ArgumentException("Target array type is not compatible with the type of items in the collection.");
            }
        }

        void SCG.ICollection<T>.Add(T item) => Add(item);

        void SCG.IList<T>.RemoveAt(int index) => RemoveAt(index);

        bool SC.ICollection.IsSynchronized => false;

        object SC.ICollection.SyncRoot { get; } = new object();

        int SC.IList.Add(object value)
        {
            try {
                return Add((T) value) ? Count - 1 : -1;
            }
            catch (InvalidCastException) {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        bool SC.IList.Contains(object value) => IsCompatibleObject(value) && Contains((T) value);

        int SC.IList.IndexOf(object value) => IsCompatibleObject(value) ? Math.Max(-1, IndexOf((T) value)) : -1;

        // Explicit implementation is needed, since C6.IList<T>.IndexOf(T) breaks SCG.IList<T>.IndexOf(T)'s precondition: Result<T>() >= -1
        int SCG.IList<T>.IndexOf(T item) => Math.Max(-1, IndexOf(item));

        void SC.IList.Insert(int index, object value)
        {
            try {
                Insert(index, (T) value);
            }
            catch (InvalidCastException) {
                throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
            }
        }

        void SC.IList.Remove(object value)
        {
            if (IsCompatibleObject(value)) {
                Remove((T) value);
            }
        }

        void SC.IList.RemoveAt(int index) => RemoveAt(index);

        object SC.IList.this[int index]
        {
            get { return this[index]; }
            set {
                try {
                    this[index] = (T) value;
                }
                catch (InvalidCastException) {
                    throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.{Environment.NewLine}Parameter name: {nameof(value)}");
                }
            }
        }

        #endregion

        #region Events        

        public event EventHandler CollectionChanged
        {
            add {
                _collectionChanged += value;
                ActiveEvents |= Changed;
            }
            remove {
                _collectionChanged -= value;
                if (_collectionChanged == null) {
                    ActiveEvents &= ~Changed;
                }
            }
        }

        public event EventHandler<ClearedEventArgs> CollectionCleared
        {
            add {
                _collectionCleared += value;
                ActiveEvents |= Cleared;
            }
            remove {
                _collectionCleared -= value;
                if (_collectionCleared == null) {
                    ActiveEvents &= ~Cleared;
                }
            }
        }

        public event EventHandler<ItemAtEventArgs<T>> ItemInserted
        {
            add {
                _itemInserted += value;
                ActiveEvents |= Inserted;
            }
            remove {
                _itemInserted -= value;
                if (_itemInserted == null) {
                    ActiveEvents &= ~Inserted;
                }
            }
        }

        public event EventHandler<ItemAtEventArgs<T>> ItemRemovedAt
        {
            add {
                _itemRemovedAt += value;
                ActiveEvents |= RemovedAt;
            }
            remove {
                _itemRemovedAt -= value;
                if (_itemRemovedAt == null) {
                    ActiveEvents &= ~RemovedAt;
                }
            }
        }

        public event EventHandler<ItemCountEventArgs<T>> ItemsAdded
        {
            add {
                _itemsAdded += value;
                ActiveEvents |= Added;
            }
            remove {
                _itemsAdded -= value;
                if (_itemsAdded == null) {
                    ActiveEvents &= ~Added;
                }
            }
        }

        public event EventHandler<ItemCountEventArgs<T>> ItemsRemoved
        {
            add {
                _itemsRemoved += value;
                ActiveEvents |= Removed;
            }
            remove {
                _itemsRemoved -= value;
                if (_itemsRemoved == null) {
                    ActiveEvents &= ~Removed;
                }
            }
        }

        #endregion

        #region Private

        private bool RemoveAllWherePrivate(Func<T, bool> predicate)
        {            
            #region Code Contract            

            Ensures(Result<bool>() || this.IsSameSequenceAs(OldValue(ToArray())));

            #endregion            

            var shouldRememberItems = ActiveEvents.HasFlag(Removed);
            IExtensible<T> itemsRemoved = null;
            var cntRemoved = 0;
            var viewHandler = new ViewHandler(this);

            // TODO: Use bulk moves - consider using predicate(item) ^ something
            var j = Offset;
            for (var i = Offset; i < Offset + Count; i++) {
                var item = _items[i];
                var predResult = predicate(item);

                if (!predResult) {
                    // Avoid overriding an item with itself
                    if (j != i) {
                        _items[j] = item;
                    }
                    _itemIndex[item] = j;
                    j++; // next "free" place                     
                }

                viewHandler.SkipEndpoints(cntRemoved, i); // TODO: not effective

                if (predResult) {
                    if (shouldRememberItems) {
                        (itemsRemoved ?? (itemsRemoved = new ArrayList<T>(allowsNull: AllowsNull))).Add(item); // TODO: Test allows null
                    }
                    cntRemoved++;
                    _itemIndex.Remove(item);
                    viewHandler.UpdateViewSizesAndCounts(cntRemoved, i + 1);
                }
            }

            // No items were removed
            if (cntRemoved == 0) // (Count == j) 
            {
                Assert(itemsRemoved == null);
                return false;
            }

            // Only update version if items are actually removed
            UpdateVersion();

            viewHandler.UpdateViewSizesAndCounts(cntRemoved, UnderlyingCount);
            // shrink the freed space
            Array.Copy(_items, Offset + Count, _items, j, UnderlyingCount - Offset - Count);

            Count -= cntRemoved;
            if (_underlying != null) {
                _underlying.Count -= cntRemoved;
            }

            // Clean up            
            Array.Clear(_items, UnderlyingCount, cntRemoved);

            ReindexPrivate(j);

            (_underlying ?? this).RaiseForRemoveAllWhere(itemsRemoved);
            return true;
        }

        private bool CheckVersion(int version)
        {
            if (_version == version) {
                return true;
            }

            throw new InvalidOperationException(CollectionWasModified);
        }

        private void ReindexPrivate(int index)
        {
            ReindexPrivate(index, UnderlyingCount);
        }

        private void ReindexPrivate(int startIndex, int endIndex)
        {
            for (var i = startIndex; i < endIndex; i++) {
                _itemIndex[_items[i]] = i; // here 
            }
        }

        private bool FindOrAddToHashPrivate(T item, int index)
        {
            #region Code Contract            

            #endregion

            if (_itemIndex.ContainsKey(item)) {
                return true;
            }
            _itemIndex[item] = index;
            return false;
        }

        /// <summary>
        /// Doesn't include _itemIndex[i] = x; reindex(index)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        private void InsertUnderlyingArrayPrivate(int index, T item)
        {
            #region Code Contracts 

            // Argument must within bounds
            Requires(index >= 0, ArgumentMustBeWithinBounds);
            Requires(index <= Count, ArgumentMustBeWithinBounds);            

            #endregion

            UpdateVersion();
            EnsureCapacity(UnderlyingCount + 1);

            index += Offset; //View:

            // Moves items one to the right
            if (index < UnderlyingCount) {
                Array.Copy(_items, index, _items, index + 1, UnderlyingCount - index);
            }
            _items[index] = item;

            Count++;
            if (_underlying != null) {
                _underlying.Count++;
            }
            // !Reindex is up            
            FixViewsAfterInsertPrivate(1, index);
        }

        private void FixViewsAfterInsertPrivate(int added, int realInsertionIndex)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;

                // in the middle
                if (view.Offset < realInsertionIndex && realInsertionIndex < view.Offset + view.Count)
                    view.Count += added;
                // before the beginning
                if (view.Offset > realInsertionIndex || (view.Offset == realInsertionIndex && view.Count > 0))
                    view.Offset += added;
            }
        }

        private void FixViewsBeforeSingleRemovePrivate(int realRemovalIndex)
        {
            if (_views == null) {
                return;
            }

            foreach (var view in _views) {
                if (view == this) {
                    continue;
                }
                if (view.Offset <= realRemovalIndex && realRemovalIndex < view.Offset + view.Count)
                    view.Count--;
                if (view.Offset > realRemovalIndex)
                    view.Offset--;
            }
        }

        private void FixViewsBeforeRemovePrivate(int start, int count)
        {
            if (_views == null) {
                return;
            }

            var clearend = start + count - 1;
            foreach (var view in _views) {
                if (view == this)
                    continue;

                int viewOffset = view.Offset, viewend = viewOffset + view.Count - 1;
                if (start < viewOffset) {
                    if (clearend < viewOffset)
                        view.Offset = viewOffset - count;
                    else {
                        view.Offset = start;
                        view.Count = clearend < viewend ? viewend - clearend : 0;
                    }
                }
                else if (start <= viewend) {
                    view.Count = clearend <= viewend ? view.Count - count : start - viewOffset;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="otherOffset"></param>
        /// <param name="otherCount"></param>
        /// <returns>The position of View(otherOffset, otherSize) with respect to this view</returns>
        MutualViewPosition ViewPositionPrivate(int otherOffset, int otherCount)
        {
            int end = Offset + Count, otherEnd = otherOffset + otherCount;
            if (otherOffset >= end || otherEnd <= Offset)
                return MutualViewPosition.NonOverlapping;
            if (Count == 0 || (otherOffset <= Offset && end <= otherEnd))
                return MutualViewPosition.Contains;
            if (otherCount == 0 || (Offset <= otherOffset && otherEnd <= end))
                return MutualViewPosition.ContainedIn;
            return MutualViewPosition.Overlapping;
        }

        private void DisposeOverlappingViewsPrivate(bool reverse)
        {
            if (_views == null)
                return;

            foreach (var view in _views) {
                if (view == this)
                    continue;
                switch (ViewPositionPrivate(view.Offset, view.Count)) {
                    case MutualViewPosition.ContainedIn:
                        if (reverse)
                            view.Offset = 2 * Offset + Count - view.Count - view.Offset;
                        else
                            view.Dispose();
                        break;

                    case MutualViewPosition.Overlapping:
                        view.Dispose();
                        break;

                    case MutualViewPosition.Contains:
                    case MutualViewPosition.NonOverlapping:
                        break;
                }
            }
        }

        private void InsertRangePrivate(T[] array, int index)
        {
            // NOT DONE for views!!!

            #region Code Contract

            #endregion

            UpdateVersion();

            var count = array.Length;
            (_underlying ?? this).EnsureCapacity(UnderlyingCount + count); 

            if (index < Count) {
                Array.Copy(_items, index, _items, index + count, Count - index);
            }

            Array.Copy(array, 0, _items, index, count);
            Count += count; // View:            
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            #region Code Contracts            

            #endregion

            if (Capacity >= requiredCapacity) {
                return;
            }

            var newCapacity = Capacity * 2;
            if ((uint) newCapacity > MaxArrayLength) {                
                newCapacity = MaxArrayLength;
            }
            else if (newCapacity < MinArrayLength) {
                newCapacity = MinArrayLength;
            }

            if (newCapacity < requiredCapacity) {
                newCapacity = requiredCapacity;
            }

            Capacity = newCapacity;
        }

        [Pure]
        private bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

        private void ClearPrivate()
        {
            _items = EmptyArray;
            _itemIndex.Clear();
            Count = 0;            
        }

        private T RemoveAtPrivate(int index)
        {
            UpdateVersion();

            index += Offset; 
            FixViewsBeforeSingleRemovePrivate(index); 

            Count--;
            if (_underlying != null) 
            {
                _underlying.Count--;
            }

            var item = _items[index];
            if (index < UnderlyingCount) 
            {
                Array.Copy(_items, index + 1, _items, index, UnderlyingCount - index);
            }
            _items[UnderlyingCount] = default(T);

            _itemIndex.Remove(item);
            ReindexPrivate(index);
            return item;
        }

        private void UpdateVersion()
        {
            _version++;
            if (_underlying != null)
            {
                _underlying._version++;
            }
        }

        private static bool IsCompatibleObject(object value) => value is T || value == null && default(T) == null;

        private void DisposePrivate(bool disposingUnderlying)
        {
            if (!IsValid) {
                return;
            }

            if (_underlying != null) // view calls Dispose
            {
                IsValid = false;
                if (!disposingUnderlying)                    
                    _views?.Remove(_myWeakReference);

                _underlying = null;
                _views = null; 
                _myWeakReference = null;
            }
            else // proper list call
            {
                
                if (_views != null) {
                    foreach (var view in _views)
                        view.DisposePrivate(true);                     
                }

                Clear(); 
            }
        }

        private void RaiseForInsertRange(int index, T[] array)
        {
            if (ActiveEvents.HasFlag(Inserted | Added)) {
                for (var i = 0; i < array.Length; i++) {
                    var item = array[i];
                    OnItemInserted(item, index + i);
                    OnItemsAdded(item, 1);
                }
            }

            OnCollectionChanged();
        }

        private void RaiseForAdd(T item)
        {
            if (ActiveEvents.HasFlag(Added)) {
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForAddRange(int index, int count)
        {
            if (ActiveEvents.HasFlag(Added)) {
                var end = index + count;
                for (var i = index; i < end; i++) {
                    OnItemsAdded(_items[i], 1);
                }
            }

            OnCollectionChanged();
        }

        private void RaiseForClear(int count)
        {
            if (ActiveEvents.HasFlag(Cleared)) {
                OnCollectionCleared(true, count);
            }
            OnCollectionChanged();
        }

        private void RaiseForRemove(T removedItem)
        {
            if (ActiveEvents.HasFlag(Removed)) {
                OnItemsRemoved(removedItem, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForUpdate(T item, T olditem)
        {
            if (ActiveEvents.HasFlag(Added | Removed)) {
                OnItemsRemoved(olditem, 1);
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForIndexSetter(T oldItem, T newItem, int index)
        {
            if (ActiveEvents == None) {
                return;
            }
            OnItemRemovedAt(oldItem, index);
            OnItemsRemoved(oldItem, 1);
            OnItemInserted(newItem, index);
            OnItemsAdded(newItem, 1);
            OnCollectionChanged();
        }

        private void RaiseForRemovedAt(T item, int index)
        {
            if (ActiveEvents.HasFlag(Removed | RemovedAt)) {
                OnItemRemovedAt(item, index);
                OnItemsRemoved(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForInsert(int index, T item)
        {
            if (ActiveEvents.HasFlag(Added | Inserted)) {
                OnItemInserted(item, index);
                OnItemsAdded(item, 1);
            }
            OnCollectionChanged();
        }

        private void RaiseForSort() => OnCollectionChanged();

        private void RaiseForReverse() => OnCollectionChanged();

        private void RaiseForShuffle() => OnCollectionChanged();

        private void RaiseForRemoveAllWhere(SCG.IEnumerable<T> itemsRemoved)
        {
            if (ActiveEvents.HasFlag(Removed)) {
                foreach (var item in itemsRemoved) {
                    OnItemsRemoved(item, 1);
                }
            }
            OnCollectionChanged();
        }

        private void RaiseForRemoveIndexRange(int startIndex, int count)
        {
            if (ActiveEvents.HasFlag(Cleared)) {
                OnCollectionCleared(false, count, startIndex);
            }
            OnCollectionChanged();
        }

        #region InvokingMethods

        private void OnItemsAdded(T item, int count) => _itemsAdded?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnCollectionChanged() => _collectionChanged?.Invoke(this, EventArgs.Empty);

        private void OnCollectionCleared(bool full, int count, int? start = null) => _collectionCleared?.Invoke(this, new ClearedEventArgs(full, count, start));

        private void OnItemsRemoved(T item, int count) => _itemsRemoved?.Invoke(this, new ItemCountEventArgs<T>(item, count));

        private void OnItemInserted(T item, int index) => _itemInserted?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        private void OnItemRemovedAt(T item, int index) => _itemRemovedAt?.Invoke(this, new ItemAtEventArgs<T>(item, index));

        #endregion

        #endregion

        #region Position, PositionComparer and ViewHandler nested types

        [Serializable]
        private class PositionComparer : SCG.IComparer<Position>
        {
            public int Compare(Position a, Position b)
            {
                return a.index.CompareTo(b.index);
            }
        }

        /// <summary>
        /// During RemoveAll, we need to cache the original endpoint indices of views 
        /// </summary>
        private struct Position
        {
            public readonly HashedArrayList<T> view;
            public readonly int index;

            public Position(HashedArrayList<T> view, bool left)
            {
                this.view = view;
                index = left ? view.Offset : view.Offset + view.Count - 1;
            }

            public Position(int index)
            {
                this.index = index;
                view = null;
            }
        }

        /// <summary>
        /// Handle the update of (other) views during a multi-remove operation.
        /// </summary>
        private struct ViewHandler
        {
            HashedArrayList<Position> leftEnds;
            HashedArrayList<Position> rightEnds;
            int _leftEndIndex, _rightEndIndex;
            private readonly int viewCount;

            internal ViewHandler(HashedArrayList<T> list)
            {
                _leftEndIndex = _rightEndIndex = viewCount = 0;
                leftEnds = rightEnds = null;
                if (list._views != null)
                    foreach (HashedArrayList<T> v in list._views)
                        if (v != list) {
                            if (leftEnds == null) {
                                leftEnds = new HashedArrayList<Position>();
                                rightEnds = new HashedArrayList<Position>();
                            }
                            leftEnds.Add(new Position(v, true));
                            rightEnds.Add(new Position(v, false));
                        }
                if (leftEnds == null)
                    return;
                viewCount = leftEnds.Count;
                leftEnds.Sort(new PositionComparer());
                rightEnds.Sort(new PositionComparer());
            }

            /// <summary>
            /// This is to be called with realindex pointing to the first node to be removed after a (stretch of) node that was not removed
            /// </summary>
            /// <param name="removed"></param>
            /// <param name="realindex"></param>
            internal void SkipEndpoints(int removed, int realindex)
            {
                if (viewCount <= 0) {
                    return;
                }

                Position endpoint;
                while (_leftEndIndex < viewCount && (endpoint = leftEnds[_leftEndIndex]).index <= realindex) {
                    HashedArrayList<T> view = endpoint.view;
                    view.Offset = view.Offset - removed;
                    view.Count += removed;
                    _leftEndIndex++;
                }
                while (_rightEndIndex < viewCount && (endpoint = rightEnds[_rightEndIndex]).index < realindex) {
                    endpoint.view.Count -= removed;
                    _rightEndIndex++;
                }
            }

            internal void UpdateViewSizesAndCounts(int removed, int realindex)
            {
                if (viewCount > 0) {
                    Position endpoint;
                    while (_leftEndIndex < viewCount && (endpoint = leftEnds[_leftEndIndex]).index <= realindex) {
                        HashedArrayList<T> view = endpoint.view;
                        view.Offset = view.Offset - removed;
                        view.Count += removed;
                        _leftEndIndex++;
                    }
                    while (_rightEndIndex < viewCount && (endpoint = rightEnds[_rightEndIndex]).index < realindex) {
                        endpoint.view.Count -= removed;
                        _rightEndIndex++;
                    }
                }
            }
        }

        #endregion

        #region Nested types        

        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Duplicates : CollectionValueBase<T>, ICollectionValue<T>
        {
            #region Fields

            private readonly HashedArrayList<T> _base;
            private readonly int _version;
            private readonly T _item;
            private HashedArrayList<T> _list;

            #endregion

            #region Code Contracts

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                // ReSharper disable InvocationIsSkipped

                // All items in the list are equal to the item
                Invariant(_list == null || ForAll(_list, x => _base.EqualityComparer.Equals(x, _item)));

                // All items in the list are equal to the item
                Invariant(_list == null || _list.Count == _base.CountDuplicates(_item));

                // ReSharper restore InvocationIsSkipped
            }

            #endregion

            #region Constructors
            
            public Duplicates(HashedArrayList<T> list, T item)
            {
                #region Code Contracts

                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                #endregion

                _base = list;
                _version = _base._version;
                _item = item;
            }

            #endregion

            #region Properties

            public override bool IsValid
            {
                get { return base.IsValid; }

                protected set { base.IsValid = value; }
            }

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();

                    return List.Count;
                }
            }

            public override Speed CountSpeed
            {
                get {
                    CheckVersion();                                        
                    return Constant;
                }
            }

            public override bool IsEmpty => CheckVersion() & List.IsEmpty;

            #endregion

            #region Public Methods

            public override T Choose()
            {
                CheckVersion();
                return _base.Choose(); 
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                List.CopyTo(array, arrayIndex);
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                #region Code contracts

                Requires(IsValid);

                #endregion

                CheckVersion();
                int index;
                if ((index = _base.IndexOf(_item)) >= 0) {
                    yield return _base._items[index];
                }
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return List.ToArray();
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);

            private ICollectionValue<T> List
            {
                get {
                    if (_list != null) {
                        return _list;
                    }

                    var index = _base.IndexOf(_item);
                    return index < 0 ?
                        new HashedArrayList<T>() :
                        new HashedArrayList<T> { _base._items[index] };
                }
            }

            #endregion
        }


        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class ItemSet : CollectionValueBase<T>, ICollectionValue<T> 
        {
            #region Fields

            private readonly HashedArrayList<T> _base;

            private readonly int _version;
            
            //private SCG.HashSet<T> _set;
            private HashedArrayList<T> _set;

            #endregion

            #region Code Contracts

            [ContractInvariantMethod]
            private void ObjectInvariant()
            {
                // ReSharper disable InvocationIsSkipped

                // Base list is never null
                Invariant(_base != null);

                // Either the set has not been created, or it contains the same as the base list's distinct items
                Invariant(_set == null || _set.UnsequenceEqual(_base.Distinct(_base.EqualityComparer), _base.EqualityComparer));

                // ReSharper restore InvocationIsSkipped
            }

            #endregion

            #region Constructors
            
            public ItemSet(HashedArrayList<T> list)
            {
                #region Code Contracts

                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                #endregion

                _base = list;
                _version = _base._version;
            }

            #endregion

            #region Properties
            
            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();
                    return Set.Count;
                }
            }

            public override Speed CountSpeed
            {
                get {
                    CheckVersion();
                    // TODO: Always use Linear?
                    return _set == null ? Linear : Constant;
                }
            }

            public override bool IsEmpty => CheckVersion() & _base.IsEmpty;

            #endregion

            #region Public Methods

            public override T Choose()
            {
                CheckVersion();
                return _base.Choose();
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                Set.CopyTo(array, arrayIndex);
            }
            
            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);
            
            public override SCG.IEnumerator<T> GetEnumerator()
            {
                // If a set already exists, enumerate that
                if (_set != null) {
                    var enumerator = Set.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext()) {
                        yield return enumerator.Current;
                    }
                }
                // Otherwise, evaluate lazily
                else {
                    //var set = new SCG.HashSet<T>(_base.EqualityComparer);
                    var set = new HashedArrayList<T>(_base.EqualityComparer);

                    var enumerator = _base.GetEnumerator();
                    while (CheckVersion() & enumerator.MoveNext()) {
                        // Only return new items
                        if (set.Add(enumerator.Current)) {
                            yield return enumerator.Current;
                        }
                    }

                    // Save set for later (re)user
                    _set = set;
                }
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return Set.ToArray();
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);
            
            private ICollectionValue<T> Set => _set ?? (_set = new HashedArrayList<T>(_base, _base.EqualityComparer));

            #endregion
        }


        [Serializable]
        [DebuggerTypeProxy(typeof(CollectionValueDebugView<>))]
        [DebuggerDisplay("{DebuggerDisplay}")]
        private sealed class Range : CollectionValueBase<T>, IDirectedCollectionValue<T>
        {
            #region Fields

            private readonly HashedArrayList<T> _base;
            private readonly int _version, _startIndex, _count, _sign;
            private readonly EnumerationDirection _direction;

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Range"/> class that starts at the specified index and spans the next
            ///     <paramref name="count"/> items in the specified direction.
            /// </summary>
            /// <param name="list">
            ///     The underlying <see cref="HashedArrayList{T}"/>.
            /// </param>
            /// <param name="startIndex">
            ///     The zero-based <see cref="HashedArrayList{T}"/> index at which the range starts.
            /// </param>
            /// <param name="count">
            ///     The number of items in the range.
            /// </param>
            /// <param name="direction">
            ///     The direction of the range.
            /// </param>
            public Range(HashedArrayList<T> list, int startIndex, int count, EnumerationDirection direction)
            {
                #region Code Contracts

                // Argument must be non-null
                Requires(list != null, ArgumentMustBeNonNull);

                // Argument must be within bounds
                Requires(-1 <= startIndex, ArgumentMustBeWithinBounds);
                Requires(startIndex < list.Count || startIndex == 0 && count == 0, ArgumentMustBeWithinBounds);

                // Argument must be within bounds
                Requires(0 <= count, ArgumentMustBeWithinBounds);
                Requires(direction.IsForward() ? startIndex + count <= list.Count : count <= startIndex + 1, ArgumentMustBeWithinBounds);

                // Argument must be valid enum constant
                Requires(Enum.IsDefined(typeof(EnumerationDirection), direction), EnumMustBeDefined);


                Ensures(_base != null);
                Ensures(_version == _base._version);
                Ensures(_sign == (direction.IsForward() ? 1 : -1));
                Ensures(-1 <= _startIndex);
                Ensures(_startIndex < _base.Count || _startIndex == 0 && _base.Count == 0);
                Ensures(-1 <= _startIndex + _sign * _count);
                Ensures(_startIndex + _sign * _count <= _base.Count);

                #endregion

                _base = list;
                _version = list._version;
                _sign = (int) direction;
                _startIndex = startIndex;
                _count = count;
                _direction = direction;
            }

            #endregion

            #region Properties

            public override bool AllowsNull => CheckVersion() & _base.AllowsNull;

            public override int Count
            {
                get {
                    CheckVersion();
                    return _count;
                }
            }

            public override Speed CountSpeed
            {
                get {
                    CheckVersion();
                    return Constant;
                }
            }

            public EnumerationDirection Direction
            {
                get {
                    CheckVersion();
                    return _direction;
                }
            }

            #endregion

            #region Public Methods

            public IDirectedCollectionValue<T> Backwards()
            {
                CheckVersion();
                var startIndex = _startIndex + (_count - 1) * _sign;
                var direction = Direction.Opposite();
                return new Range(_base, startIndex, _count, direction);
            }

            public override T Choose()
            {
                CheckVersion();
                // Select the highest index in the range
                var index = _direction.IsForward() ? _startIndex + _count - 1 : _startIndex;
                return _base._items[index]; 
            }

            public override void CopyTo(T[] array, int arrayIndex)
            {
                CheckVersion();
                if (_direction.IsForward()) {
                    // Copy array directly
                    Array.Copy(_base._items, _startIndex, array, arrayIndex, _count); //View:  _base.Offset!!
                }
                else {
                    // Use enumerator instead of copying and then reversing
                    base.CopyTo(array, arrayIndex);
                }
            }

            public override bool Equals(object obj) => CheckVersion() & base.Equals(obj);

            public override SCG.IEnumerator<T> GetEnumerator()
            {
                var items = _base._items;
                for (var i = 0; i < Count; i++) {
                    yield return items[_startIndex + _sign * i]; 
                }
            }

            public override int GetHashCode()
            {
                CheckVersion();
                return base.GetHashCode();
            }

            public override T[] ToArray()
            {
                CheckVersion();
                return base.ToArray();
            }

            #endregion

            #region Private Members

            private string DebuggerDisplay => _version == _base._version ? ToString() : "Expired collection value; original collection was modified since range was created.";

            private bool CheckVersion() => _base.CheckVersion(_version);

            #endregion
        }
        
        private sealed class WeakViewList<V> : SCG.IEnumerable<V> where V : class
        {
            Node start;


            [Serializable]
            internal class Node
            {
                internal WeakReference weakview;
                internal Node prev, next;

                internal Node(V view)
                {
                    weakview = new WeakReference(view);
                }
            }


            internal Node Add(V view)
            {
                Node newNode = new Node(view);
                if (start != null) {
                    start.prev = newNode;
                    newNode.next = start;
                }
                start = newNode;
                return newNode;
            }

            internal void Remove(Node n)
            {
                if (n == start) {
                    start = start.next;
                    if (start != null)
                        start.prev = null;
                }
                else {
                    n.prev.next = n.next;
                    if (n.next != null)
                        n.next.prev = n.prev;
                }
            }

            internal void Clear()
            {
                start = null;
            }

            /// <summary>
            /// Note that it is safe to call views.Remove(view.myWeakReference) if view
            /// is the currently yielded object
            /// </summary>
            /// <returns></returns>
            public SCG.IEnumerator<V> GetEnumerator()
            {
                Node n = start;
                while (n != null) {
                    //V view = n.weakview.Target as V; //This provokes a bug in the beta1 verifyer
                    object o = n.weakview.Target;
                    V view = o is V ? (V) o : null;
                    if (view == null)
                        Remove(n);
                    else
                        yield return view;
                    n = n.next;
                }
            }

            SC.IEnumerator SC.IEnumerable.GetEnumerator() => GetEnumerator();

        }

        #endregion
    }
}