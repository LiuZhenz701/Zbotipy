using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LocalMusicPlayer.Services;

/// <summary>
/// Samples on-screen pixels behind a window rectangle to estimate background luminance.
/// Uses an edge-only mask so centered lyric text does not dominate the average.
/// </summary>
internal static class DesktopBackdropSampler
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static bool EnsureBitmap(ref Bitmap? bmp, int w, int h)
    {
        if (w <= 0 || h <= 0)
            return false;

        if (bmp != null && bmp.Width == w && bmp.Height == h)
            return true;

        bmp?.Dispose();
        bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        return true;
    }

    /// <summary>
    /// Returns approximate relative luminance in [0,1] (sRGB quick blend). Returns null on failure.
    /// </summary>
    public static double? TrySampleEdgeLuminance(IntPtr hwnd, ref Bitmap? scratch)
    {
        if (hwnd == IntPtr.Zero)
            return null;

        if (!GetWindowRect(hwnd, out var rc))
            return null;

        int w = rc.Right - rc.Left;
        int h = rc.Bottom - rc.Top;
        if (!EnsureBitmap(ref scratch, w, h) || scratch == null)
            return null;

        try
        {
            using var g = Graphics.FromImage(scratch);
            g.CopyFromScreen(rc.Left, rc.Top, 0, 0, scratch.Size, CopyPixelOperation.SourceCopy);
        }
        catch
        {
            return null;
        }

        return AverageEdgeLuminance(scratch);
    }

    private static double AverageEdgeLuminance(Bitmap bmp)
    {
        var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int w = bmp.Width;
            int h = bmp.Height;
            int edge = Math.Clamp(Math.Min(w, h) / 5, 12, 56);

            int stride = data.Stride;
            int byteCount = Math.Abs(stride) * h;
            var buf = new byte[byteCount];
            Marshal.Copy(data.Scan0, buf, 0, byteCount);

            double sum = 0;
            int n = 0;

            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                for (int x = 0; x < w; x++)
                {
                    if (x >= edge && x < w - edge && y >= edge && y < h - edge)
                        continue;

                    int i = row + x * 4;
                    if (i + 2 >= buf.Length)
                        continue;

                    byte b = buf[i];
                    byte g = buf[i + 1];
                    byte r = buf[i + 2];
                    sum += (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
                    n++;
                }
            }

            return n > 0 ? sum / n : 0.5;
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
