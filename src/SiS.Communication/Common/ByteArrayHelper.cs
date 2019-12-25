using System;

namespace SiS.Communication
{
    internal static class ByteArrayHelper
    {
        public static byte[] ByteArrayJoin(byte[] array1, byte[] array2)
        {
            byte[] resData = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, resData, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, resData, array1.Length, array2.Length);
            return resData;
        }

        public static byte[] ByteArrayJoin(ArraySegment<byte> array1, byte[] array2)
        {
            byte[] resData = new byte[array1.Count + array2.Length];
            Buffer.BlockCopy(array1.Array, array1.Offset, resData, 0, array1.Count);
            Buffer.BlockCopy(array2, 0, resData, array1.Count, array2.Length);
            return resData;
        }

        public static int SearchByteArray(byte[] desArray, byte[] searchArray)
        {
            return SearchArray(desArray, desArray.Length, searchArray);
        }

        public static int SearchByteArray(byte[] desArray, int offset, int count, byte[] searchArray)
        {
            count = Math.Min(count, desArray.Length - offset);
            if (searchArray.Length > count)
            {
                return -1;
            }
            for (int i = offset; i <= count + offset - searchArray.Length; i++)
            {
                bool bFound = true;
                for (int j = 0; j < searchArray.Length; j++)
                {
                    if (searchArray[j] != desArray[i + j])
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int SearchArray(byte[] desArray, int desLen, byte[] searchArray)
        {
            return SearchByteArray(desArray, 0, desLen, searchArray);
        }

        public static int SearchByteArrayBack(byte[] desArray, byte[] searchArray)
        {
            return SearchByteArrayBack(desArray, desArray.Length, searchArray);
        }

        public static int SearchByteArrayBack(byte[] desArray, int desLen, byte[] searchArray)
        {
            desLen = Math.Min(desLen, desArray.Length);
            if (searchArray.Length > desLen)
            {
                return -1;
            }
            for (int i = desLen - searchArray.Length; i >= 0; i--)
            {
                bool bFound = true;
                for (int j = 0; j < searchArray.Length; j++)
                {
                    if (searchArray[j] != desArray[i + j])
                    {
                        bFound = false;
                        break;
                    }
                }
                if (bFound)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
