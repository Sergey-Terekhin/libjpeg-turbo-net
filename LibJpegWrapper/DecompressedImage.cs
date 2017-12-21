using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurboJpegWrapper
{
    /// <summary>
    /// represensts decompressed image
    /// </summary>
    public class DecompressedImage
    {
        public DecompressedImage(int width, int height, int rowBytes, byte[] data, TJPixelFormats pixelFormat)
        {
            Width = width;
            Height = height;
            RowBytes = rowBytes;
            Data = data;
            PixelFormat = pixelFormat;
        }
        public TJPixelFormats PixelFormat { get; }
        public int Width { get;  }
        public int Height { get; }
        public int RowBytes { get; }
        public byte[] Data { get; }
    }
}
