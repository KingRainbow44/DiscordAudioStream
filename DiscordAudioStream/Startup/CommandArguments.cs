﻿using System;
using System.Drawing;
using Mono.Options;

namespace DiscordAudioStream
{
	internal class CommandArguments
	{
		private Point? position = null;
		private bool startStream = false;
		
		public CommandArguments(string[] args)
		{
			bool showHelp = false;

			OptionSet options = new OptionSet()
			{
				{
					"position=",
					"Move the window to the given position coordinates. Format: 'position=x,y'\n"
						+ "Example: --position=0,0 moves the window to the top-left edge of the main screen.",
					v => position = ParsePoint(v)
				},
				{
					"start",
					"Start streaming immediately, using the previous settings. Ignores possible warnings.",
					v => startStream = v != null
				},
				{
					"h|help", 
					"Show this message and exit",
					v => showHelp = v != null
				},
			};

			try
			{
				options.Parse(args);
			}
			catch (OptionException e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `DiscordAudioStream.exe --help' for more information.");
				ExitImmediately = true;
				return;
			}
			
			if (showHelp)
			{
				PrintHelp(options);
				ExitImmediately = true;
			}
		}

		public bool ExitImmediately { get; private set; } = false;
		

		public void ProcessArgs(MainController controller)
		{
			if (startStream) controller.StartStream(true);
			
			if (position.HasValue)
			{
				controller.MoveWindow(position.Value);
			}
		}


		private void PrintHelp(OptionSet options)
		{
			Console.WriteLine("Usage: DiscordAudioStream.exe [options]");
			Console.WriteLine();
			Console.WriteLine("Options:");
			options.WriteOptionDescriptions(Console.Out);
			Console.WriteLine("All options can be used in UNIX or Windows styles (-option, --option or /option)");
		}

		static private Point ParsePoint(string str)
		{
			string[] parts = str.Split(',');
			if (parts.Length != 2) throw new FormatException("Invalid point format. Expected \"X,Y\".");
			if (!int.TryParse(parts[0], out int x)) throw new FormatException("Invalid X coordinate.");
			if (!int.TryParse(parts[1], out int y)) throw new FormatException("Invalid Y coordinate.");

			return new Point(x, y);
		}
	}
}