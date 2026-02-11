using System.Security.Cryptography;
using System.Text;

namespace IncidentManagementAPI.Common
{
    public static class CryptoHelper
    {
        public static string Sha256Base64(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public static string SecureRandomBase64(int byteLen = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLen);
            return Convert.ToBase64String(bytes);
        }

        public static string Otp6Digits()
            => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }
}
