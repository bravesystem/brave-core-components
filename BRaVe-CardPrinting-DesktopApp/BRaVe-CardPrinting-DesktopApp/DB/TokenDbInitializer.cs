using Microsoft.Data.Sqlite;

namespace BRaVe_CardPrinting_DesktopApp.Db
{
    public static class TokenDbInitializer
    {
        private static readonly string DbPath = "brave_tokens.db";

        public static void Initialize()
        {
            using var cn = new SqliteConnection($"Data Source={DbPath}");

            cn.Open();

            var cmd = cn.CreateCommand();

            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS tbl_AccessTokens
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EncryptedToken TEXT NOT NULL,
    ExpiresAtUtc TEXT NOT NULL,
    CreatedOnUtc TEXT NOT NULL
);";

            cmd.ExecuteNonQuery();
        }
    }
}