using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace rcbd.nCode
{
    public static class PrintingHelper
    {
        // Windows error codes
        private const int ERROR_FILE_NOT_FOUND = 2;
        /// <summary>
        /// Buffer is too small. Need to resize it.
        /// </summary>
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

#region Winspool.drv functions

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetDefaultPrinter(StringBuilder pszBuffer, ref int pcchBuffer);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern int ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFO di);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

#endregion

        /// <summary>
        /// Get default printer name
        /// </summary>
        /// <returns>Default printer name</returns>
        public static String GetDefaultPrinter()
        {
            int bufferLength = 0;

            // Get required buffer length
            if (GetDefaultPrinter(null, ref bufferLength))
            {
                return null;
            }

            var lastError = Marshal.GetLastWin32Error();

            if (lastError == ERROR_INSUFFICIENT_BUFFER)
            {
                var buffer = new StringBuilder(bufferLength);

                if (GetDefaultPrinter(buffer, ref bufferLength))
                {
                    return buffer.ToString();
                }
                else
                {
                    lastError = Marshal.GetLastWin32Error();
                }
            }

            //No default printer
            if (lastError == ERROR_FILE_NOT_FOUND)
            {
                return null;
            }

            // Something definitely went wrong.
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Send ANSI text to printer
        /// </summary>
        /// <param name="printerName">Printer name</param>
        /// <param name="text">Text to send</param>
        public static void SendAnsiTextToPrinter(string printerName, string text)
        {
            int lastError   = 0;
            bool success    = false;
            bool hasError   = false;

            // Prepare data to send
            var ansibytes = text.ToCharArray().Select(x => Convert.ToByte(Convert.ToInt32((char) x) % 0x100)).ToArray();
            var count     = ansibytes.Length;
            var bytes     = Marshal.AllocCoTaskMem(count);
            Marshal.Copy(ansibytes, 0, bytes, count);

            var hPrinter = new IntPtr(0);
            var docInfo  = new DOCINFO();

            // Document information.
            docInfo.pDocName  = "Cash drawer opening";
            docInfo.pDataType = "RAW";

            if (OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, docInfo))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        int written = 0;

                        success = WritePrinter(hPrinter, bytes, count, out written);

                        EndPagePrinter(hPrinter);
                    }
                    else
                    {
                        lastError = Marshal.GetLastWin32Error();
                        hasError = true;
                    }

                    EndDocPrinter(hPrinter);
                }
                else
                {
                    lastError = Marshal.GetLastWin32Error();
                    hasError = true;
                }

                ClosePrinter(hPrinter);
            }

            if (!success && !hasError)
            {
                lastError = Marshal.GetLastWin32Error();
            }

            Marshal.FreeCoTaskMem(bytes);

            // Something definitely went wrong.
            if (!success && lastError != 0)
            {
                throw new Win32Exception(lastError);
            }
            else if(!success)
            {
                throw new ApplicationException("Something terrible happened. Have no lastError, but not succeded");
            }
        }
    }
}
