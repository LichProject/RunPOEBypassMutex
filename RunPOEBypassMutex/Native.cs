using System;
using System.Runtime.InteropServices;

namespace RunPOEBypassMutex
{
    public static class Native
    {
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess,
                                                     IntPtr lpBaseAddress,
                                                     byte[] lpBuffer,
                                                     int nSize,
                                                     ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
                                                    IntPtr lpBaseAddress,
                                                    byte[] lpBuffer,
                                                    int nSize,
                                                    ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        public static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(IntPtr hProcess,
                                                   IntPtr lpAddress,
                                                   IntPtr dwSize,
                                                   int flNewProtect,
                                                   out int lpflOldProtect);
    }
}