using System;
using System.Runtime.InteropServices;

namespace ReadKey
{
    public class WinApi
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll",SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess,IntPtr lpBaseAddress,[Out] byte[] lpBuffer,int dwSize,out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll",SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess,bool bInheritHandle,int processId);

        [DllImport("kernel32.dll",SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll",CallingConvention = CallingConvention.StdCall,SetLastError = true)]
        public static extern int EnumProcessModules(IntPtr hProcess,out uint lphModule,uint cb,out uint lpcbNeeded);

        [DllImport("kernel32.dll",CharSet = CharSet.Auto,SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("Advapi32.dll",CharSet = CharSet.Auto,SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle,uint DesiredAccesss,out IntPtr TokenHandle);

        [DllImport("advapi32.dll",CharSet = CharSet.Unicode,SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LookupPrivilegeValue(string lpSystemName,string lpName,[MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

        [DllImport("advapi32.dll",CharSet = CharSet.Unicode,SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle,[MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,[MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES NewState,uint BufferLength,IntPtr PreviousState,uint ReturnLength);
        
        [StructLayout(LayoutKind.Sequential,CharSet = CharSet.Unicode)]
        public struct LUID
        {
            public int LowPart;
            public uint HighPart;
        }

        [StructLayout(LayoutKind.Sequential,CharSet = CharSet.Unicode)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential,CharSet = CharSet.Unicode)]
        public struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privilege;
        }

        public static bool grantPrivilege(string name)
        {
            LUID privilegeId = new LUID();
            if(!LookupPrivilegeValue(null,name,ref privilegeId))
            {
                return false;
            }
            TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privilege = new LUID_AND_ATTRIBUTES
                {
                    Attributes = 0x00000002,
                    Luid = privilegeId
                }
            };
            IntPtr tokenHandle = IntPtr.Zero;
            return OpenProcessToken(GetCurrentProcess(),0x0020 | 0x0008,out tokenHandle) &&
                AdjustTokenPrivileges(tokenHandle,false,ref tokenPrivileges,1024,IntPtr.Zero,0) && 
                CloseHandle(tokenHandle);
        }
    }
}
