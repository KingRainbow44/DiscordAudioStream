﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using DLLs;

namespace DiscordAudioStream.ScreenCapture;

public class ProcessHandleList
{
    private readonly List<IntPtr> handles;
    private readonly List<string> processNames;

    // Cannot instantiate directly, must call ProcessHandleList.Refresh()
    private ProcessHandleList(Dictionary<IntPtr, string> processes)
    {
        handles = processes.Keys.ToList();
        processNames = processes.Values.ToList();
    }

    public static ProcessHandleList Refresh()
    {
        IntPtr shellWindow = User32.GetShellWindow();
        Dictionary<IntPtr, string> windows = new();

        User32.EnumWindows(
            (hWnd, lParam) =>
            {
                // Called for each top-level window

                // Ignore shell
                if (hWnd == shellWindow)
                {
                    return true;
                }

                // Ignore this window
                if (hWnd == Process.GetCurrentProcess().MainWindowHandle)
                {
                    return true;
                }

                // Ignore windows without WS_VISIBLE
                if (!User32.IsWindowVisible(hWnd))
                {
                    return true;
                }

                // Ignore windows with "" as title
                int windowTextLength = User32.GetWindowTextLength(hWnd);
                if (User32.GetWindowTextLength(hWnd) == 0)
                {
                    return true;
                }

                // Ignore suspended Windows Store apps
                try
                {
                    if (Dwmapi.GetBoolAttr(hWnd, Dwmapi.DwmWindowAttribute.CLOAKED))
                    {
                        return true;
                    }
                }
                catch (ExternalException)
                {
                    Logger.Log(
                        $"Cannot get property CLOAKED of window {hWnd}. This is normal on Windows 7."
                    );
                }

                StringBuilder builder = new(windowTextLength);
                _ = User32.GetWindowText(hWnd, builder, windowTextLength + 1);
                string name = builder.ToString();

                if (name == AreaForm.AREA_FORM_TITLE)
                {
                    return true;
                }

                windows[hWnd] = name;
                return true;
            },
            IntPtr.Zero
        );

        return new ProcessHandleList(windows);
    }

    public ICollection<string> Names => processNames;

    public IntPtr this[int index]
    {
        get
        {
            if (handles == null)
            {
                throw new InvalidOperationException("Call RefreshHandles() before attempting to get a handle");
            }
            if (index < 0 || index >= handles.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return handles[index];
        }
    }

    public int IndexOf(IntPtr handle)
    {
        return handles.IndexOf(handle);
    }
}
