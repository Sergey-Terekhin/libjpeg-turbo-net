using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
// ReSharper disable InconsistentNaming

namespace TurboJpegWrapper
{

    /// <summary>
    /// Implements compression of RGB, CMYK, grayscale images to the jpeg format
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    public class TJCompressor : IDisposable
    {
        private readonly IntPtr _compressorHandle;
        private bool _isDisposed;
        private readonly object _lock = new object();

        /// <summary>
        /// Creates new instance of <see cref="TJCompressor"/>
        /// </summary>
        /// <exception cref="TJException">
        /// Throws if internal compressor instance can not be created
        /// </exception>
        public TJCompressor()
        {
            _compressorHandle = TurboJpegImport.tjInitCompress();
            if (_compressorHandle == IntPtr.Zero)
            {
                TJUtils.GetErrorAndThrow();
            }
        }

        /// <summary>
        /// Compresses input image to the jpeg format with specified quality
        /// </summary>
        /// <param name="srcPtr">
        /// Pointer to an image buffer containing RGB, grayscale, or CMYK pixels to be compressed.  
        /// This buffer is not modified.
        /// </param>
        /// <param name="stride">
        /// Bytes per line in the source image.  
        /// Normally, this should be <c>width * BytesPerPixel</c> if the image is unpadded, 
        /// or <c>TJPAD(width * BytesPerPixel</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.  
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * BytesPerPixel</c>.
        /// </param>
        /// <param name="width">Width (in pixels) of the source image</param>
        /// <param name="height">Height (in pixels) of the source image</param>
        /// <param name="pixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// <param name="subSamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="quality">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <exception cref="TJException"> Throws if compress function failed </exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        public byte[] Compress(IntPtr srcPtr, int stride, int width, int height, TJPixelFormats pixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            CheckOptionsCompatibilityAndThrow(subSamp, pixelFormat);

            var buf = IntPtr.Zero;
            ulong bufSize = 0;
            try
            {
                var result = TurboJpegImport.tjCompress2(
                    _compressorHandle,
                    srcPtr,
                    width,
                    stride,
                    height,
                    (int)pixelFormat,
                    ref buf,
                    ref bufSize,
                    (int)subSamp,
                    quality,
                    (int)flags);

                if (result == -1)
                {
                    TJUtils.GetErrorAndThrow();
                }

                var jpegBuf = new byte[bufSize];
                // ReSharper disable once ExceptionNotDocumentedOptional
                Marshal.Copy(buf, jpegBuf, 0, (int)bufSize);
                return jpegBuf;
            }
            finally
            {
                TurboJpegImport.tjFree(buf);
            }
        }

        /// <summary>
        /// Compresses input image to the jpeg format with specified quality
        /// </summary>
        /// <param name="srcBuf">
        /// Image buffer containing RGB, grayscale, or CMYK pixels to be compressed.  
        /// This buffer is not modified.
        /// </param>
        /// <param name="stride">
        /// Bytes per line in the source image.  
        /// Normally, this should be <c>width * BytesPerPixel</c> if the image is unpadded, 
        /// or <c>TJPAD(width * BytesPerPixel</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.  
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * BytesPerPixel</c>.
        /// </param>
        /// <param name="width">Width (in pixels) of the source image</param>
        /// <param name="height">Height (in pixels) of the source image</param>
        /// <param name="pixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// <param name="subSamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="quality">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <exception cref="TJException">
        /// Throws if compress function failed
        /// </exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException"> 
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        public unsafe byte[] Compress(byte[] srcBuf, int stride, int width, int height, TJPixelFormats pixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");

            CheckOptionsCompatibilityAndThrow(subSamp, pixelFormat);

            var buf = IntPtr.Zero;
            ulong bufSize = 0;
            try
            {
                fixed (byte* srcBufPtr = srcBuf)
                {
                    var result = TurboJpegImport.tjCompress2(
                        _compressorHandle,
                        (IntPtr)srcBufPtr,
                        width,
                        stride,
                        height,
                        (int)pixelFormat,
                        ref buf,
                        ref bufSize,
                        (int)subSamp,
                        quality,
                        (int)flags);
                    if (result == -1)
                    {
                        TJUtils.GetErrorAndThrow();
                    }
                }

                var jpegBuf = new byte[bufSize];
                // ReSharper disable once ExceptionNotDocumentedOptional
                Marshal.Copy(buf, jpegBuf, 0, (int)bufSize);
                return jpegBuf;
            }
            finally
            {
                TurboJpegImport.tjFree(buf);
            }
        }

        /// <summary>
        /// Compresses input image to the jpeg format with specified quality
        /// </summary>
        /// <param name="srcPtr">
        /// Pointer to an image buffer containing RGB, grayscale, or CMYK pixels to be compressed.
        /// This buffer is not modified.
        /// </param>
        /// <param name="stride">
        /// Bytes per line in the source image.
        /// Normally, this should be <c>width * BytesPerPixel</c> if the image is unpadded,
        /// or <c>TJPAD(width * BytesPerPixel</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * BytesPerPixel</c>.
        /// </param>
        /// <param name="width">Width (in pixels) of the source image</param>
        /// <param name="height">Height (in pixels) of the source image</param>
        /// <param name="tjPixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// <param name="subSamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="quality">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <param name="onCompressionCompleted">Method to process compressed data </param>
        /// <exception cref="TJException">Throws if compress function failed.</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        /// <returns>
        /// </returns>
        public void Compress(IntPtr srcPtr, int stride, int width, int height,
            TJPixelFormats tjPixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags,
            Action<IntPtr, int> onCompressionCompleted)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");
            if (onCompressionCompleted == null)
                throw new ArgumentNullException(nameof(onCompressionCompleted));

            CheckOptionsCompatibilityAndThrow(subSamp, tjPixelFormat);

            var buf = IntPtr.Zero;
            ulong bufSize = 0;
            try
            {
                var result = TurboJpegImport.tjCompress2(
                    _compressorHandle,
                    srcPtr,
                    width,
                    stride,
                    height,
                    (int)tjPixelFormat,
                    ref buf,
                    ref bufSize,
                    (int)subSamp,
                    quality,
                    (int)flags);

                if (result == -1)
                {
                    TJUtils.GetErrorAndThrow();
                }

                onCompressionCompleted(buf, (int)bufSize);
            }
            finally
            {
                TurboJpegImport.tjFree(buf);
            }
        }

        /// <summary>
        /// Compresses input image to the jpeg format with specified quality
        /// </summary>
        /// <param name="srcPtr">
        /// Pointer to an image buffer containing RGB, grayscale, or CMYK pixels to be compressed.
        /// This buffer is not modified.
        /// </param>
        /// <param name="stride">
        /// Bytes per line in the source image.
        /// Normally, this should be <c>width * BytesPerPixel</c> if the image is unpadded,
        /// or <c>TJPAD(width * BytesPerPixel</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * BytesPerPixel</c>.
        /// </param>
        /// <param name="width">Width (in pixels) of the source image</param>
        /// <param name="height">Height (in pixels) of the source image</param>
        /// <param name="tjPixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// <param name="subSamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="quality">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <param name="onCompressionCompleted">Method to process compressed data</param>
        /// <param name="state">User-defined state passed to <paramref name="onCompressionCompleted"/> method</param>
        /// <exception cref="TJException">Throws if compress function failed.</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        /// <returns>
        /// </returns>
        public void Compress(IntPtr srcPtr, int stride, int width, int height,
            TJPixelFormats tjPixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags,
            TJCompressionComplete onCompressionCompleted, object state)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");
            if (onCompressionCompleted == null)
                throw new ArgumentNullException(nameof(onCompressionCompleted));

            CheckOptionsCompatibilityAndThrow(subSamp, tjPixelFormat);

            var buf = IntPtr.Zero;
            ulong bufSize = 0;
            try
            {
                var result = TurboJpegImport.tjCompress2(
                    _compressorHandle,
                    srcPtr,
                    width,
                    stride,
                    height,
                    (int)tjPixelFormat,
                    ref buf,
                    ref bufSize,
                    (int)subSamp,
                    quality,
                    (int)flags);

                if (result == -1)
                {
                    TJUtils.GetErrorAndThrow();
                }
                onCompressionCompleted(buf, (int)bufSize, state);
            }
            finally
            {
                TurboJpegImport.tjFree(buf);
            }
        }

        /// <summary>
        /// Compresses input image to the jpeg format with specified quality
        /// </summary>
        /// <param name="srcPtr">
        /// Pointer to an image buffer containing RGB, grayscale, or CMYK pixels to be compressed.
        /// This buffer is not modified.
        /// </param>
        /// <param name="stride">
        /// Bytes per line in the source image.
        /// Normally, this should be <c>width * BytesPerPixel</c> if the image is unpadded,
        /// or <c>TJPAD(width * BytesPerPixel</c> if each line of the image
        /// is padded to the nearest 32-bit boundary, as is the case for Windows bitmaps.
        /// You can also be clever and use this parameter to skip lines, etc.
        /// Setting this parameter to 0 is the equivalent of setting it to
        /// <c>width * BytesPerPixel</c>.
        /// </param>
        /// <param name="width">Width (in pixels) of the source image</param>
        /// <param name="height">Height (in pixels) of the source image</param>
        /// <param name="tjPixelFormat">Pixel format of the source image (see <see cref="TJPixelFormats"/> "Pixel formats")</param>
        /// <param name="subSamp">
        /// The level of chrominance subsampling to be used when
        /// generating the JPEG image (see <see cref="TJSubsamplingOptions"/> "Chrominance subsampling options".)
        /// </param>
        /// <param name="quality">The image quality of the generated JPEG image (1 = worst, 100 = best)</param>
        /// <param name="flags">The bitwise OR of one or more of the <see cref="TJFlags"/> "flags"</param>
        /// <param name="onAsyncCompressionCompleted">Method to process compressed data</param>
        /// <param name="state">User-defined state passed to <paramref name="onAsyncCompressionCompleted"/> method</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="TJException">Throws if compress function failed.</exception>
        /// <exception cref="ObjectDisposedException">Object is disposed and can not be used anymore</exception>
        /// <exception cref="NotSupportedException">
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        /// <returns>
        /// </returns>
        public async Task CompressAsync(IntPtr srcPtr, int stride, int width, int height,
            TJPixelFormats tjPixelFormat, TJSubsamplingOptions subSamp, int quality, TJFlags flags,
            TJAsyncCompressionComplete onAsyncCompressionCompleted, object state, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("this");
            if (onAsyncCompressionCompleted == null)
                throw new ArgumentNullException(nameof(onAsyncCompressionCompleted));

            CheckOptionsCompatibilityAndThrow(subSamp, tjPixelFormat);

            var buf = IntPtr.Zero;
            ulong bufSize = 0;
            try
            {
                var result = TurboJpegImport.tjCompress2(
                    _compressorHandle,
                    srcPtr,
                    width,
                    stride,
                    height,
                    (int)tjPixelFormat,
                    ref buf,
                    ref bufSize,
                    (int)subSamp,
                    quality,
                    (int)flags);

                if (result == -1)
                {
                    TJUtils.GetErrorAndThrow();
                }

                await onAsyncCompressionCompleted(buf, (int)bufSize, state, cancellationToken);
            }
            finally
            {
                TurboJpegImport.tjFree(buf);
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
            TurboJpegImport.tjDestroy(_compressorHandle);
        }


        /// <summary>
        /// Finalizer
        /// </summary>
        ~TJCompressor()
        {
            Dispose(false);
        }

        /// <exception cref="NotSupportedException"> 
        /// Some parameters' values are incompatible:
        /// <list type="bullet">
        /// <item><description>Subsampling not equals to <see cref="TJSubsamplingOptions.TJSAMP_GRAY"/> and pixel format <see cref="TJPixelFormats.TJPF_GRAY"/></description></item>
        /// </list>
        /// </exception>
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void CheckOptionsCompatibilityAndThrow(TJSubsamplingOptions subSamp, TJPixelFormats srcFormat)
        {
            if (srcFormat == TJPixelFormats.TJPF_GRAY && subSamp != TJSubsamplingOptions.TJSAMP_GRAY)
                throw new NotSupportedException(
                    $"Subsampling differ from {TJSubsamplingOptions.TJSAMP_GRAY} for pixel format {TJPixelFormats.TJPF_GRAY} is not supported");
        }
    }

    /// <summary>
    /// Delegate to process compressed data
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data</param>
    /// <param name="compressedDataSize">Size of compressed data in bytes</param>
    /// <param name="state">User-defined object</param>
    public delegate void TJCompressionComplete(IntPtr compressedDataPtr, int compressedDataSize, object state);

    /// <summary>
    /// Delegate to process compressed data
    /// </summary>
    /// <param name="compressedDataPtr">Pointer to compressed data</param>
    /// <param name="compressedDataSize">Size of compressed data in bytes</param>
    /// <param name="state">User-defined object</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An instance of <see cref="Task"/> to await completion.</returns>
    public delegate Task TJAsyncCompressionComplete(IntPtr compressedDataPtr, int compressedDataSize, object state, CancellationToken cancellationToken);

}
