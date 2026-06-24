using System.Buffers.Text;
using System.Text;

namespace BRaVe_Mobile_Backend.Helpers
{
    public class PemHelper
    {

        public static string DerBytesB64(byte[] derBytes)
        {
            // Convert to Base64
            string base64 = Convert.ToBase64String(derBytes);

            return WrapWithPemHeader(base64);
        }

        private static string WrapWithPemHeader(string base64)
        {

            // Wrap with PEM headers
            string pemString = $"-----BEGIN PUBLIC KEY-----\n{InsertLineBreaks(base64, 64)}\n-----END PUBLIC KEY-----";
            
            return pemString;
        }
        private static string InsertLineBreaks(string input, int lineLength)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i += lineLength)
            {
                sb.AppendLine(input.Substring(i, Math.Min(lineLength, input.Length - i)));
            }
            return sb.ToString().TrimEnd();
        }

    }
}
