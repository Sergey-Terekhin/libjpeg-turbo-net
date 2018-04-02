using System;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace TurboJpegWrapper
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Implements compression of RGB, CMYK, grayscale images to the jpeg format
    /// </summary>
    public class TJDecompressor : IDisposable
    {
        private readonly IntPtr _decompressorHandle;
        private bool _isDisposed;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Creates new instance of <see cref="TJDecompressor"/>
        /// </summary>
        /// <exception cref="TJException">
        /// Throws if internal compressor instance can not be created
        /// </exception>
        public TJDecompressor()
        {
            _decompressorHandle = TurboJpegImport.tjInitDecompress();
            if (_decompressorHandle == IntPtr.Zero)
            {
                TJUtils.GetErrorAndThrow();
            }
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="jpegBuf">Pointer to a buffer containing the JPEG image to decompress. This buffer is not modified</param>
        /// <param name="jpegBufSize">Size of the JPEG image (in bytes)</param>
        /// <param name="destPixelFormat">Pixel format of the destination image</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="stride">Bytes per line in the destination image</param>
        /// <returns>Raw pixel data of specified format</returns>
        /// <exception cref="TJException">Throws if underlying decompress function failed</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        public unsafe byte[] Decompress(IntPtr jpegBuf, ulong jpegBufSize, TJPixelFormats destPixelFormat, TJFlags flags, out int width, out int height, out int stride)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            var funcResult = TurboJpegImport.tjDecompressHeader(_decompressorHandle, jpegBuf, jpegBufSize,
                out width, out height, out _, out _);

            if (funcResult == -1)
            {
                TJUtils.GetErrorAndThrow();
            }

            var targetFormat = destPixelFormat;
            stride = TurboJpegImport.TJPAD(width * TurboJpegImport.PixelSizes[targetFormat]);
            var bufSize = stride * height;
            var buf = new byte[bufSize];
            fixed (byte* bufPtr = buf)
            {
                funcResult = TurboJpegImport.tjDecompress(
                    _decompressorHandle,
                    jpegBuf,
                    jpegBufSize,
                    (IntPtr)bufPtr,
                    width,
                    stride,
                    height,
                    (int)targetFormat,
                    (int)flags);

                if (funcResult == -1)
                {
                    TJUtils.GetErrorAndThrow();
                }

                return buf;
            }
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="jpegBuf">A buffer containing the JPEG image to decompress. This buffer is not modified</param>
        /// <param name="destPixelFormat">Pixel format of the destination image</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <param name="width">Width of image in pixels</param>
        /// <param name="height">Height of image in pixels</param>
        /// <param name="stride">Bytes per line in the destination image</param>
        /// <returns>Raw pixel data of specified format</returns>
        /// <exception cref="TJException">Throws if underlying decompress function failed</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        public unsafe byte[] Decompress(byte[] jpegBuf, TJPixelFormats destPixelFormat, TJFlags flags, out int width, out int height, out int stride)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            var jpegBufSize = (ulong)jpegBuf.Length;
            fixed (byte* jpegPtr = jpegBuf)
            {
                return Decompress((IntPtr)jpegPtr, jpegBufSize, destPixelFormat, flags, out width, out height, out stride);
            }
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="jpegBuf">Pointer to a buffer containing the JPEG image to decompress. This buffer is not modified</param>
        /// <param name="jpegBufSize">Size of the JPEG image (in bytes)</param>
        /// <param name="destPixelFormat">Pixel format of the destination image (see <see cref="TJPixelFormats"/> "Pixel formats".)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <returns>Decompressed image of specified format</returns>
        /// <exception cref="TJException">Throws if underlying decompress function failed</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">Convertion to the requested pixel format can not be performed</exception>
        public DecompressedImage Decompress(IntPtr jpegBuf, ulong jpegBufSize, TJPixelFormats destPixelFormat, TJFlags flags)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            var buffer = Decompress(jpegBuf, jpegBufSize, destPixelFormat, flags, out var width, out var height, out var stride);

            return new DecompressedImage(width, height, stride, buffer, destPixelFormat);
        }

        /// <summary>
        /// Decompress a JPEG image to an RGB, grayscale, or CMYK image.
        /// </summary>
        /// <param name="jpegBuf">A buffer containing the JPEG image to decompress. This buffer is not modified</param>
        /// <param name="destPixelFormat">Pixel format of the destination image (see <see cref="TJPixelFormats"/> "Pixel formats".)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <returns>Decompressed image of specified format</returns>
        /// <exception cref="TJException">Throws if underlying decompress function failed</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">Convertion to the requested pixel format can not be performed</exception>
        public unsafe DecompressedImage Decompress(byte[] jpegBuf, TJPixelFormats destPixelFormat, TJFlags flags)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            var jpegBufSize = (ulong)jpegBuf.Length;
            fixed (byte* jpegPtr = jpegBuf)
            {
                return Decompress((IntPtr)jpegPtr, jpegBufSize, destPixelFormat, flags);
            }
        }

        /// <summary>
        /// Releases resources
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {

            if (_isDisposed)
                return;

            lock (_lock)
            {
                if (_isDisposed)
                    return;

                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private void Dispose(bool callFromUserCode)
        {
            if (callFromUserCode)
            {
                _isDisposed = true;
            }
            TurboJpegImport.tjDestroy(_decompressorHandle);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~TJDecompressor()
        {
            Dispose(false);
        }

    }

}
