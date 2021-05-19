using System.Security.Cryptography;
using System.Text;

namespace Vendr.PaymentProviders.Worldpay.Helpers
{
    public static class Md5Helper
    {
        public static string CreateMd5(string input)
        {
            return new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(input)).ToHex(true);
        }

        public static string ToHex(this byte[] bytes, bool toLower)
        {
            var c = new char[bytes.Length * 2];
            for (int bx = 0, cx = 0; bx < bytes.Length; ++bx, ++cx)
            {
                var b = ((byte)(bytes[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(bytes[bx] & 0x0F));
                c[++cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return toLower ? new string(c).ToLower() : new string(c);
        }
    }
}