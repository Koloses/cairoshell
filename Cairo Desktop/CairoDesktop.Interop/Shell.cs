﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static CairoDesktop.Interop.NativeMethods;

namespace CairoDesktop.Interop
{
    public partial class Shell
    {
        /* ******************************************
         * DLL Imports for getting special folders 
         * that are not supported by .Net Framework
         * *************************************** */

        // Uses some code from https://gist.github.com/madd0/1433330

        private const int MAX_PATH = 260;


        public static IntPtr GetIconByFilename(string fileName, int size)
        {
            /*switch (size)
            {
                case 0:
                    return GetIcon(fileName, SHGFI.LargeIcon);
                case 1:
                    return GetIcon(fileName, SHGFI.SmallIcon);
                case 2:
                    return GetIcon_New(fileName, size);
                default:
                    return GetIcon(fileName, SHGFI.LargeIcon);
            }*/
            return GetIcon_New(fileName, size);
        }

        /*private static IntPtr GetIcon(string filename, SHGFI flags)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hIconInfo;

                if (!filename.StartsWith("\\") && (File.GetAttributes(filename) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.SysIconIndex | flags));
                }
                else
                {
                    hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.UseFileAttributes | SHGFI.SysIconIndex | flags));
                }

                IntPtr hIcon = ImageList_GetIcon(hIconInfo, shinfo.iIcon, (int)0x00000001);

                return hIcon;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }*/
        
        private static IntPtr GetIcon_New(string filename, int size)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                IntPtr hIconInfo;

                if (!filename.StartsWith("\\") && (File.GetAttributes(filename) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_DIRECTORY, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.SysIconIndex));
                }
                else
                {
                    hIconInfo = SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.UseFileAttributes | SHGFI.SysIconIndex));
                }

                var iconIndex = shinfo.iIcon;

                // Get the System IImageList object from the Shell:
                Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

                IImageList iml;
                var hres = SHGetImageList(size, ref iidImageList, out iml);

                IntPtr hIcon = IntPtr.Zero;
                int ILD_TRANSPARENT = 1;
                hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
                Marshal.ReleaseComObject(iml);

                return hIcon;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public static string GetDisplayName(string filename)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(filename, FILE_ATTRIBUTE_NORMAL, ref shinfo, (uint)Marshal.SizeOf(shinfo), (uint)(SHGFI.DisplayName));

            return shinfo.szDisplayName;
        }

        public static string UsersStartMenuPath
        {
            get {
                return GetSpecialFolderPath((int)CSIDL.CSIDL_STARTMENU);
            }
        }

        public static string AllUsersStartMenuPath
        {
            get {
                return GetSpecialFolderPath((int)CSIDL.CSIDL_COMMON_STARTMENU);
            }
        }

        public static string GetSpecialFolderPath(int FOLDER)
        {
            StringBuilder sbPath = new StringBuilder(MAX_PATH);
            SHGetFolderPath(IntPtr.Zero, FOLDER, IntPtr.Zero, 0, sbPath);
            return sbPath.ToString();
        }

        public static bool StartProcess(string filename)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                if (filename.StartsWith("appx:"))
                    Process.Start("LaunchWinApp.exe", "shell:appsFolder\\" + filename.Substring(5));
                else if (filename.Contains("://"))
                    Process.Start("explorer.exe", filename);
                else
                    Process.Start(filename);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string filename, string args)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                Process.Start(filename, args);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool StartProcess(string filename, string args, string verb)
        {
            try
            {
                if (!Environment.Is64BitProcess)
                {
                    filename.Replace("system32", "sysnative");
                }

                Process proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = filename;
                proc.StartInfo.Verb = verb;
                try
                {
                    proc.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Error running the {0} verb on {1}. ({2})", verb, filename, ex.Message));
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool Exists(string filename)
        {
            return !filename.StartsWith("\\\\") && (File.Exists(filename) || Directory.Exists(filename));
        }

        public static void ShowWindowBottomMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)HWND_BOTTOMMOST,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE/* | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static void ShowWindowTopMost(IntPtr handle)
        {
            SetWindowPos(
                handle,
                (IntPtr)HWND_TOPMOST,
                0,
                0,
                0,
                0,
                (int)SetWindowPosFlags.SWP_NOSIZE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOACTIVATE/* | SWP_NOZORDER | SWP_NOOWNERZORDER*/);
        }

        public static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }

        /// <summary>
        /// Calls the Windows OpenWith dialog (shell32.dll) to open the file specified.
        /// </summary>
        /// <param name="fileName">Path to the file to open</param>
        public static void ShowOpenWithDialog(string fileName)
        {
            Process owProc = new Process();
            owProc.StartInfo.UseShellExecute = true;
            owProc.StartInfo.FileName = Environment.GetEnvironmentVariable("WINDIR") + @"\system32\rundll32.exe";
            owProc.StartInfo.Arguments =
                @"C:\WINDOWS\system32\shell32.dll,OpenAs_RunDLL " + fileName;
            owProc.Start();
        }

        public static void ShowRunDialog()
        {
            SHRunFileDialog(IntPtr.Zero, IntPtr.Zero, null, Localization.DisplayString.sRun_Title, Localization.DisplayString.sRun_Info, RunFileDialogFlags.None);
        }

        public static void ShowWindowSwitcher()
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.wScan = 0;
            inputs[0].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[0].mkhi.ki.wVk = VK_LWIN;
            inputs[0].mkhi.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].mkhi.ki.wScan = 0;
            inputs[1].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[1].mkhi.ki.wVk = VK_TAB;
            inputs[1].mkhi.ki.dwFlags = 0;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].mkhi.ki.wScan = 0;
            inputs[2].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[2].mkhi.ki.wVk = VK_TAB;
            inputs[2].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].mkhi.ki.wScan = 0;
            inputs[3].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[3].mkhi.ki.wVk = VK_LWIN;
            inputs[3].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void ShowActionCenter()
        {
            INPUT[] inputs = new INPUT[4];

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].mkhi.ki.time = 0;
            inputs[0].mkhi.ki.wScan = 0;
            inputs[0].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[0].mkhi.ki.wVk = VK_LWIN;
            inputs[0].mkhi.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].mkhi.ki.wScan = 0;
            inputs[1].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[1].mkhi.ki.wVk = 0x41;
            inputs[1].mkhi.ki.dwFlags = 0;

            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].mkhi.ki.wScan = 0;
            inputs[2].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[2].mkhi.ki.wVk = 0x41;
            inputs[2].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            inputs[3].type = INPUT_KEYBOARD;
            inputs[3].mkhi.ki.wScan = 0;
            inputs[3].mkhi.ki.dwExtraInfo = GetMessageExtraInfo();
            inputs[3].mkhi.ki.wVk = VK_LWIN;
            inputs[3].mkhi.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        /// <summary>
        /// Send file to recycle bin
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
        public static bool SendToRecycleBin(string path, FileOperationFlags flags)
        {
            try
            {
                var fs = new SHFILEOPSTRUCT
                {
                    wFunc = FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                SHFileOperation(ref fs);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool SendToRecycleBin(string path)
        {
            return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
        }

        /// <summary>
        /// Send file silently to recycle bin.  Surpress dialog, surpress errors, delete if too large.
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool MoveToRecycleBin(string path)
        {
            return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT);

        }

        public static void ToggleDesktopIcons(bool enable)
        {
            var toggleDesktopCommand = new IntPtr(0x7402);
            //IntPtr hWnd = GetWindow(FindWindow("Progman", "Program Manager"), GetWindow_Cmd.GW_CHILD);
            IntPtr hWnd = FindWindowEx(FindWindow("Progman", "Program Manager"), IntPtr.Zero, "SHELLDLL_DefView", "");

            if (hWnd == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    StringBuilder cName = new StringBuilder(256);
                    GetClassName(hwnd, cName, cName.Capacity);
                    if (cName.ToString() == "WorkerW")
                    {
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero)
                        {
                            hWnd = child;
                            return true;
                        }
                    }
                    return true;
                }, 0);
            }

            if (IsDesktopVisible() != enable)
                SendMessageTimeout(hWnd, WM_COMMAND, toggleDesktopCommand, IntPtr.Zero, 2, 200, ref hWnd);
        }

        static bool IsDesktopVisible()
        {
            //IntPtr hWnd = GetWindow(GetWindow(FindWindow("Progman", "Program Manager"), GetWindow_Cmd.GW_CHILD), GetWindow_Cmd.GW_CHILD);
            IntPtr hWnd = GetWindow(FindWindowEx(FindWindow("Progman", "Program Manager"), IntPtr.Zero, "SHELLDLL_DefView", ""), GetWindow_Cmd.GW_CHILD);


            if (hWnd == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    StringBuilder cName = new StringBuilder(256);
                    GetClassName(hwnd, cName, cName.Capacity);
                    if (cName.ToString() == "WorkerW")
                    {
                        IntPtr child = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero)
                        {
                            hWnd = FindWindowEx(child, IntPtr.Zero, "SysListView32", "FolderView");
                            return true;
                        }
                    }
                    return true;
                }, 0);
            }

            WINDOWINFO info = new WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            GetWindowInfo(hWnd, ref info);
            return (info.dwStyle & 0x10000000) == 0x10000000;
        }

        /// <summary>
        /// Transforms device independent units (1/96 of an inch)
        /// to pixels
        /// </summary>
        /// <param name="unitX">a device independent unit value X</param>
        /// <param name="unitY">a device independent unit value Y</param>
        /// <param name="pixelX">returns the X value in pixels</param>
        /// <param name="pixelY">returns the Y value in pixels</param>
        public static void TransformToPixels(double unitX, double unitY, out int pixelX, out int pixelY)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                pixelX = (int)((g.DpiX / 96) * unitX);
                pixelY = (int)((g.DpiY / 96) * unitY);
            }
        }

        public static void TransformFromPixels(double unitX, double unitY, out int pixelX, out int pixelY)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                pixelX = (int)(unitX / (g.DpiX / 96));
                pixelY = (int)(unitY / (g.DpiY / 96));
            }
        }

        public static double GetDpiScale()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (g.DpiX / 96);
            }
        }

        private static int osVersionMajor = 0;
        private static int osVersionMinor = 0;

        private static void getOSVersion()
        {
            osVersionMajor = Environment.OSVersion.Version.Major;
            osVersionMinor = Environment.OSVersion.Version.Minor;
        }

        public static bool IsWindows2kOrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                    getOSVersion();

                return (osVersionMajor >= 5);
            }
        }

        public static bool IsWindowsVistaOrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                    getOSVersion();

                return (osVersionMajor >= 6);
            }
        }

        public static bool IsWindows8OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                    getOSVersion();

                return (osVersionMajor > 6 || (osVersionMajor == 6 && osVersionMinor >= 2));
            }
        }

        public static bool IsWindows10OrBetter
        {
            get
            {
                if (osVersionMajor == 0)
                    getOSVersion();

                return (osVersionMajor >= 10);
            }
        }
    }
}
