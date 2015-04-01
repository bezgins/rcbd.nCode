using System;
using System.Runtime.InteropServices;
using System.Text;

namespace rcbd.nCode
{
    public static class Win32Helpers
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr RegisterEventSource(string server, string source);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DeregisterEventSource(IntPtr handle);

        public enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool LookupAccountName(
            [In, MarshalAs(UnmanagedType.LPTStr)] string systemName,
            [In, MarshalAs(UnmanagedType.LPTStr)] string accountName,
            IntPtr sid,
            ref int cbSid,
            StringBuilder referencedDomainName,
            ref int cbReferencedDomainName,
            out SID_NAME_USE use);

        [DllImport("advapi32.dll", SetLastError=true)]
        public static extern bool ReportEvent(
            IntPtr hHandle,
            short wType,
            ushort wCategory,
            uint dwEventID,
            IntPtr uSid,
            short wStrings,
            int dwDataSize,
            string[] lpStrings,
            byte[] bData);
    }
}
