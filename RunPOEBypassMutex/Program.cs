using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

// ReSharper disable PossibleNullReferenceException

namespace RunPOEBypassMutex
{
    class Program
    {
        static readonly string[] MutexObjectNames =
        {
            "PathOfExileSingleInstance",
            "Global\\PoERunMutexA",
            "Global\\PoERunMutexB"
        };

        static Process _process;
        static readonly Random Random = new();

        static void Main(string[] args)
        {
            var path = args[0];

            if (!File.Exists(path) || !path.EndsWith(".exe"))
            {
                Console.WriteLine("{0} is not executable.", path);
                Console.ReadLine();
                return;
            }

            try
            {
                _process = Process.Start(
                    new ProcessStartInfo
                    {
                        WorkingDirectory = Path.GetDirectoryName(path), 
                        FileName = path, 
                        Arguments = "--nopatch"
                    });

                WaitForProcess();

                var mainModule = _process.MainModule;
                var handle = _process.Handle;

                Threads.Suspend(_process);
                Thread.Sleep(500);

                int bytesRead = 0;
                var size = mainModule.ModuleMemorySize;
                var buffer = new byte[size];

                try
                {
                    if (!Native.ReadProcessMemory(handle, mainModule.BaseAddress, buffer, size, ref bytesRead))
                    {
                        ReportError("[-] Unable to read the entire process memory.");
                        return;
                    }
                }
                catch (Exception e)
                {
                    ReportError("[-] [Exception] Unable to read the entire process memory.");
                    return;
                }

                Console.WriteLine("-\n[+] Changing mutex names...\n-");

                // Set new mutex names.
                var addresses = new IntPtr[MutexObjectNames.Length];

                for (int i = 0; i < MutexObjectNames.Length; i++)
                {
                    var name = MutexObjectNames[i];

                    if (!ChangeMutexName(name, buffer, out var baseAddress))
                        return;

                    addresses[i] = baseAddress;
                }

                // Run poe.
                Threads.Resume(_process);
                Thread.Sleep(500);

                Console.WriteLine("-\n[+] Restoring mutex names...\n-");

                // Restore original mutex names.
                for (int i = 0; i < MutexObjectNames.Length; i++)
                {
                    var name = MutexObjectNames[i];
                    var address = addresses[i];

                    if (!ChangeMutexName(address, name))
                        return;
                }
            }
            catch (Exception e)
            {
                // Prevent .exe hangs.
                ReportError("[-] " + e);
            }
            finally
            {
                _process?.Dispose();
            }
        }

        static bool ChangeMutexName(string name, byte[] processMemory, out IntPtr baseAddress)
        {
            baseAddress = IntPtr.Zero;

            var original = Encoding.Unicode.GetBytes(name);
            var originalSize = original.Length;

            var offset = BoyerMooreHorspool.IndexOf(processMemory, original);
            if (offset == -1)
            {
                Console.WriteLine("[-] Offset for '{0}' is not found.", name);
                return false;
            }

            baseAddress = _process.MainModule.BaseAddress + offset;
            Console.WriteLine(
                "[+] BaseAddress {0:X} found for mutex with name {1} (name size: {2})",
                baseAddress.ToInt64(),
                name,
                originalSize);

            var randomNumberString = Random.Next(10000, 99999).ToString();
            var newName = name.Replace("Exile", randomNumberString).Replace("Mutex", randomNumberString);

            return ChangeMutexName(baseAddress, newName);
        }

        static bool ChangeMutexName(IntPtr baseAddress, string newName)
        {
            var newBytes = Encoding.Unicode.GetBytes(newName);
            var size = newBytes.Length;

            Console.WriteLine("[+] New mutex name: {0} (size: {1})", newName, newBytes.Length);

            if (!ChangeProtection(baseAddress, size, 0x40, out int oldProtectionFlag))
            {
                ReportError("[-] Unable to change memory protection flag 1.");
                return false;
            }

            var bytesRead = 0;
            if (!Native.WriteProcessMemory(_process.Handle, baseAddress, newBytes, size, ref bytesRead))
            {
                ReportError("[-] Unable to write a new mutex name.");
                return false;
            }

            if (!ChangeProtection(baseAddress, size, oldProtectionFlag, out _))
            {
                ReportError("[-] Unable to change memory protection flag 2.");
                return false;
            }

            return true;
        }

        static bool ChangeProtection(IntPtr baseAddress, int size, int flag, out int oldProtectionFlag)
        {
            return Native.VirtualProtectEx(_process.Handle, baseAddress, new IntPtr(size), flag, out oldProtectionFlag);
        }

        static void ReportError(string error)
        {
            _process.Kill();
            Console.WriteLine("[-] " + error);
            Console.ReadLine();
        }

        static void WaitForProcess()
        {
            while (true)
            {
                try
                {
                    var _ = _process.StartTime;
                    break;
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}