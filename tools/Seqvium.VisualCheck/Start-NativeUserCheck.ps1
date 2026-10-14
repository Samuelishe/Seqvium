# SPDX-License-Identifier: Apache-2.0
param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Arguments,
    [Parameter(Mandatory=$true)][string]$WorkingDirectory
)
$ErrorActionPreference = 'Stop'
# Native Explorer drag requires the same integrity level as the interactive shell. This launcher
# borrows that same user's primary shell token; it changes no user/system security settings.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
public static class SeqviumNativeUserLaunch {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    struct Startup { public uint Size; public string Reserved,Desktop,Title; public uint X,Y,W,H,CX,CY,Fill,Flags; public ushort Show,ReservedSize; public IntPtr ReservedBytes,Input,Output,Error; }
    [StructLayout(LayoutKind.Sequential)] struct Info { public IntPtr Process,Thread; public uint Pid,Tid; }
    [DllImport("user32.dll")] static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("advapi32.dll",SetLastError=true)] static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
    [DllImport("advapi32.dll",SetLastError=true)] static extern bool DuplicateTokenEx(IntPtr token,uint access,IntPtr attributes,int impersonation,int type,out IntPtr duplicate);
    [DllImport("advapi32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcessWithTokenW(IntPtr token,uint logon,string application,StringBuilder command,uint flags,IntPtr environment,string directory,ref Startup startup,out Info info);
    static void Check(bool success) { if(!success) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
    public static uint Start(string executable,string arguments,string directory) {
        IntPtr process=IntPtr.Zero,token=IntPtr.Zero,duplicate=IntPtr.Zero;
        try {
            IntPtr shell=GetShellWindow(); if(shell==IntPtr.Zero) throw new InvalidOperationException("No interactive Explorer shell.");
            GetWindowThreadProcessId(shell,out uint pid); process=OpenProcess(0x1000,false,pid); Check(process!=IntPtr.Zero);
            Check(OpenProcessToken(process,0x000B,out token));
            using(var identity=new WindowsIdentity(token)) if(identity.User!=WindowsIdentity.GetCurrent().User) throw new InvalidOperationException("Shell belongs to a different user.");
            Check(DuplicateTokenEx(token,0x02000000,IntPtr.Zero,2,1,out duplicate));
            var startup=new Startup { Size=(uint)Marshal.SizeOf(typeof(Startup)), Desktop="winsta0\\default" };
            Info info; Check(CreateProcessWithTokenW(duplicate,0,executable,new StringBuilder("\""+executable+"\" "+arguments),0x08000000,IntPtr.Zero,directory,ref startup,out info));
            CloseHandle(info.Thread); CloseHandle(info.Process); return info.Pid;
        } finally { if(duplicate!=IntPtr.Zero) CloseHandle(duplicate); if(token!=IntPtr.Zero) CloseHandle(token); if(process!=IntPtr.Zero) CloseHandle(process); }
    }
}
'@
$nativeExecutable = (Resolve-Path -LiteralPath $Executable).Path
$nativeDirectory = (Resolve-Path -LiteralPath $WorkingDirectory).Path
[SeqviumNativeUserLaunch]::Start($nativeExecutable, $Arguments, $nativeDirectory)
