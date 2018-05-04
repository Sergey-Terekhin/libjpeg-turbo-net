using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TurboJpegWrapper.Tests
{
    // ReSharper disable once InconsistentNaming
    [TestFixture]
    public class TJCompressorTests
    {
        private TJCompressor _compressor;

        private string OutDirectory => Path.Combine(TestUtils.BinPath, "compress_images_out");

        [TestFixtureSetUp]
        public void SetUp()
        {
            TJInitializer.Initialize(logger: Console.WriteLine);

            _compressor = new TJCompressor();
            if (Directory.Exists(OutDirectory))
            {
                Directory.Delete(OutDirectory, true);
            }
            Directory.CreateDirectory(OutDirectory);
        }

        [TestFixtureTearDown]
        public void Clean()
        {
            _compressor.Dispose();
        }
        
        [Test, Combinatorial]
        public void CompressIntPtr(
            [Values
            (TJSubsamplingOptions.TJSAMP_GRAY,
            TJSubsamplingOptions.TJSAMP_411,
            TJSubsamplingOptions.TJSAMP_420,
            TJSubsamplingOptions.TJSAMP_440,
            TJSubsamplingOptions.TJSAMP_422,
            TJSubsamplingOptions.TJSAMP_444)]TJSubsamplingOptions options,
            [Values(1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100)]int quality)
        {
            foreach (var bitmap in TestUtils.GetTestImages("*.bmp"))
            {
                BitmapData data = null;
                try
                {
                    data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                        bitmap.PixelFormat);

                    Trace.WriteLine($"Options: {options}; Quality: {quality}");
                    Assert.DoesNotThrow(() =>
                    {
                        var result = _compressor.Compress(data.Scan0, data.Stride, data.Width, data.Height, TestUtils.ConvertPixelFormat(data.PixelFormat), options, quality, TJFlags.NONE);
                        Assert.NotNull(result);
                    });

                }
                finally
                {
                    if (data != null)
                    {
                        bitmap.UnlockBits(data);
                    }
                    bitmap.Dispose();
                }
            }
        }
        [Test, Combinatorial]
        public void CompressByteArray(
            [Values
            (TJSubsamplingOptions.TJSAMP_GRAY,
            TJSubsamplingOptions.TJSAMP_411,
            TJSubsamplingOptions.TJSAMP_420,
            TJSubsamplingOptions.TJSAMP_440,
            TJSubsamplingOptions.TJSAMP_422,
            TJSubsamplingOptions.TJSAMP_444)]TJSubsamplingOptions options,
            [Values(1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100)]int quality)
        {
            foreach (var bitmap in TestUtils.GetTestImages("*.bmp"))
            {
                try
                {
                    var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                        bitmap.PixelFormat);

                    var stride = data.Stride;
                    var width = data.Width;
                    var height = data.Height;
                    var pixelFormat = TestUtils.ConvertPixelFormat(data.PixelFormat);


                    var buf = new byte[stride * height];
                    Marshal.Copy(data.Scan0, buf, 0, buf.Length);
                    bitmap.UnlockBits(data);

                    Trace.WriteLine($"Options: {options}; Quality: {quality}");
                    Assert.DoesNotThrow(() =>
                    {
                        var result = _compressor.Compress(buf, stride, width, height, pixelFormat, options, quality, TJFlags.NONE);
                        Assert.NotNull(result);
                    });

                }
                finally
                {
                    bitmap.Dispose();
                }
            }
        }


        [Test, Combinatorial]
        public async void CompressAsync(
            [Values
            (TJSubsamplingOptions.TJSAMP_GRAY,
                TJSubsamplingOptions.TJSAMP_411,
                TJSubsamplingOptions.TJSAMP_420,
                TJSubsamplingOptions.TJSAMP_440,
                TJSubsamplingOptions.TJSAMP_422,
                TJSubsamplingOptions.TJSAMP_444)]TJSubsamplingOptions options,
            [Values(1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100)]int quality)
        {
            foreach (var bitmap in TestUtils.GetTestImages("*.bmp"))
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
                    bitmap.PixelFormat);
                try
                {
                    var stride = data.Stride;
                    var width = data.Width;
                    var height = data.Height;
                    var pixelFormat = TestUtils.ConvertPixelFormat(data.PixelFormat);

                    var buf = new byte[stride * height];
                    Marshal.Copy(data.Scan0, buf, 0, buf.Length);

                    Trace.WriteLine($"Options: {options}; Quality: {quality}");
                    CancellationTokenSource cancellation = new CancellationTokenSource();

                    await _compressor.CompressAsync(data.Scan0, stride, width, height, pixelFormat, options, quality, TJFlags.NONE,
                        async (ptr, size, state, token) =>
                        {
                            await Task.Delay(10, token);

                            Assert.IsTrue(ptr != IntPtr.Zero, "ptr != IntPtr.Zero");
                            Assert.IsTrue(size > 0, "size > 0");
                            Assert.IsNull(state, "state != null");
                        }, null, cancellation.Token);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                    bitmap.Dispose();
                }
            }
        }
    }
}
