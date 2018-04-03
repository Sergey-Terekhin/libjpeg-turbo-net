using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace TurboJpegWrapper.Tests
{
    [TestFixture]
    // ReSharper disable once InconsistentNaming
    internal class TJDecompressorTests
    {
        private TJDecompressor _decompressor;
        private string OutDirectory => Path.Combine(TestUtils.BinPath, "decompress_images_out");

        [TestFixtureSetUp]
        public void SetUp()
        {
            TJInitializer.Initialize(logger: Console.WriteLine);
            _decompressor = new TJDecompressor();
            if (Directory.Exists(OutDirectory))
            {
                Directory.Delete(OutDirectory, true);
            }
            Directory.CreateDirectory(OutDirectory);
        }

        [TestFixtureTearDown]
        public void Clean()
        {
            _decompressor.Dispose();
        }

        [Test, Combinatorial]
        public void DecompressByteArray(
            [Values(
            PixelFormat.Format32bppArgb,
            PixelFormat.Format24bppRgb,
            PixelFormat.Format8bppIndexed)]PixelFormat format)
        {
            foreach (var data in TestUtils.GetTestImagesData("*.jpg"))
            {
                Assert.DoesNotThrow(() =>
                {
                    var result = _decompressor.Decompress(data.Item2, TestUtils.ConvertPixelFormat(format), TJFlags.NONE);
                    Assert.NotNull(result);
                });
            }
        }

        [Test, Combinatorial]
        public void DecompressIntPtr(
           [Values(
            PixelFormat.Format32bppArgb,
            PixelFormat.Format24bppRgb,
            PixelFormat.Format8bppIndexed)]PixelFormat format)
        {
            foreach (var data in TestUtils.GetTestImagesData("*.jpg"))
            {
                var dataPtr = TJUtils.CopyDataToPointer(data.Item2);
                Assert.DoesNotThrow(() =>
                {
                    var result = _decompressor.Decompress(dataPtr, (ulong)data.Item2.Length, TestUtils.ConvertPixelFormat(format), TJFlags.NONE);
                    Assert.NotNull(result);
                });
                TJUtils.FreePtr(dataPtr);
            }
        }

        [Test, Combinatorial]
        public unsafe void DecompressIntPtrToIntPtr(
            [Values(
                PixelFormat.Format32bppArgb,
                PixelFormat.Format24bppRgb,
                PixelFormat.Format8bppIndexed)]PixelFormat format)
        {
            foreach (var data in TestUtils.GetTestImagesData("*.jpg"))
            {
                var dataPtr = TJUtils.CopyDataToPointer(data.Item2);
                
                Assert.DoesNotThrow(() =>
                {
                    _decompressor.GetImageInfo(dataPtr, (ulong)data.Item2.Length, TestUtils.ConvertPixelFormat(format), out var width, out var height, out var stride, out var decompressedBufferSize);

                    var decompressed = new byte[decompressedBufferSize];
                    fixed (byte* ptr = decompressed)
                    {
                        _decompressor.Decompress(
                            dataPtr, 
                            (ulong) data.Item2.Length,
                            (IntPtr)ptr, 
                            decompressedBufferSize,
                            TestUtils.ConvertPixelFormat(format), 
                            TJFlags.NONE);
                    }

                    Assert.IsTrue(decompressed.Any(b => b != 0));

                });
                TJUtils.FreePtr(dataPtr);
            }
        }
    }
}
