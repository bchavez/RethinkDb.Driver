using System.Security.Cryptography;

namespace RethinkDb.Driver.Net
{
    internal class Crypto
    {
        private static string DEFAULT_SSL_PROTOCOL = "TLSv1.2";
        private static string HMAC_SHA_256 = "HmacSHA256";
        private static string PBKDF2_ALGORITHM = "PBKDF2WithHmacSHA256";

        private static RNGCryptoServiceProvider secureRandom = new RNGCryptoServiceProvider();

        private static int NONCE_BYTES = 18;
    }
}