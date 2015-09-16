﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FuncSharp
{
    public abstract class DataCube<TPosition, TValue>
        where TPosition : IProduct
    {
        /// <summary>
        /// Creates a new empty data cube.
        /// </summary>
        protected DataCube()
        {
            Index = new Dictionary<TPosition, TValue>();
        }

        /// <summary>
        /// Positions of all values stored in the cube.
        /// </summary>
        public IEnumerable<TPosition> Positions
        {
            get { return Index.Keys; }
        }

        /// <summary>
        /// All values stored in the cube.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get { return Index.Values; }
        }

        /// <summary>
        /// Values in the cube indexed by their positions.
        /// </summary>
        private Dictionary<TPosition, TValue> Index { get; set; }

        /// <summary>
        /// Returns whether the cube is empty.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return !Index.Any();
        }

        /// <summary>
        /// Returns whether the cube contains a value at the specified position.
        /// </summary>
        public bool Contains(TPosition position)
        {
            return Index.ContainsKey(position);
        }

        /// <summary>
        /// Returns value at the specified position.
        /// </summary>
        public IOption<TValue> Get(TPosition position)
        {
            TValue result;
            if (Index.TryGetValue(position, out result))
            {
                return Option.Valued(result);
            }
            return Option.Empty<TValue>();
        }

        /// <summary>
        /// Returns value at the specified position. If there is no value present, sets the position to value generated by 
        /// the <paramref name="setter"/> function and returns the newly generated value.
        /// </summary>
        public TValue GetOrElseSet(TPosition position, Func<TValue> setter)
        {
            return Get(position).GetOrElse(_ => Set(position, setter()));
        }

        /// <summary>
        /// Sets value at the specified position. If there is value already present at that position, overwrites it.
        /// </summary>
        public virtual TValue Set(TPosition position, TValue value)
        {
            Index[position] = value;
            return value;
        }

        /// <summary>
        /// Sets value at the specified position. If there is value already present at that position, updates it with the
        /// result of the <paramref name="updater"/> function which is given the present value and the new value.
        /// </summary>
        public virtual TValue SetOrElseUpdate(TPosition position, TValue value, Func<TValue, TValue, TValue> updater)
        {
            return Set(position, Get(position).Match(
                v => updater(v, value),
                _ => value
            ));
        }

        /// <summary>
        /// For each value in the cube, invokes the specified function passing in the position and the stored value.
        /// </summary>
        public void ForEach(Action<TPosition, TValue> a)
        {
            foreach (var kv in Index)
            {
                a(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Transforms the current cube into a new cube. The transformation is directed by two functions. 
        /// The <paramref name="positionMapper"/> maps positions of values in the current cube into positions 
        /// in the new cube. If there are multiple values in the current cube, whose positions are mapped onto 
        /// the same position in the new cube, then the <paramref name="aggregator"/> function is used to 
        /// aggregate all the colliding values into one value.
        /// </summary>
        public TNewCube Transform<TNewCube, TNewPosition>(Func<TPosition, TNewPosition> positionMapper, Func<TValue, TValue, TValue> aggregator)
            where TNewCube : DataCube<TNewPosition, TValue>, new()
            where TNewPosition : IProduct
        {
            var result = new TNewCube();
            ForEach((position, value) => result.SetOrElseUpdate(positionMapper(position), value, aggregator));
            return result;
        }

        protected void AddDomain<P>(Dictionary<IProduct1<P>, int> rangeCounts, P value)
        {
            var key = Product.Create(value);
            var count = 0;
            rangeCounts.TryGetValue(key, out count);
            rangeCounts[key] = count + 1;
        }

        protected void RemoveDomain<P>(Dictionary<IProduct1<P>, int> rangeCounts, P value)
        {
            var key = Product.Create(value);
            var count = rangeCounts[key];
            if (count == 1)
            {
                rangeCounts.Remove(key);
            }
            else
            {
                rangeCounts[key] = count - 1;
            }
        }
    }
}