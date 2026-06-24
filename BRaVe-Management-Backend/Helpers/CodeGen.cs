using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace BRaVe_Management_Backend.Helpers
{
    public static class CodeGen
    {
        // Crockford Base32 (no I,L,O,0,1), with optional check digit
        private static readonly char[] B32 = "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

        public static string SessionCode(string prefix, int bodyLen = 6, bool addCheckDigit = true)
        {
            try
            {
                Log.Information("Generating session code with prefix: {Prefix}, bodyLen: {BodyLen}, addCheckDigit: {AddCheckDigit}", prefix, bodyLen, addCheckDigit);

                Span<byte> rnd = stackalloc byte[bodyLen];
                RandomNumberGenerator.Fill(rnd);

                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    sb.Append(prefix);
                }

                int sum = 0;
                for (int i = 0; i < bodyLen; i++)
                {
                    int idx = rnd[i] % B32.Length;
                    sb.Append(B32[idx]);
                    sum = (sum + idx) % B32.Length;
                }

                if (addCheckDigit)
                {
                    sb.Append(B32[sum]); // simple mod-N check digit
                    Log.Debug("Added check digit {CheckDigit} for session code prefix: {Prefix}", B32[sum], prefix);
                }

                string code = sb.ToString();
                Log.Information("Generated session code successfully: {SessionCode}", code);

                return code;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating session code with prefix {Prefix}", prefix);
                throw;
            }
        }
    }
}
