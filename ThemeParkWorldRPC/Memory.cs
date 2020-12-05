using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ThemeParkRPC
{
    // Class adapted from https://stackoverflow.com/a/50672487
    public class Memory
    {
        private static Process process;
        private static IntPtr processHandle;

        private static int bytesWritten;
        private static int bytesRead;

        public static bool Attach(string processName)
        {
            if (Process.GetProcessesByName(processName).Length > 0)
            {
                process = Process.GetProcessesByName(processName)[0];
                processHandle =
                    OpenProcess(Flags.PROCESS_VM_OPERATION | Flags.PROCESS_VM_READ | Flags.PROCESS_VM_WRITE,
                        false, process.Id);
                return true;
            }

            return false;
        }

        public static T ReadMemory<T>(int address) where T : struct
        {
            var ByteSize = Marshal.SizeOf(typeof(T));

            var buffer = new byte[ByteSize];

            ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesRead);

            return ByteArrayToStructure<T>(buffer);
        }

        public static byte[] ReadMemory(int offset, int size)
        {
            var buffer = new byte[size];

            ReadProcessMemory((int)processHandle, offset, buffer, size, ref bytesRead);

            return buffer;
        }

        public static string ReadMemoryString(int address, int size)
        {
            return Encoding.ASCII.GetString(ReadMemory(address, size));
        }

        public static float[] ReadMatrix<T>(int Adress, int MatrixSize) where T : struct
        {
            var ByteSize = Marshal.SizeOf(typeof(T));
            var buffer = new byte[ByteSize * MatrixSize];
            ReadProcessMemory((int)processHandle, Adress, buffer, buffer.Length, ref bytesRead);

            return ConvertToFloatArray(buffer);
        }

        public static int GetModuleAddress(string Name)
        {
            try
            {
                foreach (ProcessModule ProcMod in process.Modules)
                    if (Name == ProcMod.ModuleName)
                        return (int)ProcMod.BaseAddress;
            }
            catch
            {
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: Cannot find - " + Name + " | Check file extension.");
            Console.ResetColor();

            return -1;
        }

        #region P/Invoke
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        #endregion

        #region Other

        internal struct Flags
        {
            public const int PROCESS_VM_OPERATION = 0x0008;
            public const int PROCESS_VM_READ = 0x0010;
            public const int PROCESS_VM_WRITE = 0x0020;
        }

        #endregion

        #region Conversion

        public static float[] ConvertToFloatArray(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
                throw new ArgumentException();

            var floats = new float[bytes.Length / 4];

            for (var i = 0; i < floats.Length; i++)
                floats[i] = BitConverter.ToSingle(bytes, i * 4);

            return floats;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private static byte[] StructureToByteArray(object obj)
        {
            var length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            return array;
        }

        #endregion
    }
}
