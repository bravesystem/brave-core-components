using System.Text;

namespace BRaVe_Management_Backend.Helpers
{
    public static class Bytes
    {
        // Hex
        public static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
        public static byte[] FromHex(string hex)
        {
            if (hex.Length % 2 != 0) throw new ArgumentException("Hex length must be even");
            var buf = new byte[hex.Length / 2];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return buf;
        }
    }
}
