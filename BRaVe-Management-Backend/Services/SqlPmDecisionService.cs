using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;

namespace BRaVe_Management_Backend.Services
{
    public class SqlPmDecisionService : IPmDecisionService
    {
        private string connectionString { get; set; }
        public SqlPmDecisionService(ISecretProvider secretProvider)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        public async Task SaveDecision(DecisionDto data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"sp_SavePmImplementationDecision @AssessmentId,@AssessmentStatus,@Note,@IsSubmitted,@DecisionStatus,@TenantId,@UserId";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@AssessmentStatus", data.AssessmentStatusId);
            cmd.Parameters.AddWithValue("@Note", (object?)data.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsSubmitted", data.IsSubmitted);
            cmd.Parameters.AddWithValue("@DecisionStatus", (object?)data.DecisionStatusId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@UserId", UserId);

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new Exception("DatabaseError", ex);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Decision> GetCurrentDecision(int AssessmentId, int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT TOP (1)
                    AssessmentId, TenantId,
                    CreatedByUserId, CreatedOn,
                    UpdatedByUserId, UpdatedOn,
                    IsSubmitted, StatusId, Note
                FROM tbl_PMRecommendationImplementations
                WHERE AssessmentId = @AssessmentId AND TenantId = @TenantId  AND IsSubmitted = 0
                ORDER BY CreatedOn DESC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AssessmentId", AssessmentId);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);


                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Decision()
                        {
                            AssessmentId = reader.GetInt32(reader.GetOrdinal("AssessmentId")),
                            TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                            IsSubmitted = false,
                            DecisionStatusId = reader.IsDBNull(reader.GetOrdinal("StatusId")) ? null : reader.GetInt32(reader.GetOrdinal("StatusId")),
                            Note = reader.IsDBNull(reader.GetOrdinal("Note")) ? null : reader.GetString(reader.GetOrdinal("Note")),
                        };

                    }
                }

            }
            catch (Exception e)
            {

            }

            return null;
        }
    }
}
