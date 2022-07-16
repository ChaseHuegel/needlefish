using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Needlefish.Reflection;
using Needlefish.Types;

namespace Needlefish
{
    public static class NeedlefishFormatter
    {
        public static EventHandler<SerializeFallbackArgs> SerializeFallback;
        public static EventHandler<DeserializeCallbackArgs> DeserializeFallback;

        public static byte[] Serialize<T>(T source) where T : IFormattable => WriteObject(source);

        public static void Populate<T>(T target, byte[] data) where T : IFormattable, new() => PopulateObject(target, data);

        public static T Deserialize<T>(byte[] data) where T : IFormattable, new() => PopulateNew<T>(data);

        internal static byte[] WriteObject(object obj)
        {
            if (obj == null)
                throw new SerializationException($"Tried to write a null object.");

            List<byte> buffer = new List<byte>();

            foreach (FieldInfo field in Reflector.GetFields(obj.GetType()))
            {
                try
                {
                    buffer.AddRange(Write(field.GetValue(obj)));
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Error writing field {field} in {field.DeclaringType}", ex);
                }
            }
            
            foreach (PropertyInfo property in Reflector.GetProperties(obj.GetType()))
            {
                try
                {
                    buffer.AddRange(Write(property.GetValue(obj)));
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Error writing property {property} in {property.DeclaringType}", ex);
                }
            }

            return buffer.ToArray();
        }

        internal static byte[] Write(object value)
        {
            if (value == null)
                throw new SerializationException($"Tried to write a null value.");
            
            byte[] bytes;
            
            if (value.GetType() == typeof(bool))
                return BitConverter.GetBytes((bool)value);

            if (value.GetType() == typeof(byte))
                return new byte[] { (byte)value };

            if (value.GetType() == typeof(sbyte))
                return new byte[] { (byte)value };

            if (value.GetType() == typeof(char))
                return BitConverter.GetBytes((char)value);

            if (value.GetType() == typeof(decimal))
                return Write(Decimal.GetBits((decimal)value));

            if (value.GetType() == typeof(double))
                return BitConverter.GetBytes((double)value);

            if (value.GetType() == typeof(float))
                return BitConverter.GetBytes((float)value);

            if (value.GetType() == typeof(int))
                return BitConverter.GetBytes((int)value);

            if (value.GetType() == typeof(uint))
                return BitConverter.GetBytes((uint)value);

            if (value.GetType() == typeof(long))
                return BitConverter.GetBytes((long)value);

            if (value.GetType() == typeof(ulong))
                return BitConverter.GetBytes((ulong)value);

            if (value.GetType() == typeof(short))
                return BitConverter.GetBytes((short)value);

            if (value.GetType() == typeof(ushort))
                return BitConverter.GetBytes((ushort)value);

            if (value.GetType() == typeof(MultiBool))
                return new byte[] { (MultiBool)value };

            if (value.GetType() == typeof(string))
            {
                byte[] stringBytes = Encoding.Unicode.GetBytes((string)value);
                bytes = new byte[stringBytes.Length + 4];
                Write(stringBytes.Length).CopyTo(bytes, 0);
                stringBytes.CopyTo(bytes, 4);
                return bytes;
            }

            if (value.GetType().IsArray)
            {
                Array array = (Array) value;
                List<byte> buffer = new List<byte>();

                int count = 0;
                foreach (object obj in array)
                {
                    buffer.AddRange(Write(obj));
                    count++;
                }

                buffer.InsertRange(0, BitConverter.GetBytes(count));

                return buffer.ToArray();
            }

            if (value.GetType().IsEnum)
            {
                Type enumType = value.GetType().GetEnumUnderlyingType();
                
                if (enumType == typeof(int))
                    return Write((int)value);

                if (enumType == typeof(byte))
                    return Write((byte)value);

                if (enumType == typeof(long))
                    return Write((long)value);

                if (enumType == typeof(short))
                    return Write((short)value);

                if (enumType == typeof(ushort))
                    return Write((ushort)value);

                if (enumType == typeof(uint))
                    return Write((uint)value);

                if (enumType == typeof(ulong))
                    return Write((ulong)value);

                if (enumType == typeof(sbyte))
                    return Write((sbyte)value);
            }

            if (typeof(IList).IsAssignableFrom(value.GetType()))
            {
                IList list = (IList) value;
                List<byte> buffer = new List<byte>();

                int count = 0;
                foreach (object obj in list)
                {
                    buffer.AddRange(Write(obj));
                    count++;
                }

                buffer.InsertRange(0, BitConverter.GetBytes(count));

                return buffer.ToArray();
            }

            if (value.GetType().IsClass)
                return WriteObject(value);

            SerializeFallbackArgs args = new SerializeFallbackArgs(value);
            SerializeFallback?.Invoke(value, args);
            if (args.Bytes != null)
                return args.Bytes;

            throw new SerializationException($"Unsupported type {value.GetType()}");
        }

        internal static T PopulateNew<T>(byte[] data) where T : new()
        {
            T obj = new T();
            PopulateObject(obj, data);
            return obj;
        }

        internal static void PopulateObject(object obj, byte[] data)
        {
            int index = 0;
            StepPopulateObject(obj, data, ref index);
        }

        internal static object StepPopulateNew(Type type, byte[] data, ref int index)
        {
            object obj = Activator.CreateInstance(type);
            StepPopulateObject(obj, data, ref index);
            return obj;
        }

        internal static void StepPopulateObject(object obj, byte[] data, ref int index)
        {
            foreach (FieldInfo field in Reflector.GetFields(obj.GetType()))
            {
                try
                {
                    field.SetValue(obj, Read(field.FieldType, data, ref index));
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Error populating field {field} in {field.DeclaringType}", ex);
                }
            }
            
            foreach (PropertyInfo property in Reflector.GetProperties(obj.GetType()))
            {
                try
                {
                    property.SetValue(obj, Read(property.PropertyType, data, ref index));
                }
                catch (Exception ex)
                {
                    throw new SerializationException($"Error populating property {property} in {property.DeclaringType}", ex);
                }
            }
        }

        internal static T Read<T>(byte[] data, ref int index) => (T)Read(typeof(T), data, ref index);

        internal static object Read(Type type, byte[] data, ref int index)
        {
            if (type == typeof(bool))
                return BitConverter.ToBoolean(data, index++);
            
            if (type == typeof(byte))
                return data[index++];
            
            if (type == typeof(sbyte))
                return (sbyte)data[index++];

            if (type == typeof(char))
            {
                index += 2;
                return BitConverter.ToChar(data, index-2);
            }

            if (type == typeof(decimal))
            {
                return new Decimal((int[]) Read(typeof(int[]), data, ref index));
            }

            if (type == typeof(double))
            {
                index += 8;
                return BitConverter.ToDouble(data, index-8);
            }

            if (type == typeof(float))
            {
                index += 4;
                return BitConverter.ToSingle(data, index-4);
            }

            if (type == typeof(int))
            {
                index += 4;
                return BitConverter.ToInt32(data, index-4);
            }

            if (type == typeof(uint))
            {
                index += 4;
                return BitConverter.ToUInt32(data, index-4);
            }

            if (type == typeof(long))
            {
                index += 8;
                return BitConverter.ToInt64(data, index-8);
            }

            if (type == typeof(ulong))
            {
                index += 8;
                return BitConverter.ToUInt64(data, index-8);
            }

            if (type == typeof(short))
            {
                index += 2;
                return BitConverter.ToInt16(data, index-2);
            }

            if (type == typeof(ushort))
            {
                index += 2;
                return BitConverter.ToUInt16(data, index-2);
            }

            if (type == typeof(string))
            {
                int length = BitConverter.ToInt32(data, index);
                index += 4;
                if (length <= 0)
                    return string.Empty;
                    
                string result = Encoding.Unicode.GetString(data, index, length);
                index += length;
                return result;
            }

            if (type == typeof(MultiBool))
                return new MultiBool(data[index++]);

            if (type.IsArray)
            {
                int length = BitConverter.ToInt32(data, index);
                index += 4;

                Array array = Array.CreateInstance(type.GetElementType(), length);
                for (int i = 0; i < length; i++)
                    array.SetValue(Read(type.GetElementType(), data, ref index), i);

                return array;
            }

            if (type.IsEnum)
            {
                Type enumType = type.GetEnumUnderlyingType();
                return Enum.ToObject(type, Read(enumType, data, ref index));
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                int length = BitConverter.ToInt32(data, index);
                index += 4;

                IList values = (IList) Activator.CreateInstance(type);
                for (int i = 0; i < length; i++)
                    values.Add(Read(values.GetType().GenericTypeArguments[0], data, ref index));

                return values;
            }

            if (type.IsClass)
                return StepPopulateNew(type, data, ref index);

            DeserializeCallbackArgs args = new DeserializeCallbackArgs(type, data, index);
            DeserializeFallback?.Invoke(type, args);
            if (args.Successful)
                return args.Result;

            throw new SerializationException($"Unsupported type {type}");
        }
    }
}
