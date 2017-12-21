using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TurboJpegWrapper
{/// <summary>
 /// Static class used to load native libraries
 /// </summary>
    internal static class NativeModulesLoader
    {
        private static string _nativePath;

        private static readonly ConcurrentDictionary<string, IntPtr> LoadedLibraries = new ConcurrentDictionary<string, IntPtr>();

        /// <summary>
        /// Sets path to search native libraries
        /// </summary>
        /// <param name="path">Path to search native libraries</param>
        public static void SetNativePath(string path = null)
        {
            _nativePath = path;
        }

        public static string NativePath => _nativePath;

        /// <summary>
        /// Set native libraries Directory
        /// </summary>
        /// <exception cref="DllNotFoundException">Not found Sdk folder</exception>
        public static void LoadLibraries(string unmanagedModule, Action<string> logger = null)
        {
            if (!LoadUnmanagedModules(new[] { unmanagedModule }, logger))
            {
                throw new Exception($"Unable to load library {unmanagedModule}");
            }
        }

        /// <summary>
        /// Set native libraries Directory
        /// </summary>
        /// <exception cref="DllNotFoundException">Not found Sdk folder</exception>
        public static void LoadLibraries(string[] unmanagedModules, Action<string> logger = null)
        {
            if (!LoadUnmanagedModules(unmanagedModules, logger))
            {
                throw new Exception($"Unable to load libraries {unmanagedModules.Aggregate("", (current, value) => current + value + "; ")}");
            }
        }
        /// <summary>
        /// Releases specified unmanaged modules
        /// </summary>
        public static void FreeUnmanagedModules()
        {
            while (!LoadedLibraries.IsEmpty)
            {
                IntPtr ptr;
                var first = LoadedLibraries.First().Key;
                if (!LoadedLibraries.TryRemove(first, out ptr))
                    continue;

                if (Platform.OperationSystem != OS.Windows)
                {
                    Dlclose(ptr);
                }
                else
                {
                    FreeLibrary(ptr);
                }
            }
        }
        /// <summary>
        /// Releases specified unmanaged modules
        /// </summary>
        public static void FreeUnmanagedModules(params string[] unmanagedModules)
        {
            foreach (var name in unmanagedModules)
            {
                IntPtr ptr;
                if (!LoadedLibraries.TryGetValue(name, out ptr))
                    continue;

                if (Platform.OperationSystem != OS.Windows)
                {
                    Dlclose(ptr);
                }
                else
                {
                    FreeLibrary(ptr);
                }
            }
        }

        /// <summary>
        /// Attempts to load native sdk modules from the specific location
        /// </summary>
        /// <param name="unmanagedModules">The names of sdk modules. e.g. "fis_face_detector.dll" on windows.</param>
        /// <param name="logger">logger func</param>
        /// <returns>True if all the modules has been loaded successfully</returns>
        private static bool LoadUnmanagedModules(string[] unmanagedModules, Action<string> logger)
        {
            if (!string.IsNullOrEmpty(NativePath) && Directory.Exists(NativePath))
            {
                return LoadUnmanagedModules(NativePath, unmanagedModules, logger);
            }


#if NET47
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            if (string.IsNullOrEmpty(asm?.Location))
            {
                //it is possible to run from tests environment - do not know that to do 
                return false;
            }
            var location = Path.GetDirectoryName(asm.Location);
#elif NETSTANDARD2_0
            var location = AppContext.BaseDirectory;
#endif

            if (!Directory.Exists(location))
                return false;

            var osSubfolder = GetOSSubfolder(Platform.OperationSystem);
            var platform = Platform.GetPlatformName();

            var subFolderSearchPattern = $"{osSubfolder}-{platform}*";

            var dirs = Directory.GetDirectories(location, subFolderSearchPattern);

            if (dirs.Length == 0)
            {
                logger?.Invoke("No suitable directory found to load unmanaged modules");
                return false;
            }

            foreach (var dir in dirs)
            {
                logger?.Invoke($"Attempt to load unmanaged modules from {dir}");
                var result = LoadUnmanagedModules(dir, unmanagedModules, logger);
                if (result)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool LoadUnmanagedModules(string dir, string[] unmanagedModules, Action<string> logger)
        {
            if (!Directory.Exists(dir))
            {
                logger?.Invoke("No suitable directory found to load unmanaged modules");
                return false;
            }

            var oldDir = Directory.GetCurrentDirectory();

            dir = Path.GetFullPath(dir);
            Directory.SetCurrentDirectory(dir);

            logger?.Invoke($"Loading unmanaged libraries from {dir}");
            var success = true;

            foreach (var module in unmanagedModules)
            {
                //Use absolute path for Windows Desktop
                var fullPath = Path.Combine(dir, module);

                var fileExist = File.Exists(fullPath);
                if (!fileExist)
                    logger?.Invoke($"File {fullPath} do not exist.");

                var libraryPtr = LoadLibrary(fullPath, logger);

                var fileExistAndLoaded = fileExist && !IntPtr.Zero.Equals(libraryPtr);
                if (fileExist && !fileExistAndLoaded)
                    logger?.Invoke($"File {fullPath} cannot be loaded.");
                else
                {
                    logger?.Invoke($"Library {fullPath} loaded successfully");
                    LoadedLibraries.TryAdd(module, libraryPtr);
                }
                success &= fileExistAndLoaded;
            }
            Directory.SetCurrentDirectory(oldDir);
            return success;
        }


        private static string GetOSSubfolder(OS operationSystem)
        {
            switch (operationSystem)
            {
                case OS.Windows:
                    return "win";
                case OS.Linux:
                    return "linux";
                case OS.MacOS:
                    return "mac";
                case OS.Android:
                    return "android";
                case OS.IOS:
                    return "ios";
                case OS.WindowsPhone:
                    return "wp";
                default:
                    throw new ArgumentOutOfRangeException(nameof(operationSystem));
            }
        }

        /// <summary>
        /// Maps the specified executable module into the address space of the calling process.
        /// </summary>
        /// <param name="dllname">The name of the dll</param>
        /// <param name="logger"></param>
        /// <returns>The handle to the library</returns>
        private static IntPtr LoadLibrary(string dllname, Action<string> logger)
        {
            if (Platform.OperationSystem != OS.Windows)
                return Dlopen(dllname, 2); // 2 == RTLD_NOW


            const int loadLibrarySearchDllLoadDir = 0x00000100;
            const int loadLibrarySearchDefaultDirs = 0x00001000;
            var handler = LoadLibraryEx(dllname, IntPtr.Zero, loadLibrarySearchDllLoadDir | loadLibrarySearchDefaultDirs);
            if (handler != IntPtr.Zero)
                return handler;

            var error = Marshal.GetLastWin32Error();

            var ex = new System.ComponentModel.Win32Exception(error);
            logger?.Invoke($"LoadLibraryEx {dllname} failed with error code {(uint)error}: {ex.Message}");
            return handler;
        }

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx([MarshalAs(UnmanagedType.LPStr)]string fileName, IntPtr hFile, int dwFlags);

        /// <summary>
        /// Decrements the reference count of the loaded dynamic-link library (DLL). When the reference count reaches zero, the module is unmapped from the address space of the calling process and the handle is no longer valid
        /// </summary>
        /// <param name="handle">The handle to the library</param>
        /// <returns>If the function succeeds, the return value is true. If the function fails, the return value is false.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr handle);


        [DllImport("dl", EntryPoint = "dlopen")]
        private static extern IntPtr Dlopen([MarshalAs(UnmanagedType.LPStr)]string dllname, int mode);

        [DllImport("dl", EntryPoint = "dlclose")]
        private static extern int Dlclose(IntPtr handle);
    }
}
