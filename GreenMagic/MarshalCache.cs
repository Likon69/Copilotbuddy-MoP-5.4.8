using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GreenMagic
{
    public static class MarshalCache<T> where T : struct
    {
        static MarshalCache()
        {
            Type type = typeof(T);
            Size = GetSizeOf(type);
            Type = type;
            if (typeof(T) == typeof(bool))
            {
                Size = 1;
            }
            else if (typeof(T).IsEnum)
            {
                Type = typeof(T).GetEnumUnderlyingType();
                Size = Marshal.SizeOf(Type);
            }
            RequiresMarshal = GetRequiresMarshal(Type);
        }

        private static int GetSizeOf(Type t)
        {
            try
            {
                return Marshal.SizeOf(Activator.CreateInstance(t));
            }
            catch (Exception)
            {
                int size = 0;
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    object[] fixedBufferAttributes = field.GetCustomAttributes(typeof(FixedBufferAttribute), false);
                    if (fixedBufferAttributes.Length > 0)
                    {
                        FixedBufferAttribute fixedBufferAttribute = (FixedBufferAttribute)fixedBufferAttributes[0];
                        size += GetSizeOf(fixedBufferAttribute.ElementType) * fixedBufferAttribute.Length;
                    }
                    else
                    {
                        size += GetSizeOf(field.FieldType);
                    }
                }
                return size;
            }
        }

        private static bool GetRequiresMarshal(Type t)
        {
            FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.GetCustomAttributes(typeof(MarshalAsAttribute), true).Length != 0)
                {
                    return true;
                }
                if (t != typeof(IntPtr) && System.Type.GetTypeCode(t) == TypeCode.Object && GetRequiresMarshal(field.FieldType))
                {
                    return true;
                }
            }
            return false;
        }

        public static int Size;

        public static Type Type;

        public static TypeCode TypeCode;

        public static bool RequiresMarshal;
    }
}
