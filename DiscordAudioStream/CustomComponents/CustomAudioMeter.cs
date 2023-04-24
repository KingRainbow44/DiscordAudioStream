﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace CustomComponents
{
	public class CustomAudioMeter : NAudio.Gui.VolumeMeter
	{
		private const double BACKGROUND_PERCENT = 0.2;
		private const int BACKGROUND_COLOR_INACTIVE = unchecked((int) 0xff225561);
		private const int BACKGROUND_COLOR_ACTIVE = unchecked((int) 0xff61c4db);

		private const double SPEAKING_PERCENT = 0.8;
		private const int SPEAKING_COLOR_INACTIVE = unchecked((int) 0xff226325);
		private const int SPEAKING_COLOR_ACTIVE = unchecked((int) 0xff5fdd65);

		private const double LOUD_PERCENT = 0.95;
		private const int LOUD_COLOR_INACTIVE = unchecked((int) 0xff556122);
		private const int LOUD_COLOR_ACTIVE = unchecked((int) 0xffc4db61);

		private const double CLIPPING_PERCENT = 1;
		private const int CLIPPING_COLOR_INACTIVE = unchecked((int) 0xff5c2222);
		private const int CLIPPING_COLOR_ACTIVE = unchecked((int) 0xffce0606);

		private bool darkMode;

		public CustomAudioMeter()
		{
			if (darkMode)
			{
				SetStyle(ControlStyles.UserPaint, value: true);
				SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
			}
		}

		public void SetDarkMode(bool dark)
		{
			darkMode = dark;
		}


		protected override void OnPaint(PaintEventArgs pe)
		{
			// Draw an audio meter, see https://github.com/p-rivero/DiscordAudioStream/issues/15

			Brush foregroundBrush = new SolidBrush(darkMode ? Color.White : Color.Black);

			double db = 20.0 * Math.Log10(Amplitude);
			db = Math.Min(db, MaxDb);
			db = Math.Max(db, MinDb);
			double percent = (db - (double)MinDb) / (double)(MaxDb - MinDb);

			// Background volume
			DrawMeterSegment(pe.Graphics, percent, 0.0, BACKGROUND_PERCENT, BACKGROUND_COLOR_ACTIVE, BACKGROUND_COLOR_INACTIVE);
			// Speaking volume
			DrawMeterSegment(pe.Graphics, percent, BACKGROUND_PERCENT, SPEAKING_PERCENT, SPEAKING_COLOR_ACTIVE, SPEAKING_COLOR_INACTIVE);
			// Loud volume
			DrawMeterSegment(pe.Graphics, percent, SPEAKING_PERCENT, LOUD_PERCENT, LOUD_COLOR_ACTIVE, LOUD_COLOR_INACTIVE);
			// Clipping volume
			DrawMeterSegment(pe.Graphics, percent, LOUD_PERCENT, CLIPPING_PERCENT, CLIPPING_COLOR_ACTIVE, CLIPPING_COLOR_INACTIVE);

			//int num4 = base.Height - 2;
			//num4 = (int)((double)num4 * percent);
			//pe.Graphics.FillRectangle(foregroundBrush, 1, base.Height - 1 - num4, Width - 2, num4);
		}

		private void DrawMeterSegment(Graphics g, double meterPercent, double segmentStart, double segmentEnd, int active, int inactive)
		{
			Brush colorActive = new SolidBrush(Color.FromArgb(active));
			Brush colorInactive = new SolidBrush(Color.FromArgb(inactive));
			int top = (int)((1 - segmentEnd) * Height);
			if (meterPercent >= segmentEnd)
			{
				// Segment is completely filled
				int height = (int)((segmentEnd - segmentStart) * Height) + 1;
				g.FillRectangle(colorActive, 1, top, Width - 2, height);
			}
			else if (meterPercent <= segmentStart)
			{
				// Segment is completely empty
				int height = (int)((segmentEnd - segmentStart) * Height) + 1;
				g.FillRectangle(colorInactive, 1, top, Width - 2, height);
			}
			else
			{
				int inactiveHeight = (int)((segmentEnd - meterPercent) * Height) + 1;
				int activeTop = top + inactiveHeight;
				int activeHeight = (int)((meterPercent - segmentStart) * Height) + 1;
				g.FillRectangle(colorInactive, 1, top, Width - 2, inactiveHeight);
				g.FillRectangle(colorActive, 1, activeTop, Width - 2, activeHeight);
			}
		}
	}
}