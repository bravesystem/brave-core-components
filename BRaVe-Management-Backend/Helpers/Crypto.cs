using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Management_Backend.Helpers
{
    public static class Crypto
    {
        public static byte[] Sha256Bytes(string s)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }
    }
}
