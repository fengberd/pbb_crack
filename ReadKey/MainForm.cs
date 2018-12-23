using System;
using System.Linq;
using System.Management;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace ReadKey
{
    public partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
            if(!WinApi.grantPrivilege("SeDebugPrivilege"))
            {
                MessageBox.Show("无法提权到SeDebugPrivilege","错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
                Environment.Exit(1);
            }
        }

        private void log(string tag,string text)
        {
            textBox1.AppendText("[" + tag + "]" + text + Environment.NewLine);
        }

        private void button2_Click(object sender,EventArgs e)
        {
            textBox1.Text = "";
        }

        private void button3_Click(object sender,EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        private void button1_Click(object sender,EventArgs e)
        {
            foreach(var p in Process.GetProcesses())
            {
                string name;
                try
                {
                    name = p.MainModule.FileName.ToLower();
                }
                catch
                {
                    continue;
                }
                if(name.EndsWith("pycplayer.exe"))
                {
                    IntPtr process = WinApi.OpenProcess(WinApi.ProcessAccessFlags.VirtualMemoryRead | WinApi.ProcessAccessFlags.QueryInformation,false,p.Id);
                    if(process == IntPtr.Zero)
                    {
                        log("ERROR/" + p.Id,"OpenProcess failed(" + Marshal.GetLastWin32Error() + ").");
                        continue;
                    }
                    if(WinApi.EnumProcessModules(process,out uint module,8,out uint cbNeeded) == 0)
                    {
                        log("ERROR/" + p.Id,"EnumProcessModules failed(" + Marshal.GetLastWin32Error() + ").");
                        continue;
                    }
                    byte[] vBuffer = new byte[16];
                    IntPtr vBytesAddress = Marshal.UnsafeAddrOfPinnedArrayElement(vBuffer,0);
                    WinApi.ReadProcessMemory(process,new IntPtr(module + 0x1152398),vBuffer,vBuffer.Length,out int count);
                    string tag = "TAG_NOT_EXISTS";
                    foreach(var c in GetCommandLine(p).Split('"'))
                    {
                        if(c.EndsWith(".pbb",StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                tag = Path.GetFileNameWithoutExtension(c);
                                break;
                            }
                            catch { }
                        }
                    }
                    log("INFO"," Key from " + p.Id + "(" + tag + ")  " + BitConverter.ToString(vBuffer).Replace("-",string.Empty));
                }
            }

        }

        private static string GetCommandLine(Process process)
        {
            using(ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using(ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }

        }
    }
}
