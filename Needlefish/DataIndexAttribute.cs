using System;

namespace Needlefish
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DataFieldAttribute : Attribute
    {
        public int Index;

        public DataFieldAttribute()
        {
            Index = int.MaxValue;
        }

        public DataFieldAttribute(int index)
        {
            Index = index;
        }
    }
}
