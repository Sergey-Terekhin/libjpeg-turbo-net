using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace TurboJpegWrapper.Tests
{
    static class TestUtils
    {
        public static IEnumerable<Bitmap> GetTestImages(string searchPattern)
        {
            var path = Assembly.GetExecutingAssembly().Location;
            var imagesDir = Path.Combine(Path.GetDirectoryName(path), "images");

            foreach (var file in Directory.EnumerateFiles(imagesDir, searchPattern))
            {
                Bitmap bmp;
                try
                {
                    bmp = (Bitmap)Image.FromFile(file);
                    Debug.WriteLine($"Input file is {file}");
                }
                catch (OutOfMemoryException)
                {
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }
                yield return bmp;
            }
        }

        public static IEnumerable<Tuple<string, byte[]>> GetTestImagesData(string searchPattern)
        {
            var imagesDir = Path.Combine(BinPath, "images");

            foreach (var file in Directory.EnumerateFiles(imagesDir, searchPattern))
            {
                Debug.WriteLine($"Input file is {file}");
                yield return new Tuple<string, byte[]>(file, File.ReadAllBytes(file));
            }
        }

        public static string BinPath
        {
            get
            {
                var path = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(path);
            }
        }
        public static TJPixelFormats ConvertPixelFormat(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    return TJPixelFormats.TJPF_BGRA;
                case PixelFormat.Format24bppRgb:
                    return TJPixelFormats.TJPF_BGR;
                case PixelFormat.Format8bppIndexed:
                    return TJPixelFormats.TJPF_GRAY;
                default:
                    throw new NotSupportedException(string.Format("Provided pixel format \"{0}\" is not supported", pixelFormat));
            }
        }
    }
}
