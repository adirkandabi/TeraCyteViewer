using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace TeraCyteViewer.Utils
{
    public static class ImageHelper
    {
        public static BitmapImage? FromBase64PngSafe(string base64, ILogger? log = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64))
                    throw new InvalidOperationException("Empty image data");

                var trimmed = base64.Trim().Replace("\n", "").Replace("\r", "");
                if (!IsLikelyBase64(trimmed))
                    throw new FormatException("Not a valid Base64 payload");

                var bytes = Convert.FromBase64String(trimmed);
                using var ms = new MemoryStream(bytes) { Position = 0 };

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                return bmp;
            }
            catch (Exception ex)
            {
                log?.LogWarning(ex, "Image decode failed. base64Len={Len} head={Head}",
                    base64?.Length ?? 0,
                    base64 is { Length: > 16 } ? base64.Substring(0, 16) : base64);
                return null;
            }
        }

        private static bool IsLikelyBase64(string s)
        {
            foreach (var ch in s)
            {
                if ((ch >= 'A' && ch <= 'Z') ||
                    (ch >= 'a' && ch <= 'z') ||
                    (ch >= '0' && ch <= '9') ||
                    ch == '+' || ch == '/' || ch == '=')
                    continue;
                return false;
            }
            return true;
        }
    }
}
