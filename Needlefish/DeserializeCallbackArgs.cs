using System;

namespace Needlefish
{
    public class DeserializeCallbackArgs
    {
        public Type Type { get; }

        public byte[] Data { get; }

        public int Index { get; set; }

        public object Result { get; set; }

        public bool Successful { get; set; }

        public DeserializeCallbackArgs(Type type, byte[] data, int index)
        {
            Type = type;
            Data = data;
            Index = index;
        }
    }
}