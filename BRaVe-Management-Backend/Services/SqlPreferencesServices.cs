using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using static BRaVe_Management_Backend.Helpers.KeyVaultSecretNames;
using static Lucene.Net.Util.Fst.Util;

namespace BRaVe_Management_Backend.Services
{
    public class SqlPreferencesServices : IPreferenceService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlPreferencesServices> _logger;

        public SqlPreferencesServices(ISecretProvider secretProvider, ILogger<SqlPreferencesServices> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }


        public async Task<IEnumerable<MissionPreferences>> GetAllMissionPreferences(int tenantId)
        {
            _logger.LogInformation("Fetching all preferences for mission {TenantId}", tenantId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
          
                SELECT 
                m.Id,
                m.TenantId,
                m.DefaultValue,
                m.CreatedOn,
                t.Name,
                m.CreatedByUserId,
                m.UpdatedOn,
                m.UpdatedByUserId,
                p.PreferenceName,
                p.Description
            FROM tbl_MissionPreferences m
            INNER JOIN tbl_Preferences p ON m.Id = p.Id INNER JOIN lkp_PreferenceType t ON t.Id= p.PreferenceType
            WHERE m.TenantId = @TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                var missionPreferences = new List<MissionPreferences>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var pref = new MissionPreferences
                    {
                        PreferenceId = reader.GetInt32(reader.GetOrdinal("Id")),
                        TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),

                        PreferenceName = reader.IsDBNull(reader.GetOrdinal("PreferenceName"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("PreferenceName")),

                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),

                        DefaultValue = reader.IsDBNull(reader.GetOrdinal("DefaultValue"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("DefaultValue")),

                        PreferenceType = reader.GetString(reader.GetOrdinal("Name")),

                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CreatedByUserId")),

                        CreatedOn = reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UpdatedByUserId")),

                        UpdatedOn = reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("UpdatedOn"))
                    };

                    missionPreferences.Add(pref);
                }

                _logger.LogInformation("Fetched {Count} mission preferences for tenant {TenantId}.", missionPreferences.Count, tenantId);
                return missionPreferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mission preferences for tenant {TenantId}", tenantId);
                throw;
            }
        }


        public async Task<IEnumerable<MissionPreferences>> GetPreferenceById(int PreferenceId, int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"
            SELECT 
                p.Id AS PreferenceId,
                p.PreferenceName,
                m.DefaultValue,
                m.TenantId,
                p.Description,
                p.PreferenceType,
                m.CreatedByUserId,
                m.UpdatedByUserId,
                m.CreatedOn,
                m.UpdatedOn
            FROM tbl_Preferences p
            INNER JOIN tbl_MissionPreferences m ON p.Id = m.Id
            WHERE p.Id = @PreferenceId AND m.TenantId = @TenantId";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PreferenceId", PreferenceId);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                using var reader = await cmd.ExecuteReaderAsync();

                var results = new List<MissionPreferences>();

                while (await reader.ReadAsync())
                {
                    var preference = new MissionPreferences
                    {
                        PreferenceId = reader.GetInt32(reader.GetOrdinal("PreferenceId")),
                        PreferenceName = reader.GetString(reader.GetOrdinal("PreferenceName")),

                        DefaultValue = reader.IsDBNull(reader.GetOrdinal("DefaultValue"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("DefaultValue")),

                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
                            ? (int?)null
                            : reader.GetInt32(reader.GetOrdinal("TenantId")),

                        Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Description")),

                        PreferenceType = reader.GetString(reader.GetOrdinal("PreferenceType")),

                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("CreatedByUserId")),

                        UpdatedByUserId = reader.IsDBNull(reader.GetOrdinal("UpdatedByUserId"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("UpdatedByUserId"))

                        //                    CreatedOn = (DateTime)(reader.IsDBNull(reader.GetOrdinal("CreatedOn"))
                        //? (DateTime?)null
                        //: reader.GetDateTime(reader.GetOrdinal("CreatedOn"))),
                        //                    UpdatedOn = (DateTime)(reader.IsDBNull(reader.GetOrdinal("UpdatedOn"))
                        //? (DateTime?)null
                        //: reader.GetDateTime(reader.GetOrdinal("UpdatedOn")))
                    };

                    results.Add(preference);
                }

                return results;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task CreatePreference(Preferences newPreference)
        {
            throw new NotImplementedException();
        }


        public Task UpdatePreference(Preferences updatedPreference)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<MissionPreferences>> GetAllProgramPreferences(int programId)
        {
            throw new NotImplementedException();
        }




        public async Task<IEnumerable<Preferences>> GetDefaultPreferences()
        {
            _logger.LogInformation("Fetching all preferences for ProgramId");

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = "SELECT p.Id,p.PreferenceName, p.Description, t.Name, p.DefaultValue, p.CreatedByUserId, p.CreatedOn FROM tbl_Preferences p INNER JOIN lkp_PreferenceType t ON t.Id= p.PreferenceType";
                using var cmd = new SqlCommand(sql, conn);


                var preferences = new List<Preferences>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    preferences.Add(new Preferences
                    {
                        PreferenceId = reader.GetInt32(reader.GetOrdinal("Id")),
                        PreferenceName = reader.GetString(reader.GetOrdinal("PreferenceName")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DefaultValue = reader.GetString(reader.GetOrdinal("DefaultValue")),
                        PreferenceType = reader.GetString(reader.GetOrdinal("Name")),
                        CreatedByUserId = reader.IsDBNull(reader.GetOrdinal("CreatedByUserId")) ? null : reader.GetString(reader.GetOrdinal("CreatedByUserId")),
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"))

                    });
                }
                _logger.LogInformation("Fetched {Count} mission preferences", preferences.Count);


                return (IEnumerable<Preferences>)preferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching surveys for ProgramId {ProgramId}");
                throw;
            }
        }

        public async Task InsertPreferenceAsync(MissionPreferencesDto dto)
        {

        }

        public async Task<int> CreatePreferenceAsync(int tenantId, string userId)
        {
            int result = 0;

            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_AttachedTenantDefaultPreferences", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                cmd.Parameters.AddWithValue("@UserId", userId);


                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = Convert.ToInt32(reader["InsertedCount"]);
                    }
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Error attaching default preferences to Mission Id {tenantId}");
                throw;
            }

            return result;
        }

        public Task AddPreferenceToMission(MissionPreferences newPreference)
        {
            throw new NotImplementedException();
        }


        public async Task UpdateMissionPreference(List<MissionPreferences> updatedList)
        {
            if (updatedList == null || !updatedList.Any())
                return;

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            // Create DataTable to pass as TVP
            var tvp = new DataTable();
            tvp.Columns.Add("PreferenceId", typeof(int));
            tvp.Columns.Add("TenantId", typeof(int));
            tvp.Columns.Add("DefaultValue", typeof(string));
            tvp.Columns.Add("CreatedByUserId", typeof(string));
            tvp.Columns.Add("CreatedOn", typeof(DateTime));
            tvp.Columns.Add("UpdatedByUserId", typeof(string));
            tvp.Columns.Add("UpdatedOn", typeof(DateTime));

            foreach (var pref in updatedList)
            {
                tvp.Rows.Add(
                    pref.PreferenceId,
                    pref.TenantId ?? (object)DBNull.Value,
                    pref.DefaultValue ?? (object)DBNull.Value,
                    pref.CreatedByUserId ?? (object)DBNull.Value,
                    pref.CreatedOn,
                    pref.UpdatedByUserId ?? (object)DBNull.Value,
                    pref.UpdatedOn
                );
            }

            using var command = new SqlCommand("sp_UpsertMissionPreferences", connection, transaction);
            command.CommandType = CommandType.StoredProcedure;

            var param = command.Parameters.AddWithValue("@MissionPrefs", tvp);
            param.SqlDbType = SqlDbType.Structured;
            param.TypeName = "dbo.MissionPreferenceType";

            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }

        
    }
}
