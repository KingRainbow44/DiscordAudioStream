﻿using System;
using System.Drawing;

using DLLs;

namespace DiscordAudioStream.ScreenCapture.CaptureStrategy
{
    public class BitBltWindowCapture : WindowCapture
    {
        private readonly CaptureSource capture;
        private readonly IntPtr windowHandle;

        public BitBltWindowCapture(IntPtr hWnd, bool captureCursor)
        {
            windowHandle = hWnd;
            BitBltCapture bitBlt = new BitBltCapture();
            bitBlt.CaptureAreaRect += () => GetWindowArea(windowHandle);

            if (captureCursor)
            {
                CursorPainter paintCursor = new CursorPainter(bitBlt);
                paintCursor.CaptureAreaRect += () => GetWindowArea(windowHandle);
                capture = paintCursor;
            }
            else
            {
                capture = bitBlt;
            }

            SetWindowTopmost(windowHandle, true);
        }

        public override Bitmap CaptureFrame() => capture.CaptureFrame();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            capture.Dispose();

            SetWindowTopmost(windowHandle, false);
        }

        private static void SetWindowTopmost(IntPtr hWnd, bool bringToFront)
        {
            IntPtr insertAfter = bringToFront ? User32.HWND_TOPMOST : User32.HWND_NOTOPMOST;
            User32.SetWindowPos(hWnd, insertAfter, 0, 0, 0, 0, User32.SWP_NOMOVE | User32.SWP_NOSIZE);
        }
    }
}
