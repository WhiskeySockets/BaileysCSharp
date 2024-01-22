using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
