// =============================================================================
// GPAL - Generally Positive Automation Library
// Copyright © 2026 Software Decisions, Inc. All rights reserved.
//
// This file is part of GPAL.
// Licensed under the Business Source License 1.1
//
// Primary development, architecture, and vision by Michael B. Vederman,
// CEO of Software Decisions, Inc., Texas.
//
// Internal development maintained privately.
// Public releases appear on GitHub: https://github.com/SoftwareDecisionsInc/GPAL.
//
// See LICENSE for full terms, including Additional Use Grant.
// =============================================================================

using System.Drawing;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace GenerallyPositive.Image
{
    /// <summary>
    /// Image matching for GPAL. GPAL finds images through whatever matcher is registered with it and carries none of
    /// its own, so a workflow that uses image selectors adds this package and registers it once at startup.
    /// </summary>
    /// <example>
    /// <code>
    /// GPALImage.Register();
    ///
    /// Selector logo = GPAL.Selector
    ///     .WithImage((GPALFile)@"C:\templates\logo.png")
    ///     .ToGPALObject();
    /// </code>
    /// </example>
    public static class GPALImage
    {
        /// <summary>
        /// Hands GPAL the OpenCV matcher, so image selectors find what they are looking for. Call it once, before
        /// the first image selector runs.
        /// <br/><br/>
        /// OpenCV ships its native library for 64-bit Windows only, so a 32-bit process cannot load it. An AnyCPU
        /// program runs 32-bit when Prefer 32-bit is on, which is the default. In a 32-bit process nothing is
        /// registered and a warning says why, rather than OpenCV failing on the first match.
        /// </summary>
        public static void Register()
        {
            if (true == System.Environment.Is64BitProcess)
            {
                GPAL.RegisterImageMatcher(new OpenCvImageMatcher());
                GPAL.RegisterQRCodeReader(new OpenCvQRCodeReader());
            }
            else
                GPAL.PublishSimpleEvent(Enums.GPALEventType.WARNING, "GPAL.Image needs a 64-bit process, since OpenCV ships its native library for 64-bit Windows only, and this program is running 32-bit. Turn off Prefer 32-bit in the project's build settings, or target x64, so image selectors can match.");
        }
    }

    /// <summary>
    /// Finds a picture on a capture with OpenCV template matching, both images in grayscale, taking the best match
    /// when it is at least as close as the matching percentage asks.
    /// </summary>
    internal sealed class OpenCvImageMatcher : IImageMatcher
    {
        /// <summary>Where the picture in <paramref name="templatePath"/> is on <paramref name="screen"/>.</summary>
        /// <param name="screen">The capture to search</param>
        /// <param name="templatePath">The picture to find</param>
        /// <param name="matchingPercentage">How close a match has to be, 0 to 100</param>
        /// <returns>Where it is, in the capture's coordinates, or an empty rectangle when it is not there</returns>
        public Rectangle FindImage(Bitmap screen, string templatePath, int matchingPercentage)
        {
            Rectangle retVal = Rectangle.Empty;

            using (Mat source = screen.ToMat())
            using (Mat template = new Mat(templatePath))
            using (Mat sourceGray = new Mat())
            using (Mat templateGray = new Mat())
            using (Mat result = new Mat())
            {
                if (true == source.Empty())
                    GPAL.PublishSimpleEvent(Enums.GPALEventType.WARNING, "The capture to search is empty");
                else if (true == template.Empty())
                    GPAL.PublishSimpleEvent(Enums.GPALEventType.WARNING, $"The picture [{templatePath}] could not be read");
                else
                {
                    Cv2.CvtColor(source, sourceGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.MatchTemplate(sourceGray, templateGray, result, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(result, out _, out double best, out _, out OpenCvSharp.Point bestAt);

                    if (best >= matchingPercentage * 0.01)
                        retVal = new Rectangle(bestAt.X, bestAt.Y, template.Width, template.Height);
                }
            }

            return retVal;
        }
    }

    /// <summary>
    /// Reads a QR code with OpenCV's WeChat QR code reader, which finds the code anywhere on the picture. It runs
    /// without its optional detection models; OpenCV's plain QRCodeDetector misses codes this one reads.
    /// </summary>
    internal sealed class OpenCvQRCodeReader : IQRCodeReader
    {
        /// <summary>Reads the QR code on <paramref name="image"/>.</summary>
        /// <param name="image">A picture with a QR code anywhere on it</param>
        /// <returns>The text the QR code holds, or an empty string when there is none</returns>
        public string ReadQRCode(Bitmap image)
        {
            string retVal = "";

            if (null == image)
                GPAL.PublishSimpleEvent(Enums.GPALEventType.WARNING, "There is no picture to read a QR code from");
            else
            {
                using (Mat source = image.ToMat())
                using (Mat gray = new Mat())
                using (WeChatQRCode reader = new WeChatQRCode("", "", "", ""))
                {
                    if (1 == source.Channels())
                        source.CopyTo(gray);
                    else
                        Cv2.CvtColor(source, gray, 4 == source.Channels() ? ColorConversionCodes.BGRA2GRAY : ColorConversionCodes.BGR2GRAY);
                    retVal = reader.DetectAndDecode(gray, out _).FirstOrDefault() ?? "";

                    // a phone photo of a monitor carries the screen's pixel grid as a pattern over the code. A light
                    // blur smooths a fine pattern out
                    if (0 == retVal.Length)
                    {
                        using (Mat smoothed = new Mat())
                        {
                            Cv2.GaussianBlur(gray, smoothed, new OpenCvSharp.Size(3, 3), 0);
                            retVal = reader.DetectAndDecode(smoothed, out _).FirstOrDefault() ?? "";
                        }
                    }

                    // then the picture shrunk to a 1000, 900, 800 and 700 pixel long side in turn, until one reads
                    foreach (int longSide in new[] { 1000, 900, 800, 700 })
                    {
                        if (0 == retVal.Length && longSide < System.Math.Max(gray.Width, gray.Height))
                        {
                            using (Mat shrunk = new Mat())
                            {
                                double scale = (double)longSide / System.Math.Max(gray.Width, gray.Height);
                                Cv2.Resize(gray, shrunk, new OpenCvSharp.Size(), scale, scale, InterpolationFlags.Area);
                                retVal = reader.DetectAndDecode(shrunk, out _).FirstOrDefault() ?? "";
                            }
                        }
                    }
                }
            }

            return retVal;
        }
    }
}
