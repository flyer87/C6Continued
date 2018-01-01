﻿using SCG = System.Collections.Generic;

using static C6.EventTypes;
using static C6.Speed;

using C6.Collections;
using C6.Tests.Helpers;
using NUnit.Framework;


namespace C6.Tests.Collections
{
    public class LinkedListQueueTests : IQueueTests
    {
        protected override bool AllowsNull => true;
        protected override EventTypes ListenableEvents => All;
        protected override bool IsReadOnly => false;

        protected override IQueue<T> GetEmptyQueue<T>(bool allowsNull = false)
            => new LinkedList<T>(allowsNull : allowsNull);

        protected override IQueue<T> GetQueue<T>(SCG.IEnumerable<T> enumerable, bool allowsNull = false)
            => new LinkedList<T>(enumerable, allowsNull : allowsNull);
    }

    [TestFixture]
    public class LinkedListTests : TestBase
    {
    }

    [TestFixture]
    public class LinkedListViewTests : GeneralViewTests
    {
        protected override IList<T> GetEmptyList<T>(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(equalityComparer, allowsNull);

        protected override IList<T> GetList<T>(SCG.IEnumerable<T> enumerable, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(enumerable, equalityComparer, allowsNull);
    }

    [TestFixture]
    public class LinkedListListTests : IListTests
    {
        protected override bool AllowsNull => true;
        protected override EventTypes ListenableEvents => All;

        protected override bool AllowsDuplicates => true;
        protected override bool DuplicatesByCounting => false;
        protected override bool IsFixedSize => false;
        protected override bool IsReadOnly => false;
        protected override Speed ContainsSpeed => Linear;
        protected override Speed IndexingSpeed => Linear;

        protected override IList<T> GetEmptyList<T>(SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(equalityComparer, allowsNull);

        protected override IList<T> GetList<T>(SCG.IEnumerable<T> enumerable, SCG.IEqualityComparer<T> equalityComparer = null, bool allowsNull = false)
            => new LinkedList<T>(enumerable, equalityComparer, allowsNull);        
    }

    [TestFixture]
    public class LinkedListStackTests : IStackTests
    {
        protected override bool AllowsNull => true;
        protected override EventTypes ListenableEvents => All;
        protected override bool IsReadOnly => false;

        protected override IStack<T> GetEmptyStack<T>(bool allowsNull = false)
            => new LinkedList<T>(allowsNull: allowsNull);

        protected override IStack<T> GetStack<T>(SCG.IEnumerable<T> enumerable, bool allowsNull = false)
            => new LinkedList<T>(enumerable, allowsNull: allowsNull);
    }
}
