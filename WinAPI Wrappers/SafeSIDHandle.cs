using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace rcbd.nCode
{
    /// <summary>
    /// Safe SID handle
    /// </summary>
    public class SafeSIDHandle : IDisposable
    {
        public SafeSIDHandle()
        {
            Handle = IntPtr.Zero;
        }

        /// <summary>
        /// Initialize SID with account name
        /// </summary>
        /// <param name="name">Account name</param>
        public SafeSIDHandle(string name) : this()
        {
            if(String.IsNullOrEmpty(name))
                return;

            int _sidLength    = 0; //size of SID buffer.
            int _domainLength = 0; //size of domain name buffer.
            Win32Helpers.SID_NAME_USE _use; //type of object.
            StringBuilder _domain = new StringBuilder(); //stringBuilder for domain name.
            int _error = 0;

            //first call of the function only returns the sizes of buffers (SDI, domain name)
            Win32Helpers.LookupAccountName(
                null, name,
                Handle, ref _sidLength,
                _domain, ref _domainLength,
                out _use);

            _error = Marshal.GetLastWin32Error();

            if (_error != 122) //error 122 (The data area passed to a system call is too small) - normal behaviour.
            {
                throw (new Exception(new Win32Exception(_error).Message));
            }
            else
            {
                _domain  = new StringBuilder(_domainLength); //allocates memory for domain name
                Length   = _sidLength; //allocates memory for SID
                bool _rc = Win32Helpers.LookupAccountName(
                    null, name,
                    Handle, ref _sidLength,
                    _domain, ref _domainLength,
                    out _use);

                if (!_rc || _use != Win32Helpers.SID_NAME_USE.SidTypeUser)
                {
                    FreeSID();
                }
            }
        }

        /// <summary>
        /// Allocate SID memory
        /// </summary>
        /// <param name="length">SID length</param>
        private void AllocSID(int length)
        {
            if (length > 0)
                Handle = Marshal.AllocHGlobal(length);
        }

        public void Dispose()
        {
            FreeSID();
        }

        /// <summary>
        /// Free SID memory
        /// </summary>
        private void FreeSID()
        {
            if (Handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Handle);
                Handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// SID length.
        /// Changing that frees and allocates new SID memory
        /// </summary>
        public int Length
        {
            set
            {
                FreeSID();
                AllocSID(value);
            }
        }

        /// <summary>
        /// SID handle value
        /// </summary>
        public IntPtr Handle { get; private set; }
    }
}
