using System;
using System.Collections;

using Needlefish.Types;

namespace Needlefish.Enumerators
{
    public class MultiBoolEnumerator : IEnumerator
    {
        public MultiBool Value;

        private int index = -1;

        public MultiBoolEnumerator(MultiBool value)
        {
            Value = value;
        }

        object IEnumerator.Current => Current;

        public bool Current
        {
            get
            {
                try
                {
                    return Value[index];
                }
                catch
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public bool MoveNext()
        {
            index++;
            return index < MultiBool.Count;
        }

        public void Reset()
        {
            index = -1;
        }
    }
}