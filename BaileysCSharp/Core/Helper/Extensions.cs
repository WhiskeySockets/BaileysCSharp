using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaileysCSharp.Core.Helper
{
    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] array, int startIndex, int length)
        {
            int num;
            if (array == null || (num = array.Length) == 0)
            {
                return new T[0];
            }

            if (startIndex < 0 || length <= 0 || startIndex + length > num)
            {
                return new T[0];
            }

            if (startIndex == 0 && length == num)
            {
                return array;
            }

            T[] array2 = new T[length];
            Array.Copy(array, startIndex, array2, 0, length);
            return array2;
        }


        public static int AsEpoch(this DateTime date)
        {
            TimeSpan t = date - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            return secondsSinceEpoch;
        }


        public static void Set<T>(this T[] array, T[] copy, int index = 0)
        {
            Array.Copy(copy, 0, array, index, copy.Length);
        }


        public static T[] Slice<T>(this T[] bytes, int start, int end)
        {
            if (start < 0)
            {
                start = bytes.Length + start;
                return bytes.Skip(start).Take(end).ToArray();
            }
            if (end < 0)
            {
                end = bytes.Length + end;
            }
            return bytes.Skip(start).Take(end - start).ToArray();
        }

        public static TResult[] Map<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            return source.SelectMany(selector).ToArray();
        }

        public static int Compare<T>(this T[] source, T[] target)
        {
            var total = 0;
            if (source.Length != target.Length)
            {
                return -1;
            }
            for (int i = 0; i < source.Length; i++)
            {
                if (!source[i].Equals(target[i]))
                {
                    total++;
                }
            }
            return total;
        }

        public static T[] Slice<T>(this T[] bytes, int start)
        {
            if (start > 0)
            {
                return bytes.Skip(start).ToArray();
            }

            return bytes.Slice(start, bytes.Length - start);
        }

        public static long UnixTimestampSeconds(this DateTime now)
        {
            if (now.Kind == DateTimeKind.Local)
            {
                now = now.ToUniversalTime();
            }
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long unixTimestamp = (long)(now - unixEpoch).TotalSeconds;
            return unixTimestamp;
        }


        public static ushort GetUint16(this byte[] bytes, int offset)
        {
            return BitConverter.ToUInt16(bytes, offset);
        }

        public static void SetUint16(this byte[] bytes, int offset, ushort value)
        {
            var bit = BitConverter.GetBytes(value);
            bit.CopyTo(bytes, offset);
        }

    }
}
