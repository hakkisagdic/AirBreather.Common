// Ported from mt19937-64.c, which contains this copyright header:
/*
   Copyright (C) 2004, Makoto Matsumoto and Takuji Nishimura,
   All rights reserved.                          

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:

     1. Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.

     2. Redistributions in binary form must reproduce the above copyright
        notice, this list of conditions and the following disclaimer in the
        documentation and/or other materials provided with the distribution.

     3. The names of its contributors may not be used to endorse or promote 
        products derived from this software without specific prior written 
        permission.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
   LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
   NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
   SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;
using System.Diagnostics.CodeAnalysis;

using AirBreather.Common.Utilities;

namespace AirBreather.Common.Random
{
    public struct MT19937_64State : IEquatable<MT19937_64State>, IRandomGeneratorState
    {
        internal ulong[] data;
        internal int idx;

        public MT19937_64State(ulong seed)
        {
            this.data = new ulong[312];
            this.data[0] = seed;
            for (int i = 1; i < 312; i++)
            {
                ulong prev = this.data[i - 1];
                this.data[i] = unchecked((6364136223846793005 * (prev ^ (prev >> 62))) + (ulong)i);
            }

            this.idx = 312;
        }

        public MT19937_64State(MT19937_64State copyFrom)
        {
            this.idx = copyFrom.idx;

            if (copyFrom.data == null)
            {
                this.data = null;
            }
            else
            {
                this.data = new ulong[312];
                Buffer.BlockCopy(copyFrom.data, 0, this.data, 0, 312 * sizeof(ulong));
            }
        }

        // TODO: our cousins in other languages allow seeding by multiple input values... support that too.
        public bool IsValid => StateIsValid(this);

        private static bool StateIsValid(MT19937_64State state)
        {
            if (state.data == null)
            {
                return false;
            }

            if (!state.idx.IsInRange(0, 313))
            {
                return false;
            }

            ulong accumulator = 0;
            foreach (ulong value in state.data)
            {
                accumulator |= value;
            }

            return accumulator != 0;
        }

        public static bool Equals(MT19937_64State first, MT19937_64State second)
        {
            if (first.idx != second.idx)
            {
                return false;
            }

            bool firstIsDefault = first.data == null;
            bool secondIsDefault = second.data == null;

            if (firstIsDefault != secondIsDefault)
            {
                return false;
            }

            if (firstIsDefault)
            {
                return true;
            }

            ulong accumulator = 0;
            for (int i = 0; i < 312; i++)
            {
                ulong differentBits = first.data[i] ^ second.data[i];
                accumulator |= differentBits;
            }

            return accumulator == 0;
        }

        public static int GetHashCode(MT19937_64State state)
        {
            int hashCode = HashCodeUtility.Seed;

            hashCode = hashCode.HashWith(state.idx);

            if (state.data == null)
            {
                return hashCode;
            }

            ulong accumulator = 0;
            foreach (ulong value in state.data)
            {
                accumulator ^= value;
            }

            hashCode = hashCode.HashWith(accumulator);

            return hashCode;
        }

        public static string ToString(MT19937_64State state) => ToStringUtility.Begin(state).End();

        public static bool operator ==(MT19937_64State first, MT19937_64State second) => Equals(first, second);
        public static bool operator !=(MT19937_64State first, MT19937_64State second) => !Equals(first, second);
        public override bool Equals(object obj) => obj is MT19937_64State && Equals(this, (MT19937_64State)obj);
        public bool Equals(MT19937_64State other) => Equals(this, other);
        public override int GetHashCode() => GetHashCode(this);
        public override string ToString() => ToString(this);
    }

    public sealed class MT19937_64Generator : IRandomGenerator<MT19937_64State>
    {
        /// <summary>
        /// The size of each "chunk" of bytes that can be generated at a time.
        /// </summary>
        public static readonly int ChunkSize = sizeof(ulong);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        int IRandomGenerator<MT19937_64State>.ChunkSize => ChunkSize;

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        RandomnessKind IRandomGenerator<MT19937_64State>.RandomnessKind => RandomnessKind.PseudoRandom;

        /// <inheritdoc />
        public MT19937_64State FillBuffer(MT19937_64State state, byte[] buffer, int index, int count)
        {
            buffer.ValidateNotNull(nameof(buffer));
            index.ValidateInRange(nameof(index), 0, buffer.Length);

            if (buffer.Length - index < count)
            {
                throw new ArgumentException("Not enough room", nameof(buffer));
            }

            if (index % ChunkSize != 0)
            {
                throw new ArgumentException("Must be a multiple of ChunkSize.", nameof(index));
            }

            if (count % ChunkSize != 0)
            {
                throw new ArgumentException("Must be a multiple of ChunkSize.", nameof(count));
            }

            if (!state.IsValid)
            {
                throw new ArgumentException("State is not valid; use the parameterized constructor to initialize a new instance with the given seed values.", nameof(state));
            }

            FillBufferCore(ref state, buffer, index, count);
            return state;
        }

        private static unsafe void FillBufferCore(ref MT19937_64State state, byte[] buffer, int index, int count)
        {
            state = new MT19937_64State(state);

            fixed (ulong* fData = state.data)
            fixed (byte* fbuf = buffer)
            {
                // count has already been validated to be a multiple of ChunkSize,
                // and so has index, so we can do this fanciness without fear.
                ulong* pbuf = (ulong*)(fbuf + index);
                ulong* pend = pbuf + (count / ChunkSize);
                while (pbuf < pend)
                {
                    if (state.idx == 312)
                    {
                        Twist(fData);
                        state.idx = 0;
                    }

                    ulong x = fData[state.idx++];

                    x ^= (x >> 29) & 0x5555555555555555;
                    x ^= (x << 17) & 0x71D67FFFEDA60000;
                    x ^= (x << 37) & 0xFFF7EEE000000000;
                    x ^= (x >> 43);

                    *(pbuf++) = x;
                }
            }
        }

        private static unsafe void Twist(ulong* vals)
        {
            const ulong Upper33 = 0xFFFFFFFF80000000;
            const ulong Lower31 = 0x000000007FFFFFFF;

            for (int curr = 0; curr < 312; curr++)
            {
                int near = (curr + 1) % 312;
                int far = (curr + 156) % 312;

                ulong x = vals[curr] & Upper33;
                ulong y = vals[near] & Lower31;
                ulong z = vals[far] ^ ((x | y) >> 1);

                vals[curr] = z ^ ((y & 1) * 0xB5026F5AA96619E9);
            }
        }
    }
}
