﻿using System;
using System.Collections.Generic;

namespace AirBreather.Collections
{
    public sealed class StringPool
    {
        // thought: reimplement to cut down on duplicate hash code checks?  we
        // could wind up with really long strings here...
#if NETSTANDARD2_0
        private readonly Dictionary<string, string> pool = new Dictionary<string, string>();
#else
        private readonly HashSet<string> pool = new HashSet<string>();
#endif

        public string Pool(string val)
        {
            // null is already in a "pool" of its own.
            if (val == null)
            {
                return val;
            }

            // if it's interned, then we don't need to store it separately.
            string result = string.IsInterned(val);
            if (result != null)
            {
                return result;
            }

            if (!this.pool.TryGetValue(val, out result))
            {
                result = val;
#if NETSTANDARD2_0
                this.pool[val] = val;
#else
                this.pool.Add(val);
#endif
            }

            return result;
        }

        public string IsPooled(string val)
        {
            // null is already in a "pool" of its own.
            if (val == null)
            {
                return val;
            }

            // if it's interned, then we don't need to check our separate pool.
            string result = string.IsInterned(val);
            if (result != null)
            {
                return result;
            }

            // thought: if something became interned after we pooled it all
            // custom-like, then maybe we should replace our reference with
            // a reference to the interned string?
            this.pool.TryGetValue(val, out val);

            return val;
        }

        public void Clear() => this.pool.Clear();
    }
}
