using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Styx.Common
{
    public static class Extensions
    {
        public static bool Like(this string str, string wildcard)
        {
            return new Regex("^" + Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(str);
        }

        public static unsafe double** ToPointer(this double[][] array)
        {
            var pointers = new double*[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                double* ptr = null;
                if (array[i] != null && array[i].Length != 0)
                {
                    fixed (double* p = &array[i][0])
                    {
                        ptr = p;
                    }
                }

                pointers[i] = ptr;
            }

            double** result = null;
            if (pointers.Length != 0)
            {
                fixed (double** p = &pointers[0])
                {
                    result = p;
                }
            }

            return result;
        }

        public static int IndexOf<T>(this T[] bytes, T value)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i].Equals(value))
                {
                    return i;
                }
            }

            return -1;
        }

        public static unsafe byte[] ToArray<T>(byte* ptr, int length)
        {
            byte[] array = new byte[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = ptr[i];
            }

            return array;
        }

        public static void ForEach<T>(this IEnumerable<T> e, ForEachDelegate<T> @do)
        {
            foreach (T t in e)
            {
                @do(t);
            }
        }

        public delegate void ForEachDelegate<T>(T item);
    }
}
