using System;

namespace Needlefish
{
    public class SerializeFallbackArgs
    {
        public Type Type { get; }

        public object Value { get; }

        public byte[] Bytes { get; set; }

        public SerializeFallbackArgs(object value)
        {
            Type = value.GetType();
            Value = value;
        }
    }
}