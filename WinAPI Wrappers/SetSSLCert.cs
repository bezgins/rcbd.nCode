﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace rcbd.nCode
{
    /// <summary>
    /// Исключение компилятора
    /// </summary>
    public class BindCertificateException : Exception, ISerializable
    {
        public BindCertificateException()
            : base()
        {
        }

        public BindCertificateException(string message)
            : base(message)
        {
        }

        public BindCertificateException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected BindCertificateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    internal class SetSSLCert
    {
        #region DllImport

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpInitialize(
            HTTPAPI_VERSION Version,
            uint Flags,
            IntPtr pReserved);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpSetServiceConfiguration(
            IntPtr ServiceIntPtr,
            HTTP_SERVICE_CONFIG_ID ConfigId,
            IntPtr pConfigInformation,
            int ConfigInformationLength,
            IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        private static extern uint HttpDeleteServiceConfiguration(
            IntPtr ServiceIntPtr,
            HTTP_SERVICE_CONFIG_ID ConfigId,
            IntPtr pConfigInformation,
            int ConfigInformationLength,
            IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true)]
        public static extern uint HttpTerminate(
            uint Flags,
            IntPtr pReserved);

        private enum HTTP_SERVICE_CONFIG_ID
        {
            HttpServiceConfigIPListenList = 0,
            HttpServiceConfigSSLCertInfo,
            HttpServiceConfigUrlAclInfo,
            HttpServiceConfigMax
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HTTP_SERVICE_CONFIG_IP_LISTEN_PARAM
        {
            public ushort AddrLength;
            public IntPtr pAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_SERVICE_CONFIG_SSL_SET
        {
            public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
            public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HTTP_SERVICE_CONFIG_SSL_KEY
        {
            public IntPtr pIpPort;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct HTTP_SERVICE_CONFIG_SSL_PARAM
        {
            public int SslHashLength;
            public IntPtr pSslHash;
            public Guid AppId;
            [MarshalAs(UnmanagedType.LPWStr)] public string pSslCertStoreName;
            public uint DefaultCertCheckMode;
            public int DefaultRevocationFreshnessTime;
            public int DefaultRevocationUrlRetrievalTimeout;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDefaultSslCtlIdentifier;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDefaultSslCtlStoreName;
            public uint DefaultFlags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct HTTPAPI_VERSION
        {
            public ushort HttpApiMajorVersion;
            public ushort HttpApiMinorVersion;

            public HTTPAPI_VERSION(ushort majorVersion, ushort minorVersion)
            {
                HttpApiMajorVersion = majorVersion;
                HttpApiMinorVersion = minorVersion;
            }
        }

        #endregion

        #region Constants

        public const uint HTTP_INITIALIZE_CONFIG = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_USE_DS_MAPPER = 0x00000001;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT = 0x00000002;
        public const uint HTTP_SERVICE_CONFIG_SSL_FLAG_NO_RAW_FILTER = 0x00000004;
        private static int NOERROR = 0;
        private static int ERROR_ALREADY_EXISTS = 183;

        #endregion

        #region Public methods

        public static void BindCertificate(string ipAddress, int port, byte[] hash)
        {
            uint retVal = (uint) NOERROR; // NOERROR = 0

            HTTPAPI_VERSION httpApiVersion = new HTTPAPI_VERSION(1, 0);
            retVal = HttpInitialize(httpApiVersion, HTTP_INITIALIZE_CONFIG, IntPtr.Zero);

            if ((uint) NOERROR == retVal)
            {
                try
                {
                    HTTP_SERVICE_CONFIG_SSL_SET configSslSet = new HTTP_SERVICE_CONFIG_SSL_SET();
                    HTTP_SERVICE_CONFIG_SSL_KEY httpServiceConfigSslKey = new HTTP_SERVICE_CONFIG_SSL_KEY();
                    HTTP_SERVICE_CONFIG_SSL_PARAM configSslParam = new HTTP_SERVICE_CONFIG_SSL_PARAM();

                    IPAddress ip = IPAddress.Parse(ipAddress);

                    IPEndPoint ipEndPoint = new IPEndPoint(ip, port);
                    // serialize the endpoint to a SocketAddress and create an array to hold the values.  Pin the array.
                    SocketAddress socketAddress = ipEndPoint.Serialize();
                    byte[] socketBytes = new byte[socketAddress.Size];
                    GCHandle handleSocketAddress = GCHandle.Alloc(socketBytes, GCHandleType.Pinned);
                    // Should copy the first 16 bytes (the SocketAddress has a 32 byte buffer, the size will only be 16,
                    //which is what the SOCKADDR accepts
                    for (int i = 0; i < socketAddress.Size; ++i)
                    {
                        socketBytes[i] = socketAddress[i];
                    }

                    httpServiceConfigSslKey.pIpPort = handleSocketAddress.AddrOfPinnedObject();

                    GCHandle handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);
                    var guidAttribute =
                        (GuidAttribute)
                            Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (GuidAttribute), true)[0];

                    configSslParam.AppId = Guid.Parse(guidAttribute.Value);
                    configSslParam.DefaultCertCheckMode = 0;
                    configSslParam.DefaultFlags = HTTP_SERVICE_CONFIG_SSL_FLAG_NEGOTIATE_CLIENT_CERT;
                    configSslParam.DefaultRevocationFreshnessTime = 0;
                    configSslParam.DefaultRevocationUrlRetrievalTimeout = 0;
                    configSslParam.pSslCertStoreName = StoreName.My.ToString();
                    configSslParam.pSslHash = handleHash.AddrOfPinnedObject();
                    configSslParam.SslHashLength = hash.Length;
                    configSslSet.ParamDesc = configSslParam;
                    configSslSet.KeyDesc = httpServiceConfigSslKey;

                    IntPtr pInputConfigInfo =
                        Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof (HTTP_SERVICE_CONFIG_SSL_SET)));
                    Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

                    retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                        HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                        pInputConfigInfo,
                        Marshal.SizeOf(configSslSet),
                        IntPtr.Zero);

                    if ((uint) ERROR_ALREADY_EXISTS == retVal) // ERROR_ALREADY_EXISTS = 183
                    {
                        retVal = HttpDeleteServiceConfiguration(IntPtr.Zero,
                            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                            pInputConfigInfo,
                            Marshal.SizeOf(configSslSet),
                            IntPtr.Zero);

                        if ((uint) NOERROR == retVal)
                        {
                            retVal = HttpSetServiceConfiguration(IntPtr.Zero,
                                HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                pInputConfigInfo,
                                Marshal.SizeOf(configSslSet),
                                IntPtr.Zero);

                            if ((uint)NOERROR != retVal)
                            {
                                ThrowError(retVal, "HttpSetServiceConfiguration2");
                            }
                        }
                        else
                        {
                            ThrowError(retVal, "HttpDeleteServiceConfiguration");
                        }
                    }
                    else if ((uint) NOERROR != retVal)
                    {
                        ThrowError(retVal, "HttpSetServiceConfiguration");
                    }

                    Marshal.FreeCoTaskMem(pInputConfigInfo);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    HttpTerminate(HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
                }
            }
            else
            {
                ThrowError(retVal, "HttpInitialize");
            }

            if ((uint) NOERROR != retVal)
            {
                throw new Win32Exception(Convert.ToInt32(retVal));
            }
        }

        private static void ThrowError(uint retVal, string funcName)
        {
            string errorMessage = new Win32Exception(Convert.ToInt32(retVal)).Message;
            throw new BindCertificateException(
                string.Format(
                    "{0} failed with code 0x{1:x} ({2})",
                    funcName,
                    Convert.ToInt32(retVal),
                    errorMessage));
        }

        #endregion
    }
}
