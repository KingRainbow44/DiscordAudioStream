﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;


namespace DiscordAudioStream
{
	internal class ScreenCaptureWorker
	{
		private IScreenCaptureMaster master;

		private static ConcurrentQueue<Bitmap> frameQueue = new ConcurrentQueue<Bitmap>();
		private const int LIMIT_QUEUE_SZ = 3;

		private int INTERVAL_MS;
		private Thread captureThread;
		private Size oldSize = new Size(-1, -1);


		// Return the next frame, if it exists (null otherwise)
		public static Bitmap GetNextFrame()
		{
			Bitmap frame;
			bool success = frameQueue.TryDequeue(out frame);

			if (success) return frame;
			return null;
		}



		public ScreenCaptureWorker(double targetFramerate, IScreenCaptureMaster captureMaster)
		{
			master = captureMaster;

			if (targetFramerate <= 0)
			{
				throw new ArgumentOutOfRangeException("The target framerate must be greater than 0");
			}

			INTERVAL_MS = (int) (1000.0 / targetFramerate);

			captureThread = new Thread(() =>
			{
				Stopwatch stopwatch = new Stopwatch();

				while (true)
				{
					stopwatch.Restart();
					try
					{
						EnqueueFrame();
					}
					catch (ThreadAbortException)
					{
						break;
					}
					catch (Exception)
					{
						master.AbortCapture();
					}
					stopwatch.Stop();

					int wait = INTERVAL_MS - (int)stopwatch.ElapsedMilliseconds;
					if (wait > 0)
					{
						Thread.Sleep(wait);
					}
				}
			});
			captureThread.IsBackground = true;
			captureThread.Start();
		}

		public void Stop()
		{
			captureThread.Abort();
		}


		private void EnqueueFrame()
		{
			Size windowSize;
			Point position;
			bool captureCursor = master.IsCapturingCursor();
			if (ProcessHandleManager.CapturingWindow)
			{
				IntPtr proc = ProcessHandleManager.GetHandle();
				GetWindowArea(proc, out windowSize, out position);

				if (windowSize != oldSize)
				{
					oldSize = windowSize;
					master.CapturedWindowSizeChanged(windowSize);
				}
			}
			else
			{
				master.GetCaptureArea(out windowSize, out position);
			}

			Bitmap BMP = CaptureScreen(position, windowSize);
			if (captureCursor)
			{
				User32.CURSORINFO pci;
				pci.cbSize = Marshal.SizeOf(typeof(User32.CURSORINFO));

				if (User32.GetCursorInfo(out pci) && pci.flags == User32.CURSOR_SHOWING)
				{
					Graphics g = Graphics.FromImage(BMP);
					User32.DrawIcon(g.GetHdc(), pci.ptScreenPos.x - position.X, pci.ptScreenPos.y - position.Y, pci.hCursor);
					g.ReleaseHdc();
					g.Dispose();
				}
			}

			frameQueue.Enqueue(BMP);

			// Limit the size of frameQueue to LIMIT_QUEUE_SZ
			if (frameQueue.Count > LIMIT_QUEUE_SZ)
			{
				frameQueue.TryDequeue(out Bitmap b);
				b.Dispose();
			}
		}

		private static void GetWindowArea(IntPtr hwnd, out Size windowSize, out Point position)
		{
			User32.GetWindowRect(hwnd, out User32.RECT rc);
			windowSize = new Size(rc.Width, rc.Height);
			position = new Point(rc.X, rc.Y);
		}

		private static Bitmap CaptureScreen(Point startPos, Size size)
		{
			int hdcSrc = User32.GetWindowDC(User32.GetDesktopWindow());
			int hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
			int hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, size.Width, size.Height);
			GDI32.SelectObject(hdcDest, hBitmap);
			GDI32.BitBlt(hdcDest, 0, 0, size.Width, size.Height, hdcSrc, startPos.X, startPos.Y, 0x00CC0020);

			Bitmap result = Image.FromHbitmap(new IntPtr(hBitmap));

			// Cleanup
			User32.ReleaseDC(User32.GetDesktopWindow(), hdcSrc);
			GDI32.DeleteDC(hdcDest);
			GDI32.DeleteObject(hBitmap);

			return result;
		}
	}
}
