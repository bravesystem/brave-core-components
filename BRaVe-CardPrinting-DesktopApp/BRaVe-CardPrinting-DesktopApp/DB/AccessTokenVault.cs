using Microsoft.Data.Sqlite;
using System;

namespace BRaVe_CardPrinting_DesktopApp.Security
{
    public class AccessTokenVault
    {
        private readonly TokenEncryptionService _crypto;

        private const string DbPath = "brave_tokens.db";

        public AccessTokenVault(TokenEncryptionService crypto)
        {
            _crypto = crypto;
        }

        public void SaveToken(string token, DateTime expiryUtc)
        {
            using var cn = new SqliteConnection($"Data Source={DbPath}");

            cn.Open();

            var encrypted = _crypto.Encrypt(token);

            var deleteCmd = cn.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM tbl_AccessTokens";
            deleteCmd.ExecuteNonQuery();

            var cmd = cn.CreateCommand();

            cmd.CommandText = @"
INSERT INTO tbl_AccessTokens
(
    EncryptedToken,
    ExpiresAtUtc,
    CreatedOnUtc
)
VALUES
(
    $token,
    $expiry,
    $created
);";

            cmd.Parameters.AddWithValue("$token", encrypted);
            cmd.Parameters.AddWithValue("$expiry", expiryUtc.ToString("O"));
            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O"));

            cmd.ExecuteNonQuery();
        }

        public (string Token, DateTime ExpiryUtc)? LoadToken()
        {
            using var cn = new SqliteConnection($"Data Source={DbPath}");

            cn.Open();

            var cmd = cn.CreateCommand();

            cmd.CommandText = @"
SELECT EncryptedToken, ExpiresAtUtc
FROM tbl_AccessTokens
LIMIT 1;";

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var encrypted = reader.GetString(0);

            var expiry = DateTime.Parse(reader.GetString(1));

            var token = _crypto.Decrypt(encrypted);

            return (token, expiry);
        }

        public void Clear()
        {
            using var cn = new SqliteConnection($"Data Source={DbPath}");

            cn.Open();

            var cmd = cn.CreateCommand();

            cmd.CommandText = "DELETE FROM tbl_AccessTokens";

            cmd.ExecuteNonQuery();
        }
    }
}