using Microsoft.Data.Sqlite;

namespace BRaVe_CardPrinting_DesktopApp.Db
{
    public static class DbInitializer
    {
        private static string _connectionString = "Data Source=printing.db";

        public static void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
CREATE TABLE IF NOT EXISTS PrintedRecords (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT,
    HouseholdId TEXT,
    PrintedOn TEXT,
    TemplateUsed TEXT,
    PrinterName TEXT,
    SyncDate DateTime
);

CREATE TABLE IF NOT EXISTS LoadedQueue (
    HouseholdId TEXT PRIMARY KEY,
    FullName TEXT,
    FamilySize INTEGER,
    Activity TEXT,
    Program TEXT,
    LocationInformation TEXT,
    AdditionalInformation TEXT,
    RegDate TEXT,
    BarcodeId TEXT,
   
    IsLocked INTEGER,
    LockedBy TEXT,
    CardPrinted INTEGER,
    PrintedOn TEXT,
    CachedOnUtc TEXT
);";

            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public static void ClearPrintedRecords()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "DELETE FROM PrintedRecords;";

            using var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }
}