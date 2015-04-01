using System;
using System.Runtime.InteropServices;
using System.Text;

namespace rcbd.nCode
{
    /// <summary>
    /// Windows management functions
    /// </summary>
    static class WinAPIHelper
    {
        /// <summary>
        /// Windows enumeration callback
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="lparam">lParam from EnumWindows call</param>
        /// <returns>false if stop enumeration</returns>
        public delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        /// <summary>
        /// Enumerate top-level windows
        /// </summary>
        /// <param name="lpEnumFunc">Enumeration callback</param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(WindowEnumProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Get window class name
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="lpClassName">name buffer</param>
        /// <param name="nMaxCount">buffer length</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// Get window caption name length
        /// </summary>
        /// <param name="hWnd">window handle</param>
        /// <returns>window text length</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// get window text
        /// </summary>
        /// <param name="hWnd">window handle</param>
        /// <param name="lpString">name buffer</param>
        /// <param name="nMaxCount">buffer length</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Get parent window handle
        /// </summary>
        /// <param name="hWnd">child window handle</param>
        /// <returns>parent window handle</returns>
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// Get handle to the foreground window.
        /// </summary>
        /// <returns>foreground window handle</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Dialog window class name
        /// </summary>
        public const string DialogClassName = "#32770";
    }
}
