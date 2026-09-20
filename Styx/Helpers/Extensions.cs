using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Styx.Helpers
{
    public static class Extensions
    {
        public static T Parse<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static uint ToUInt32(this IntPtr val)
        {
            return (uint)val.ToInt32();
        }

        public static ulong ToUInt64(this IntPtr val)
        {
            return (ulong)val.ToInt64();
        }

        public static int ToInt32(this string value)
        {
            int result;
            int.TryParse(value, out result);
            return result;
        }

        public static uint ToUInt32(this string value)
        {
            uint result;
            uint.TryParse(value, out result);
            return result;
        }

        public static bool ToBoolean(this string value)
        {
            bool result;
            bool.TryParse(value, out result);
            return result;
        }

        public static float ToFloat(this string value)
        {
            float result;
            float.TryParse(value, out result);
            return result;
        }

        public static string ToStringInvariant(this float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToStringInvariant(this double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToUTF8String(this byte[] arr)
        {
            return arr.ToUTF8String(0);
        }

        public static string ToUTF8String(this byte[] arr, int startIndex)
        {
            return arr.ToUTF8String(startIndex, arr.Length - startIndex);
        }

        public static string ToUTF8String(this byte[] arr, int startIndex, int maxCount)
        {
            int count = maxCount;
            int index = startIndex;
            for (int i = 0; i < maxCount; i++)
            {
                if (arr[index] == 0)
                {
                    count = i;
                    break;
                }

                index++;
            }

            return Encoding.UTF8.GetString(arr, startIndex, count);
        }

        public static int IndexOf<T>(this T[] arr, T val)
        {
            return arr.IndexOf(val, 0);
        }

        public static int IndexOf<T>(this T[] arr, T val, int startIndex)
        {
            return arr.IndexOf(val, startIndex, arr.Length - startIndex);
        }

        public static int IndexOf<T>(this T[] arr, T val, int startIndex, int count)
        {
            return arr.IndexOf(val, startIndex, count, -1);
        }

        public static int IndexOf<T>(this T[] arr, T val, int startIndex, int count, int failReturn)
        {
            int index = startIndex;
            for (int i = 0; i < count; i++)
            {
                if (object.Equals(arr[index], val))
                {
                    return index;
                }

                index++;
            }

            return failReturn;
        }

        public static bool ContainsAny(this IEnumerable<string> enumer, IEnumerable<string> list)
        {
            foreach (string value in enumer)
            {
                if (value.ContainsAny(list))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsAny(this string str, IEnumerable<string> list)
        {
            foreach (string value in list)
            {
                if (str.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        public static string ToRealString(this IList lst)
        {
            List<string> list = new List<string>();
            if (lst == null)
            {
                return "NULL";
            }

            if (lst.Count == 0)
            {
                return "NULL";
            }

            foreach (object obj in lst)
            {
                try
                {
                    if (obj.Equals(null))
                    {
                        list.Add("NULL");
                        continue;
                    }
                }
                catch
                {
                    list.Add("NULL");
                    continue;
                }

                list.Add(obj.ToString());
            }

            return string.Join(",", list.ToArray());
        }

        public static string ToRealString<T>(this T[] lst)
        {
            List<T> list = new List<T>();
            if (lst == null)
            {
                return "NULL";
            }

            list.AddRange(lst);
            return list.ToRealString();
        }
    }
}
