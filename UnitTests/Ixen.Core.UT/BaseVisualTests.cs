using Ixen.Core.Visual;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Ixen.Core.UT
{
    public abstract class BaseVisualTests
    {
        private string _outputRendersDir = Path.Combine(Environment.CurrentDirectory, "RenderResults");
        private static MD5 _md5 = MD5.Create();

        public BaseVisualTests()
        {
            Directory.CreateDirectory(_outputRendersDir);
        }

        protected void AssertVisual(string expectedHash, VisualElement root, int width = 1920, int height = 1080)
        {
            var testMethod = new StackTrace().GetFrame(1)?.GetMethod();
            var testMethodName = $"{testMethod?.ReflectedType?.Name}_{testMethod?.Name}";

            var surface = new IxenSurface(root);
            surface.ComputeLayout(width, height);
            SKBitmap bitmap = surface.RenderToBitmap();
            byte[] md5hash = _md5.ComputeHash(bitmap.Bytes);
            string hash = Convert.ToHexString(md5hash).ToLower();

            string fileExpectedPath = Path.Combine(_outputRendersDir, $"{testMethodName}_EXPECTED.png");
            string fileErrorPath = Path.Combine(_outputRendersDir, $"{testMethodName}_NOK.png");
            if (expectedHash == hash)
            {
                File.Delete(fileErrorPath);
                if (!File.Exists(fileExpectedPath))
                {
                    DumpBitmapToFile(fileExpectedPath, bitmap);
                }
            }
            else
            {
                DumpBitmapToFile(fileErrorPath, bitmap);

                Assert.Fail();
            }
        }

        private void DumpBitmapToFile(string fileOutput, SKBitmap bitmap)
        {
            try
            {
                using (var fs = new FileStream(fileOutput, FileMode.Create, FileAccess.Write, FileShare.Write))
                using (var wstream = new SKManagedWStream(fs))
                {
                    bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100);
                }
            }
            catch
            { }
        }
    }
}
