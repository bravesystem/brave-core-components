using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace BRaVe_Management_Backend.Services.Mock
{
    //public sealed class MockAttestationVerifier : IAttestationVerifier
    //{
    //    public Task<bool> VerifyHardwareAsync(AttestationKeyDto dto, byte[] expectedThumb)
    //    {
    //        try
    //        {
    //            var leafPem = dto.ChainPem.First();
    //            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
    //                Convert.FromBase64String(ExtractBase64(leafPem)));
    //            var pubDer = cert.GetPublicKey();
    //            var actual = SHA256.HashData(pubDer);
    //            return Task.FromResult(actual.SequenceEqual(expectedThumb));
    //        }
    //        catch { return Task.FromResult(false); }

    //        static string ExtractBase64(string pem)
    //        {
    //            var s = pem.Replace("-----BEGIN CERTIFICATE-----", "")
    //                       .Replace("-----END CERTIFICATE-----", "")
    //                       .Replace("\r", "").Replace("\n", "");
    //            return s.Trim();
    //        }
    //    }
    //}


    public sealed class MockAttestationVerifier : IAttestationVerifier
    {
        public Task<bool> VerifyHardwareAsync(AttestationKeyDto dto, byte[] expectedThumb)
        {
            try
            {
                var leafPem = dto.ChainPem.First(); // ensure this is the leaf
                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    Convert.FromBase64String(ExtractBase64(leafPem)));

                // Get SPKI (SubjectPublicKeyInfo) bytes
                byte[] spki;
                using (var ecdsa = cert.GetECDsaPublicKey())
                {
                    if (ecdsa != null)
                    {
                        spki = ecdsa.ExportSubjectPublicKeyInfo();
                    }
                    else
                    {
                        using var rsa = cert.GetRSAPublicKey();
                        if (rsa == null) return Task.FromResult(false);
                        spki = rsa.ExportSubjectPublicKeyInfo();
                    }
                }

                var actual = System.Security.Cryptography.SHA256.HashData(spki);
                return Task.FromResult(actual.AsSpan().SequenceEqual(expectedThumb));
            }
            catch
            {
                return Task.FromResult(false);
            }

            static string ExtractBase64(string pem) =>
                pem.Replace("-----BEGIN CERTIFICATE-----", "")
                   .Replace("-----END CERTIFICATE-----", "")
                   .Replace("\r", "").Replace("\n", "").Trim();
        }
    }

}
