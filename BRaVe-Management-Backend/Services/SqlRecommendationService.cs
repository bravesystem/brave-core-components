using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;

namespace BRaVe_Management_Backend.Services
{
    public class SqlRecommendationService: IRecommendationService
    {
        private string connectionString { get; set; }
        public SqlRecommendationService(ISecretProvider secretProvider)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }


        public async Task<Recommendation> GetCurrentRecommendation(int AssessmentId, bool IsLeg, int TenantId)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();

                var sql = @"SELECT TOP (1)
                    ApprovalId, AssessmentId, TenantId,
                    CreatedByUserId, CreatedOn,
                    UpdatedByUserId, UpdatedOn,
                    IsSubmitted, Note
                FROM tbl_DHRRLEGRecommendations
                WHERE AssessmentId = @AssessmentId AND TenantId = @TenantId AND CreatedByRole=@IsLeg  AND IsSubmitted = 0
                ORDER BY CreatedOn DESC, ApprovalId DESC;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AssessmentId", AssessmentId);
                cmd.Parameters.AddWithValue("@TenantId", TenantId);
                cmd.Parameters.AddWithValue("@IsLeg", IsLeg? 2: 1);


                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Recommendation()
                        {
                            AssessmentId = reader.GetInt32(reader.GetOrdinal("AssessmentId")),
                            TenantId = TenantId,
                            IsSubmitted = false,
                            IsLeg = IsLeg,
                            Note = reader.IsDBNull(reader.GetOrdinal("Note")) ? null : reader.GetString(reader.GetOrdinal("Note"))
                        };

                    }
                }

            }
            catch (Exception e)
            {

            }

            return null;
        }

        public async Task SaveRecommendation(RecommendationDto data, int TenantId, string UserId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"sp_SaveROHQLegRecommendation @AssessmentId,@AssessmentStatus,@Note,@IsSubmitted,@TenantId,@IsLEG,@UserId";

            cmd.Parameters.AddWithValue("@AssessmentId", data.AssessmentId);
            cmd.Parameters.AddWithValue("@AssessmentStatus", data.AssessmentStatusId);
            cmd.Parameters.AddWithValue("@Note", (object?)data.Note ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsSubmitted", data.IsSubmitted);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@IsLEG", data.IsLeg);
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
    }
}
