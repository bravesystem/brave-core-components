     using Microsoft.Data.Sqlite;
    using System.IO;
    using System.Windows.Forms;

    namespace BRaVe_CardPrinting_DesktopApp.Db
    {
        public static class DbConnectionFactory
        {
            private static readonly string _dbPath =
                Path.Combine(Application.StartupPath, "printing.db");

            private static readonly string _connectionString =
                $"Data Source={_dbPath}";

            public static SqliteConnection Create()
            {
                // Ensure directory exists (safe guard)
                var dir = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return new SqliteConnection(_connectionString);
            }
        }
    }
