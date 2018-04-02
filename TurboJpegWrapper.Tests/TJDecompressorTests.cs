using System;
using System.Drawing.Imaging;
using System.IO;
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
            NativeModulesLoader.LoadLibraries("turbojpeg.dll", Console.WriteLine);
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
            NativeModulesLoader.FreeUnmanagedModules("turbojpeg.dll");
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
    }
}
