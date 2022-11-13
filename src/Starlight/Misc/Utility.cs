﻿using IWshRuntimeLibrary;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starlight.Misc;

internal class Utility
{
    public static void CreateShortcut(string filePath, string target, string workingDir)
    {
        WshShell shell = new(); // This is a nasty library; I wish COM didn't exist.
        var shortcut = (IWshShortcut)shell.CreateShortcut(filePath);

        shortcut.TargetPath = target;
        shortcut.WorkingDirectory = workingDir;
        shortcut.Save();
    }

    public static bool TryGetCultureInfo(string name, out CultureInfo ci)
    {
        try
        {
            ci = new CultureInfo(name, false);
            return true;
        }
        catch (CultureNotFoundException)
        {
            ci = null;
            return false;
        }
    }

    public static void DisperseActions(IList<Action> actions, int maxConcurrency, CancellationToken token = default)
    {
        var curConcurrency = 0;
        var threadFinishedEvent = new AutoResetEvent(false);
        var threads = new List<Thread>();

        foreach (var action in actions)
        {
            var thread = new Thread(curThread =>
            {
                action();
                threadFinishedEvent.Set();
                threads.Remove((Thread)curThread);
            });

            threads.Add(thread);
            thread.Start(thread);
            curConcurrency++;

            while (curConcurrency >= maxConcurrency)
            {
                if (WaitHandle.WaitAny(new[] { token.WaitHandle, threadFinishedEvent }) == 0)
                {
                    foreach (var t in threads)
                    {
                        t.Abort();
                        curConcurrency--;
                    }
                    throw new TaskCanceledException();
                }
                curConcurrency--;
            }
        }

        while (curConcurrency > 0)
        {
            if (WaitHandle.WaitAny(new[] { token.WaitHandle, threadFinishedEvent }) == 0)
            {
                foreach (var t in threads)
                {
                    t.Abort();
                    curConcurrency--;
                }
                throw new TaskCanceledException();
            }
            curConcurrency--;
        }
    }

    public static async Task DisperseActionsAsync<T>(IList<T> list, Action<T> action, int maxConcurrency, CancellationToken token = default)
    {
        await AsyncHelpers.RunAsync(() => DisperseActions(list.Select(x => new Action(() => action(x))).ToList(), maxConcurrency, token));
    }

    public static EventWaitHandle GetNativeEventWaitHandle(int handle)
    {
        return new EventWaitHandle(false, EventResetMode.ManualReset)
        {
            SafeWaitHandle = new SafeWaitHandle((IntPtr)handle, false)
        };
    }
}