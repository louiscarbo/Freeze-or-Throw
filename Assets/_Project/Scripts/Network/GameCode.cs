using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace FreezeOrThrow.Networking
{
    public static class GameCode
    {
        public const int Length = 5;
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        const string FallbackAddress = "127.0.0.1";
        const int BitsPerSymbol = 5;
        const int SymbolMask = (1 << BitsPerSymbol) - 1;
        const int LowerOctetCount = 3;
        const int AddressBitCount = LowerOctetCount * 8;

        public static string LocalAddress()
        {
            string publicFallback = null;
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up
                    || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation addressInformation in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    IPAddress address = addressInformation.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    byte[] octets = address.GetAddressBytes();
                    if (IsPrivate(octets))
                    {
                        return address.ToString();
                    }

                    if (publicFallback == null && !IsLinkLocal(octets))
                    {
                        publicFallback = address.ToString();
                    }
                }
            }

            return publicFallback ?? FallbackAddress;
        }

        public static string Encode(string hostAddress)
        {
            byte[] octets = IPAddress.Parse(hostAddress).GetAddressBytes();
            int packedAddress = (octets[1] << 16) | (octets[2] << 8) | octets[3];
            char[] symbols = new char[Length];
            for (int symbolIndex = Length - 1; symbolIndex >= 0; symbolIndex--)
            {
                symbols[symbolIndex] = Alphabet[packedAddress & SymbolMask];
                packedAddress >>= BitsPerSymbol;
            }

            return new string(symbols);
        }

        public static bool TryDecode(string code, string localAddress, out string hostAddress)
        {
            hostAddress = null;
            if (code == null || code.Length != Length)
            {
                return false;
            }

            if (!IPAddress.TryParse(localAddress, out IPAddress parsedLocalAddress)
                || parsedLocalAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                return false;
            }

            int packedAddress = 0;
            foreach (char symbol in code)
            {
                int symbolValue = Alphabet.IndexOf(char.ToUpperInvariant(symbol));
                if (symbolValue < 0)
                {
                    return false;
                }

                packedAddress = (packedAddress << BitsPerSymbol) | symbolValue;
            }

            if (packedAddress >> AddressBitCount != 0)
            {
                return false;
            }

            byte firstOctet = parsedLocalAddress.GetAddressBytes()[0];
            hostAddress = $"{firstOctet}.{(packedAddress >> 16) & 255}.{(packedAddress >> 8) & 255}.{packedAddress & 255}";
            return true;
        }

        public static bool IsValidSymbol(char symbol)
        {
            return Alphabet.IndexOf(char.ToUpperInvariant(symbol)) >= 0;
        }

        static bool IsPrivate(byte[] octets)
        {
            return octets[0] == 10
                || (octets[0] == 192 && octets[1] == 168)
                || (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31);
        }

        static bool IsLinkLocal(byte[] octets)
        {
            return octets[0] == 169 && octets[1] == 254;
        }
    }
}
