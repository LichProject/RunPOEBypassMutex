using System;
using System.Diagnostics;

namespace RunPOEBypassMutex
{
    public static class Threads
    {
        public static void Suspend(Process process)
        {
            foreach (ProcessThread t in process.Threads)
            {
                IntPtr thread = Native.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) t.Id);
                if (thread == IntPtr.Zero)
                    continue;

                Native.SuspendThread(thread);
                Native.CloseHandle(thread);
            }
        }

        public static void Resume(Process process)
        {
            foreach (ProcessThread t in process.Threads)
            {
                IntPtr thread = Native.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint) t.Id);
                if (thread == IntPtr.Zero)
                {
                    continue;
                }

                int suspendCount;
                do
                {
                    suspendCount = Native.ResumeThread(thread);
                }
                while (suspendCount > 0);

                Native.CloseHandle(thread);
            }
        }
    }
}