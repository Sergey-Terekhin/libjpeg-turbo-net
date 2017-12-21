using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper
{
    // ReSharper disable once InconsistentNaming
    static class TJUtils
    {
        ///<summary>
        /// Retrieves last error from underlying turbo-jpeg library and throws exception</summary>
        /// <exception cref="TJException"> Throws if low level turbo jpeg function fails </exception>
        public static void GetErrorAndThrow()
        {
            var error = TurboJpegImport.tjGetErrorStr();
            throw new TJException(error);
        }

        

        /// <summary>
        /// Converts array of managed structures to the unmanaged pointer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structArray"></param>
        /// <returns></returns>
        public static IntPtr StructArrayToIntPtr<T>(T[] structArray)
        {
            var structSize =
#if NET47
                Marshal.SizeOf(typeof(T));
#elif NETSTANDARD2_0
                Marshal.SizeOf<T>();
#endif
            var result = Marshal.AllocHGlobal(structArray.Length * structSize);
            var longPtr = result.ToInt64(); // Must work both on x86 and x64
            foreach (var s in structArray)
            {
                var structPtr = new IntPtr(longPtr);
                Marshal.StructureToPtr(s, structPtr, false); // You do not need to erase struct in this case
                longPtr += structSize;
            }

            return result;
        }

        /// <summary>
        /// Copies data from array to unmanaged pointer
        /// </summary>
        /// <param name="data">Byte array for copy</param>
        /// <param name="useComAllocation">If set to <c>true</c>, Com allocator will be used to allocate memory</param>
        /// <returns></returns>
        public static IntPtr CopyDataToPointer(byte[] data, bool useComAllocation = false)
        {
            var res = useComAllocation ? Marshal.AllocCoTaskMem(data.Length) : Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, res, data.Length);
            return res;
        }


        /// <summary>
        /// Frees unmanaged pointer using allocator
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="isComAllocated">If set to <c>true</c>, Com allocator will be used to free memory</param>
        public static void FreePtr(IntPtr ptr, bool isComAllocated = false)
        {
            if (ptr == IntPtr.Zero)
                return;
            if (isComAllocated)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
            else
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
    }
}
