using BRaVe_CardPrinting_DesktopApp.Models.Request;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace BRaVe_CardPrinting_DesktopApp.Services
{
    public class QueueCacheService
    {
        private const string ConnectionString =
            "Data Source=printing.db";

        // ==========================================
        // SAVE RECORDS TO SQLITE
        // ==========================================
        public void Save(List<CardPrintQueueItem> items)
        {
            using var connection =
                new SqliteConnection(ConnectionString);

            connection.Open();

            foreach (var item in items)
            {
                var cmd = connection.CreateCommand();

                cmd.CommandText = @"
INSERT OR REPLACE INTO LoadedQueue
(
    HouseholdId,
    FullName,
    FamilySize,
    Activity,
    Program,
    LocationInformation,
    AdditionalInformation,
    RegDate,
    BarcodeId,
   
    IsLocked,
    LockedBy,
    CardPrinted,
    PrintedOn,
    CachedOnUtc
)
VALUES
(
    $HouseholdId,
    $FullName,
    $FamilySize,
    $Activity,
    $Program,
    $LocationInformation,
    $AdditionalInformation,
    $RegDate,
    $BarcodeId,
  
    $IsLocked,
    $LockedBy,
    $CardPrinted,
    $PrintedOn,
    $CachedOnUtc
);";

                cmd.Parameters.AddWithValue(
                    "$HouseholdId",
                    item.HouseholdId ?? "");

                cmd.Parameters.AddWithValue(
                    "$FullName",
                    item.FullName ?? "");

                cmd.Parameters.AddWithValue(
                    "$FamilySize",
                    item.FamilySize);

                cmd.Parameters.AddWithValue(
                    "$Activity",
                    item.Activity ?? "");

                cmd.Parameters.AddWithValue(
                    "$Program",
                    item.Program ?? "");

                cmd.Parameters.AddWithValue(
                    "$LocationInformation",
                    item.LocationInformation ?? "");

                cmd.Parameters.AddWithValue(
                    "$AdditionalInformation",
                    item.AdditionalInformation ?? "");

                cmd.Parameters.AddWithValue(
                    "$RegDate",
                    item.RegDate.ToString("O") ?? "");

                cmd.Parameters.AddWithValue(
                    "$BarcodeId",
                    item.barcodeId ?? "");


                cmd.Parameters.AddWithValue(
                    "$IsLocked",
                    item.IsLocked ? 1 : 0);

                cmd.Parameters.AddWithValue(
                    "$LockedBy",
                    item.LockedBy ?? "");

                cmd.Parameters.AddWithValue(
                    "$CardPrinted",
                    item.CardPrinted ? 1 : 0);

                cmd.Parameters.AddWithValue(
                    "$PrintedOn",
                    item.PrintedOn?.ToString("O") ?? "");

                cmd.Parameters.AddWithValue(
                    "$CachedOnUtc",
                    DateTime.UtcNow.ToString("O"));

                cmd.ExecuteNonQuery();
            }
        }

        // ==========================================
        // LOAD RECORDS FROM SQLITE
        // ==========================================
        public List<CardPrintQueueItem> GetAll()
        {
            var items = new List<CardPrintQueueItem>();

            using var connection =
                new SqliteConnection(ConnectionString);

            connection.Open();

            var cmd = connection.CreateCommand();

            cmd.CommandText = @"
SELECT
    HouseholdId,
    FullName,
    FamilySize,
    Activity,
    Program,
    LocationInformation,
    AdditionalInformation,
    BarcodeId,
RegDate,
    IsLocked,
    LockedBy,
    CardPrinted
FROM LoadedQueue";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                items.Add(new CardPrintQueueItem
                {
                    HouseholdId =
                        reader["HouseholdId"]?.ToString(),

                    FullName =
                        reader["FullName"]?.ToString(),

                    FamilySize =
                        Convert.ToInt32(
                            reader["FamilySize"]),
                    RegDate = DateTime.TryParse(
                            reader["RegDate"]?.ToString(),
                            out var regDate)
                                ? regDate
                                : DateTime.MinValue,

                    Activity =
                        reader["Activity"]?.ToString(),

                    Program =
                        reader["Program"]?.ToString(),

                    LocationInformation =
                        reader["LocationInformation"]?.ToString(),

                    AdditionalInformation =
                        reader["AdditionalInformation"]?.ToString(),

                    barcodeId =
                        reader["BarcodeId"]?.ToString(),

                  

                    IsLocked =
                        Convert.ToInt32(
                            reader["IsLocked"]) == 1,

                    LockedBy =
                        reader["LockedBy"]?.ToString(),

                    CardPrinted =
                        Convert.ToInt32(
                            reader["CardPrinted"]) == 1
                });
            }

            return items;
        }


        // ==========================================
        // MARK RECORD AS PRINTED
        // ==========================================
        public void MarkAsPrinted(string householdId)
        {
            using var connection =
                new SqliteConnection(ConnectionString);

            connection.Open();

            var cmd = connection.CreateCommand();

            cmd.CommandText = @"
UPDATE LoadedQueue
SET
    CardPrinted = 1,
    PrintedOn = $PrintedOn
WHERE HouseholdId = $HouseholdId;";

            cmd.Parameters.AddWithValue(
                "$PrintedOn",
                DateTime.UtcNow.ToString("O"));

            cmd.Parameters.AddWithValue(
                "$HouseholdId",
                householdId);

            cmd.ExecuteNonQuery();
        }

        // ==========================================
        // CLEAR CACHE
        // ==========================================
        public void Clear()
        {
            using var connection =
                new SqliteConnection(ConnectionString);

            connection.Open();

            var cmd = connection.CreateCommand();

            cmd.CommandText =
                "DELETE FROM LoadedQueue";

            cmd.ExecuteNonQuery();
        }
    }
}