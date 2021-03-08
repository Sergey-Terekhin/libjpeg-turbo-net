﻿using System;
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
        static Platform()
        {
#if NET47 || NET40
            OperationSystem = OS.Windows;
#endif
#if NETSTANDARD2_0
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