using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
//using NCalc;
using System.Data;
using System.Linq.Expressions;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SqlTargetingCriteriaService : ITargetingCriteriaService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlTargetingCriteriaService> _logger;

        public SqlTargetingCriteriaService(
            ISecretProvider secretProvider,
            ILogger<SqlTargetingCriteriaService> logger)
        {
            _logger = logger;

            _connectionString =
                secretProvider.GetSecretAsync(
                    KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        // =====================================================
        // CREATE
        // =====================================================
        // ==========================
        // CREATE USING STORED PROCEDURE
        // ==========================
        public async Task<TargetingCriteriaDto> CreateAsync(TargetingCriteriaDto dto)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("sp_InsertTargetingCriteria", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TargetingId", dto.TargetingId);
           
            cmd.Parameters.AddWithValue("@CriteriaName", dto.CriteriaName);
            cmd.Parameters.AddWithValue("@Description", dto.Description ?? "");
            cmd.Parameters.AddWithValue("@Score", dto.Score);
            cmd.Parameters.AddWithValue("@Weight", dto.Weight);
            cmd.Parameters.AddWithValue("@JsonRule", (object?)dto.JsonRule ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Expression", (object?)dto.Expression ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DataType", (int)dto.DataType);
            cmd.Parameters.AddWithValue("@LookUpId", (object?)dto.LookUpId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

            // OUTPUT parameter for inserted Id
            var outputId = new SqlParameter("@InsertedId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputId);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            dto.Id = (int)outputId.Value;
            return dto;
        }

        // ==========================
        // UPDATE USING STORED PROCEDURE
        // ==========================
        public async Task<TargetingCriteriaDto> UpdateAsync(int id, TargetingCriteriaDto dto)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("sp_UpdateTargetingCriteria", con)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Id", id);

                cmd.Parameters.AddWithValue("@CriteriaName", dto.CriteriaName);
                cmd.Parameters.AddWithValue("@Description", dto.Description ?? "");
                cmd.Parameters.AddWithValue("@Score", dto.Score);
                cmd.Parameters.AddWithValue("@Weight", dto.Weight);
                cmd.Parameters.AddWithValue("@JsonRule", (object?)dto.JsonRule ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Expression", (object?)dto.Expression ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataType", (int)dto.DataType);
                cmd.Parameters.AddWithValue("@LookUpId", (object?)dto.LookUpId ?? DBNull.Value);

                await con.OpenAsync();
                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                    throw new Exception("Criteria not found or update failed.");

                return dto;
            }
            catch (Exception ex) 
            {
                throw new Exception();
            }
         
        }

        // =====================================================
        // DELETE
        // =====================================================
        public async Task<bool> DeleteAsync(int id)
        {
            const string sql =
                "DELETE FROM tbl_TargetingCriterias WHERE Id=@Id";

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // =====================================================
        // GET BY ID
        // =====================================================
        public async Task<TargetingCriteriaDto?> GetByIdAsync(int id)
        {
            const string sql =
                "SELECT * FROM tbl_TargetingCriterias WHERE Id=@Id";

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@Id", id);

            await con.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();

            if (!r.Read())
                return null;

            return Map(r);
        }

        // =====================================================
        // GET BY RULE
        // =====================================================
        public async Task<List<TargetingCriteriaDto>> GetByTargetingIdAsync(int targetingId)
        {
            const string sql =
                "SELECT * FROM tbl_TargetingCriterias WHERE TargetingId=@Id";

            var list = new List<TargetingCriteriaDto>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@Id", targetingId);

            await con.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                list.Add(Map(r));
            }

            return list;
        }
        public async Task<List<TargetingFieldValueDto>> GetLookupValuesAsync(int lookupId, int tenantId)
        {
            var results = new List<TargetingFieldValueDto>();

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand("sp_GetLookupValuesByLookupId", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@LookupId", SqlDbType.Int).Value = lookupId;
            cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new TargetingFieldValueDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Code = reader["Code"]?.ToString(),
                    Name = reader["Name"]?.ToString()
                });
            }

            return results;
        }

        public async Task<List<TargetingCriteriaDto>> GetAllAsync()
        {
            const string sql = "SELECT * FROM tbl_TargetingCriterias";

            var list = new List<TargetingCriteriaDto>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);

            await con.OpenAsync();

            using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                list.Add(Map(r));
            }

            return list;
        }



        private static TargetingCriteriaDto Map(SqlDataReader r)
        {
            return new TargetingCriteriaDto
            {
                Id = (int)r["Id"],
                TargetingId = (int)r["TargetingId"],
                Code = r["Code"].ToString()!,
                CriteriaName = r["CriteriaName"].ToString()!,
                Description = r["Description"].ToString()!,
                Score = Convert.ToDecimal(r["Score"]),
                Weight = Convert.ToDecimal(r["Weight"]),
                JsonRule = r["JsonRule"] as string,
                Expression = r["Expression"] as string,
                DataType = (IndicatorDataType)Convert.ToInt32(r["DataType"]),
                LookUpId = r["LookUpId"] as int?,
                CreatedOn = (DateTime)r["CreatedOn"],
                CreatedBy = r["CreatedBy"].ToString()!
            };
        }

        private static bool ValidateResultType(object result, IndicatorDataType type)
        {
            try
            {
                return type switch
                {
                    IndicatorDataType.Number => result is int,
                    IndicatorDataType.Numeric => result is double or float or decimal,
                    IndicatorDataType.String => result is string,
                    IndicatorDataType.Boolean => result is bool,
                    IndicatorDataType.Date => result is DateTime,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }
    }
}
