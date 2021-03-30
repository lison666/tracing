using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Lwaay.Tracing.Model
{
    /// <summary>
    /// Utils  Copy To Jaeger
    /// </summary>
    public static class Utils
    {
        private static readonly Random Random;

        static Utils()
        {
            // Really initialize the random number generator so that multiple processes
            // starting at the same time do not duplicate IDs.
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                rngCsp.GetBytes(bytes);
                Random = new Random(BitConverter.ToInt32(bytes, 0));
            }
        }

        public static int IpToInt(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                throw new ArgumentNullException(nameof(ipAddress));

            if (string.Equals(ipAddress, "localhost", StringComparison.Ordinal))
            {
                return (127 << 24) | 1;
            }

            return IpToInt(IPAddress.Parse(ipAddress));
        }

        public static int IpToInt(IPAddress ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException(nameof(ipAddress));

            byte[] octets = ipAddress.GetAddressBytes();
            if (octets.Length != 4)
                throw new FormatException("Not four octets");

            int intIp = 0;
            foreach (byte octet in octets)
            {
                intIp = (intIp << 8) | (octet & 0xFF);
            }
            return intIp;
        }

        public static long UniqueId()
        {
            long value = 0;
            while (value == 0)
            {
                var bytes = new byte[8];
                lock (Random)
                {
                    Random.NextBytes(bytes);
                }
                value = BitConverter.ToInt64(bytes, 0);
            }
            return value;
        }

        public static byte[] LongToNetworkBytes(long data)
        {
            var bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static string GetParentSpanId(this Span span)
        {
            if (span == null || span.References == null || span.References.Count <= 0)
                return string.Empty;
            return span.References.FirstOrDefault(f => f.RefType == SpanRefType.ChildOf)?.SpanId ?? string.Empty;
        }

        public static string GetServiceName(this Span span)
        {
            if (span == null || span.Process == null)
                return String.Empty;
            return span.Process.ServiceName ?? string.Empty;
        }
    }
}
