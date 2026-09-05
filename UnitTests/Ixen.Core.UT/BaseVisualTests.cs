using Ixen.Core.Rendering;
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
        private string _outputRendersDir = Path.Combine(Environment.CurrentDirectory, "_RenderResults");
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
            string fileDiffPath = Path.Combine(_outputRendersDir, $"{testMethodName}_DIFF.png");

            if (expectedHash == hash)
            {
                File.Delete(fileErrorPath);
                File.Delete(fileDiffPath);

                if (!File.Exists(fileExpectedPath))
                {
                    DumpBitmapToFile(fileExpectedPath, bitmap);
                }
            }
            else
            {
                DumpBitmapToFile(fileErrorPath, bitmap);

                Assert.Fail(
                    $"Render mismatch for {testMethodName} at {width}x{height}." +
                    $"{Environment.NewLine}  expected hash : {expectedHash}" +
                    $"{Environment.NewLine}  actual hash   : {hash}" +
                    $"{Environment.NewLine}  difference    : {Differences(fileExpectedPath, bitmap, fileDiffPath)}" +
                    $"{Environment.NewLine}  rendered      : {fileErrorPath}" +
                    $"{Environment.NewLine}  baseline      : {fileExpectedPath}" +
                    $"{Environment.NewLine}If the change is intentional, inspect the rendered file then update the expected hash.");
            }
        }

        private string Differences(string baselinePath, SKBitmap rendered, string diffPath)
        {
            if (!File.Exists(baselinePath))
            {
                return "no baseline on this machine, so nothing to compare against";
            }

            using (SKBitmap decoded = SKBitmap.Decode(baselinePath))
            {
                if (decoded == null)
                {
                    return $"the baseline at {baselinePath} could not be read";
                }

                using (SKBitmap baseline = BitmapDifference.Normalized(decoded))
                using (SKBitmap actual = BitmapDifference.Normalized(rendered))
                {
                    if (baseline == null || actual == null)
                    {
                        return "one of the two pictures could not be read";
                    }

                    if (!BitmapDifference.Comparable(baseline, actual))
                    {
                        return $"a {baseline.Width}x{baseline.Height} baseline against a "
                            + $"{actual.Width}x{actual.Height} render, so the sizes alone differ";
                    }

                    BitmapDifference difference = BitmapDifference.Compare(baseline, actual);

                    if (!difference.Any)
                    {
                        return "the pixels are identical, so the baseline is older than this hash";
                    }

                    using (SKBitmap highlight = BitmapDifference.Highlight(baseline, actual))
                    {
                        DumpBitmapToFile(diffPath, highlight);
                    }

                    return $"{difference.Describe()}{Environment.NewLine}  highlighted   : {diffPath}";
                }
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
            {}
        }
    }
}
