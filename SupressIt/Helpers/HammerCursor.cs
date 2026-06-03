using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SupressIt.Helpers
{
    /// <summary>
    /// Creates a cute anime-style hammer cursor for kill buttons.
    /// Draws a small hammer emoji-style icon as a 32x32 cursor.
    /// </summary>
    public static class HammerCursor
    {
        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool   fIcon;
            public int    xHotspot;
            public int    yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        private static Cursor _hammer;

        public static Cursor Get()
        {
            if (_hammer != null) return _hammer;

            try
            {
                _hammer = BuildCursor();
            }
            catch
            {
                _hammer = Cursors.Hand;
            }
            return _hammer;
        }

        private static Cursor BuildCursor()
        {
            const int size = 48;

            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Hammer head — warm pink/salmon
            using var headBrush = new SolidBrush(Color.FromArgb(255, 255, 150, 170));
            g.FillRoundedRect(headBrush, 18, 4, 26, 16, 4);

            // Hammer outline
            using var outline = new System.Drawing.Pen(Color.FromArgb(200, 180, 80, 100), 1.5f);
            g.DrawRoundedRect(outline, 18, 4, 26, 16, 4);

            // Handle — warm wood brown
            using var handleBrush = new SolidBrush(Color.FromArgb(255, 210, 160, 100));
            g.FillRoundedRect(handleBrush, 10, 16, 14, 30, 3);
            using var handleOutline = new System.Drawing.Pen(Color.FromArgb(200, 150, 100, 50), 1.2f);
            g.DrawRoundedRect(handleOutline, 10, 16, 14, 30, 3);

            // Sparkle stars ✦
            using var sparkBrush = new SolidBrush(Color.FromArgb(255, 255, 220, 100));
            g.FillEllipse(sparkBrush, 2,  2,  5, 5);
            g.FillEllipse(sparkBrush, 38, 6,  4, 4);
            g.FillEllipse(sparkBrush, 34, 24, 6, 6);

            // Convert to cursor
            using var ms  = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            // Build cursor from bitmap
            IntPtr hBitmap = bmp.GetHbitmap(Color.Transparent);
            try
            {
                // Create mask (all black = opaque cursor)
                using var mask = new Bitmap(size, size, PixelFormat.Format1bppIndexed);
                IntPtr hMask   = mask.GetHbitmap();

                var info = new ICONINFO
                {
                    fIcon    = false,      // cursor, not icon
                    xHotspot = 12,         // tip of the hammer
                    yHotspot = 4,
                    hbmMask  = hMask,
                    hbmColor = hBitmap
                };

                IntPtr hCursor = CreateIconIndirect(ref info);
                DeleteObject(hMask);

                if (hCursor == IntPtr.Zero) return Cursors.Hand;

                var cursor = CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
                return cursor;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    }

    // Extension methods so we can draw rounded rects on System.Drawing.Graphics
    internal static class GraphicsExt
    {
        public static void FillRoundedRect(this Graphics g, Brush b, float x, float y, float w, float h, float r)
        {
            using var path = RoundedRect(x, y, w, h, r);
            g.FillPath(b, path);
        }

        public static void DrawRoundedRect(this Graphics g, System.Drawing.Pen p, float x, float y, float w, float h, float r)
        {
            using var path = RoundedRect(x, y, w, h, r);
            g.DrawPath(p, path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Safe handle wrapper for cursor
    internal class SafeCursorHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        [DllImport("user32.dll")] private static extern bool DestroyCursor(IntPtr handle);

        public SafeCursorHandle(IntPtr handle) : base(true) { SetHandle(handle); }
        protected override bool ReleaseHandle() => DestroyCursor(handle);
    }
}
