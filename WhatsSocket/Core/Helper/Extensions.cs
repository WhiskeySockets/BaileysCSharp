using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Encodings;

namespace WhatsSocket.Core.Helper
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


        public static void Set(this byte[] array, byte[] copy, int index = 0)
        {
            Array.Copy(copy, 0, array, index, copy.Length);
        }


        public static byte[] Slice(this byte[] bytes, int start, int end)
        {
            if (start < 0)
            {
                start = bytes.Length + start;
            }
            return bytes.Skip(start).Take(end).ToArray();
        }

        public static byte[] Slice(this byte[] bytes, int start)
        {
            return bytes.Slice(start, bytes.Length - start);
        }

    }
}
