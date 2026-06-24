using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BRaVe_Management_Backend.Services
{
    public class SqlRegionService : IRegionService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlRegionService> _logger;

        public SqlRegionService(ISecretProvider secretProvider, ILogger<SqlRegionService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            _logger.LogInformation("SqlRegionService initialized with connection string from KeyVault");
        }

        public async Task CreateCountry(Country data, string UserId)
        {
            _logger.LogInformation("Creating country {CountryIso2} by user {UserId}", data.CountryIso2, UserId);

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"INSERT INTO lkp_Countries (CountryIso2,Name,PhoneCode,RegionId,CreatedByUserId,CreatedOn) 
                  VALUES (@CountryCode, @Name, @PhoneCode, @RegionId, @CreatedByUserId, GETUTCDATE())", conn);
            cmd.Parameters.AddWithValue("@CountryCode", data.CountryIso2);
            cmd.Parameters.AddWithValue("@Name", data.Name);
            cmd.Parameters.AddWithValue("@PhoneCode", data.PhoneCode);
            cmd.Parameters.AddWithValue("@RegionId", data.RegionId);
            cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Country {CountryIso2} created successfully", data.CountryIso2);
        }

        public async Task CreateRegion(RegionDto data, string UserId)
        {
            try
            {
                _logger.LogInformation("Creating region {RegionName} by user {UserId}", data.Name, UserId);

                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_CreateRegion", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Name", data.Name);

                // handle nullable Note safely
                cmd.Parameters.AddWithValue("@Note", (object?)data.Note ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@CreatedByUserId", UserId);

                var rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Region {RegionName} created successfully. Rows affected: {Rows}",
                    data.Name,
                    rows
                );
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx,
                    "SQL error while creating region {RegionName} for user {UserId}",
                    data?.Name,
                    UserId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while creating region {RegionName} for user {UserId}",
                    data?.Name,
                    UserId);

                throw;
            }
        }

        public async Task DeleteCountry(string id)
        {
            _logger.LogInformation("Deleting country {CountryIso2}", id);

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"DELETE c FROM lkp_Countries c
                  LEFT JOIN tbl_Missions m on m.CountryIso2=c.CountryIso2
                  WHERE c.CountryIso2=@CountryIso2 AND c.MissionId IS NULL;", conn);
            cmd.Parameters.AddWithValue("@CountryIso2", id);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Country {CountryIso2} deleted successfully", id);
        }

        public async Task DeleteRegion(int id)
        {
            _logger.LogInformation("Deleting region {RegionId}", id);

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"DELETE r FROM lkp_Regions r
                  LEFT JOIN lkp_Countries c on r.RegionId=c.RegionId
                  WHERE r.RegionId=@RegionId AND c.CountryIso2 IS NULL;", conn);
            cmd.Parameters.AddWithValue("@RegionId", id);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Region {RegionId} deleted successfully", id);
        }

        public async Task<IEnumerable<Country>> GetAllCountries()
        {
            _logger.LogInformation("Fetching all countries");

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT CountryIso2,Name,PhoneCode,RegionId from lkp_Countries";
                using var cmd = new SqlCommand(sql, conn);

                var countries = new List<Country>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    countries.Add(new Country
                    {
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        PhoneCode = reader.GetString(reader.GetOrdinal("PhoneCode")),
                        RegionId = reader.GetInt32(reader.GetOrdinal("RegionId"))
                    });
                }

                _logger.LogInformation("Fetched {Count} countries", countries.Count);
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch countries");
                return null;
            }
        }

        public async Task<IEnumerable<Region>> GetAllRegions()
        {
            _logger.LogInformation("Fetching all regions");

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT RegionId, Name, Note FROM lkp_Regions ORDER BY    COALESCE(UpdatedOn, CreatedOn) DESC;";
                using var cmd = new SqlCommand(sql, conn);

                var regions = new List<Region>();

                using var reader = await cmd.ExecuteReaderAsync();

                var regionIdOrdinal = reader.GetOrdinal("RegionId");
                var nameOrdinal = reader.GetOrdinal("Name");
                var noteOrdinal = reader.GetOrdinal("Note");

                while (await reader.ReadAsync())
                {
                    regions.Add(new Region
                    {
                        RegionId = reader.GetInt32(regionIdOrdinal),
                        Name = reader.GetString(nameOrdinal),

                        Note = reader.IsDBNull(noteOrdinal)
                            ? null
                            : reader.GetString(noteOrdinal)
                    });
                }

                _logger.LogInformation("Fetched {Count} regions", regions.Count);
                return regions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch regions");
                return Enumerable.Empty<Region>();
            }
        }

        public async Task<Country?> GetCountry(string id)
        {
            _logger.LogInformation("Fetching country {CountryIso2}", id);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT CountryIso2, Name, PhoneCode, RegionId from lkp_Countries WHERE CountryIso2=@CountryIso2";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@CountryIso2", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Country
                    {
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        PhoneCode = reader.GetString(reader.GetOrdinal("PhoneCode")),
                        RegionId = reader.GetInt32(reader.GetOrdinal("RegionId"))
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch country {CountryIso2}", id);
            }

            return null;
        }

        public async Task<Region?> GetRegion(int id)
        {
            _logger.LogInformation("Fetching region {RegionId}", id);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT RegionId, Name, Note FROM lkp_Regions WHERE RegionId=@RegionId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@RegionId", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var noteOrdinal = reader.GetOrdinal("Note");

                    return new Region
                    {
                        RegionId = reader.GetInt32(reader.GetOrdinal("RegionId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),

                        Note = reader.IsDBNull(noteOrdinal)
                            ? null
                            : reader.GetString(noteOrdinal)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch region {RegionId}", id);
            }

            return null;
        }

        public async Task UpdateCountry(Country data, string UserId)
        {
            _logger.LogInformation("Updating country {CountryIso2} by user {UserId}", data.CountryIso2, UserId);

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                @"UPDATE lkp_Countries 
                  SET Name=@Name, PhoneCode=@PhoneCode, RegionId=@RegionId, UpdatedByUserId=@UpdatedByUserId, UpdatedOn=GETUTCDATE() 
                  WHERE CountryIso2=@CountryIso2", conn);
            cmd.Parameters.AddWithValue("@Name", data.Name);
            cmd.Parameters.AddWithValue("@PhoneCode", data.PhoneCode);
            cmd.Parameters.AddWithValue("@RegionId", data.RegionId);
            cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);
            cmd.Parameters.AddWithValue("@CountryIso2", data.CountryIso2);

            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("Country {CountryIso2} updated successfully", data.CountryIso2);
        }
        public async Task UpdateRegion(Region data, string UserId)
        {
            _logger.LogInformation("Updating region {RegionId} by user {UserId}", data.RegionId, UserId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(
                    @"UPDATE lkp_Regions 
              SET Name=@Name, 
                  Note=@Note, 
                  UpdatedByUserId=@UpdatedByUserId, 
                  UpdatedOn=GETUTCDATE() 
              WHERE RegionId=@RegionId", conn);

                cmd.Parameters.AddWithValue("@Name", data.Name);

                //NULL-safe handling
                cmd.Parameters.AddWithValue("@Note", (object?)data.Note ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@RegionId", data.RegionId);
                cmd.Parameters.AddWithValue("@UpdatedByUserId", UserId);

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("Region {RegionId} updated successfully", data.RegionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update region {RegionId} by user {UserId}",
                    data.RegionId,
                    UserId);

                throw;
            }
        }


        public async Task<IEnumerable<Country>> GetAllCountriesByRegionId(int id)
        {
            _logger.LogInformation("Fetching countries for RegionId={RegionId}", id);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT CountryIso2,Name,PhoneCode,RegionId from lkp_Countries WHERE RegionId=@RegionId";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@RegionId", id);

                var countries = new List<Country>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    countries.Add(new Country
                    {
                        CountryIso2 = reader.GetString(reader.GetOrdinal("CountryIso2")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        PhoneCode = reader.GetString(reader.GetOrdinal("PhoneCode")),
                        RegionId = reader.GetInt32(reader.GetOrdinal("RegionId"))
                    });
                }

                _logger.LogInformation("Fetched {Count} countries for RegionId={RegionId}", countries.Count, id);
                return countries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch countries for RegionId={RegionId}", id);
                return null;
            }
        }
    }
}
