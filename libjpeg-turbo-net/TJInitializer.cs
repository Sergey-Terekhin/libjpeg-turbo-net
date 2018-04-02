using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TurboJpegWrapper
{
    public static class TJInitializer
    {
        private static bool _isInitialized;
        private static readonly object _lock = new object();

        public static void Initialize(string dllPath = null, Action<string> logger = null)
        {
            if (_isInitialized)
            {
                logger?.Invoke("Library already loaded");
            }
            lock (_lock)
            {
                if (!Directory.Exists(dllPath))
                {
                    var rootPath =
#if NET47
                Path.GetDirectoryName(typeof(TJUtils).Assembly.Location);
#elif NETSTANDARD2_0
                        AppContext.BaseDirectory;
#endif
                    var platform = Platform.GetPlatformName();
                    dllPath = Path.Combine(rootPath, platform);
                }

                logger?.Invoke($"Set libjpeg-turbo path to {dllPath}");

                var current = NativeModulesLoader.NativePath;
                NativeModulesLoader.SetNativePath(dllPath);

                string libraryName;
                switch (Platform.OperationSystem)
                {
                    case OS.Windows:
                    case OS.WindowsPhone:
                        libraryName = TurboJpegImport.LibraryName + ".dll";
                        break;
                    case OS.Linux:
                    case OS.Android:
                        libraryName = "lib"+TurboJpegImport.LibraryName + ".so";
                        break;
                    case OS.MacOS:
                    case OS.IOS:
                        libraryName = "lib"+TurboJpegImport.LibraryName + ".dylib";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                NativeModulesLoader.LoadLibraries(libraryName, logger);
                NativeModulesLoader.SetNativePath(current);
                _isInitialized = true;
            }
        }
    }
}
