using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly:InternalsVisibleTo("TurboJpegWrapper.Tests")]

namespace TurboJpegWrapper
{
    /// <summary>
    /// Provide information for the platform which is using. 
    /// </summary>
    internal static class Platform
    {
        //only for linux systems
        [DllImport("c")]
        private static extern int uname(IntPtr buffer);

        static Platform()
        {
#if NET45
            var pid = Environment.OSVersion.Platform;
            if (pid == PlatformID.MacOSX)
            {
                //This never works, it is a bug in Mono
                OperationSystem = OS.MacOS;
            }
            else
            {
                int p = (int)pid;
                OperationSystem = (p == 4) || (p == 128) ? OS.Linux : OS.Windows;

                if (OperationSystem == OS.Linux)
                {  //Check if the OS is Mac OSX
                    IntPtr buf = IntPtr.Zero;
                    try
                    {
                        buf = Marshal.AllocHGlobal(8192);
                        // This is a hacktastic way of getting sysname from uname () 
                        if (uname(buf) == 0)
                        {
                            string os = Marshal.PtrToStringAnsi(buf);
                            if (os == "Darwin")
                                OperationSystem = OS.MacOS;
                        }
                    }
                    catch
                    {
                        //Some unix system may not be able to call "libc"
                        //such as Ubuntu 13.04, we provide a safe catch here
                    }
                    finally
                    {
                        if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
                    }
                }
            }
#elif NETSTANDARD1_3
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OperationSystem = OS.MacOS;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                OperationSystem = OS.Linux;
            }
            else
            {
                OperationSystem = OS.Windows;
            }
#endif

        }

        /// <summary>
        /// Get the type of the current operating system
        /// </summary>
        public static OS OperationSystem { get; }
        

        /// <summary>
        /// Returns name of executing platform
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static string GetPlatformName()
        {
            switch (IntPtr.Size)
            {
                case 4:
                    return "x86";
                case 8:
                    return "x64";
                default:
                    return "Unknown";
            }
        }
    }

    /// <summary>Type of operating system</summary>
    internal enum OS
    {
        Windows,
        Linux,
        MacOS,
        IOS,
        Android,
        WindowsPhone,
    }
}