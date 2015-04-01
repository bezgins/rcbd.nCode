using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace rcbd.nCode
{
    /// <summary>
    /// Safe Event Source handle
    /// </summary>
    public class SafeEventSource : IDisposable
    {
        /// <summary>
        /// Event source handle
        /// </summary>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Constructor. Registers event source.
        /// </summary>
        /// <param name="machine">Machine name</param>
        /// <param name="source">Source name</param>
        public SafeEventSource(string machine, string source)
        {
            Handle = Win32Helpers.RegisterEventSource(machine, source);

            if (Handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw (new Exception(new Win32Exception(error).Message));
            }
        }

        public void ReportEvent(
            string message,
            string user     = "",
            EventType type  = EventType.Information,
            ushort category = 1,
            uint eventID    = 1)
        {
            if(Handle == IntPtr.Zero)
                throw new InvalidOperationException("Source handle is NULL");

            using (var _sid = new SafeSIDHandle(user))
            {
                bool success =
                    Win32Helpers.ReportEvent(
                        Handle,
                        (short) type, category, eventID,
                        _sid.Handle,
                        1, 0,
                        new string[] {message}, null);

                if (!success)
                {
                    int _error = Marshal.GetLastWin32Error();
                    throw (new Exception(new Win32Exception(_error).Message));
                }
            }
        }

        public void Dispose()
        {
            if(Handle != IntPtr.Zero)
                Win32Helpers.DeregisterEventSource(Handle);
        }
    }
}
