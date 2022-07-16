using System;
using System.Collections;

using Needlefish.Enumerators;

namespace Needlefish.Types
{
    /// <summary>
    /// A memory-efficient container for up to 8 boolean values using a byte.
    /// </summary>
    public struct MultiBool : IEnumerable
    {
        public const int Count = 8;

        internal byte Value;

        public MultiBool(byte value)
        {
            Value = value;
        }
        
        public bool this[int index] {
            get {
                if (index >= Count)
                    throw new IndexOutOfRangeException($"Index {index} is greater than length 8.");

                return (Value & (1 << index)) != 0;
            }
            set {
                if (index >= Count)
                    throw new IndexOutOfRangeException($"Index {index} is greater than length 8.");
                
                if (value)
                    Value = (byte)(Value | (1 << index));
                else
                    Value = (byte)(Value & ~(1 >> index));
            }
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.Value.Equals(((MultiBool)obj).Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        public MultiBoolEnumerator GetEnumerator()
        {
            return new MultiBoolEnumerator(this);
        }

        public static implicit operator byte(MultiBool multiBool) => multiBool.Value;
        public static explicit operator MultiBool(byte value) => new MultiBool(value);
    }
}
