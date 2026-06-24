using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace BRaVe_Management_Backend.Services
{
    public class SqlIndicatorService : IIndicatorRepositoryService
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlProgramService> _logger;

        public SqlIndicatorService(ISecretProvider secretProvider, ILogger<SqlProgramService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            _logger.LogInformation("SqlIndicatorService initialized with connection string from KeyVault");
        }

        public async Task<List<IndicatorDto>> GetActiveIndicatorsAsync(int TenantId)
        {
            const string sql = @"
            SELECT IndicatorId,ProgramId, LookUpId, Code, Name, [Level], IndicatorType, DataType,
                   Description, IsActive, Expression, JsonRule, updated_on
            FROM tbl_Indicators
            WHERE IsActive = 1 AND (TenantId = @TenantId OR TenantId IS NULL)";

            var results = new List<IndicatorDto>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            cmd.Parameters.Add(new SqlParameter("@TenantId", TenantId));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapToDto(reader));
            }

            return results;
        }

        
        public async Task<IndicatorDto?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            const string sql = @"
            SELECT TOP 1 IndicatorId,ProgramId, LookUpId, Code, Name, [Level], IndicatorType, DataType,
                   Description, IsActive, Expression, JsonRule,updated_on
            FROM tbl_Indicators
            WHERE Code = @Code";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new SqlParameter("@Code", code));

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapToDto(reader) : null;
        }

        public async Task<IndicatorDto?> GetByIdAsync(int id)
        {
            const string sql = @"
            SELECT IndicatorId, ProgramId,LookUpId, Code, Name, [Level], IndicatorType, DataType,
                   Description, IsActive, Expression, JsonRule, updated_on
            FROM tbl_Indicators
            WHERE IndicatorId = @Id";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Add(new SqlParameter("@Id", id));
            try
            {
                using var reader = await cmd.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapToDto(reader) : null;
            }
            catch (Exception e)
            {
                return null;
            }
            
        }


        public Task<List<CompositeIndicatorDto>> GetCompositeIndicatorsAsync(int TenantId)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndicatorDependencyDto>> GetDependenciesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IndicatorDto> SaveCompositeAsync(int tenantId, string createdBy, IndicatorDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

                await using var cmd = new SqlCommand("dbo.sp_InsertIndicator", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                AddSaveParameters(cmd, dto, tenantId, createdBy);

                var outputId = new SqlParameter("@IndicatorId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                cmd.Parameters.Add(outputId);

                await cmd.ExecuteNonQueryAsync();

                dto.IndicatorId = (int)outputId.Value;

                return dto;
            
        }



        public async Task<IndicatorDto?> UpdateAsync(int id, int tenantId, string updatedBy, IndicatorDto dto)
        {

            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (id != dto.IndicatorId)
                return null;

            const string updateSql = @"
            UPDATE tbl_Indicators SET
                Code = @Code, Name = @Name, [Level] = @Level,
                DataType = @DataType, Description = @Description,
                IsActive = @IsActive, Expression = @Expression, JsonRule = @JsonRule,
                updated_on = GETDATE(), updated_by = @UpdatedBy
            WHERE IndicatorId = @Id AND TenantId = @TenantId";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = updateSql;
            AddSaveParameters(cmd, dto);
            cmd.Parameters.Add(new SqlParameter("@Id", id));
            cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new SqlParameter("@UpdatedBy", updatedBy ?? ""));
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0 ? dto : null;

        }


        private static IndicatorDto MapToDto(System.Data.Common.DbDataReader reader)
        {
            return new IndicatorDto
            {
                IndicatorId = reader.GetInt32(reader.GetOrdinal("IndicatorId")),
                ProgramId= reader.IsDBNull(reader.GetOrdinal("ProgramId")) ? null : reader.GetInt32(reader.GetOrdinal("ProgramId")),
                LookUpId = reader.IsDBNull(reader.GetOrdinal("LookUpId")) ? null : reader.GetInt32(reader.GetOrdinal("LookUpId")),
                Code = reader.GetString(reader.GetOrdinal("Code")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Level = (IndicatorLevel)reader.GetInt32(reader.GetOrdinal("Level")),
                IndicatorType = (IndicatorType)reader.GetInt32(reader.GetOrdinal("IndicatorType")),
                DataType = (IndicatorDataType)reader.GetInt32(reader.GetOrdinal("DataType")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                Expression = reader.IsDBNull(reader.GetOrdinal("Expression")) ? null : reader.GetString(reader.GetOrdinal("Expression")),
                JsonRule = reader.IsDBNull(reader.GetOrdinal("JsonRule")) ? null : reader.GetString(reader.GetOrdinal("JsonRule")),
                UpdatedOn= reader.IsDBNull(reader.GetOrdinal("updated_on")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_on"))
            };
        }
        private static void AddSaveParameters(DbCommand cmd,IndicatorDto dto,int tenantId, string createdBy)
        {
            cmd.Parameters.Add(new SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new SqlParameter("@ProgramId", dto.ProgramId));
            cmd.Parameters.Add(new SqlParameter("@CreatedBy", createdBy ?? ""));

            cmd.Parameters.Add(new SqlParameter("@Code", dto.Code ?? ""));
            cmd.Parameters.Add(new SqlParameter("@Name", dto.Name ?? ""));
            cmd.Parameters.Add(new SqlParameter("@Level", (int)dto.Level));
            cmd.Parameters.Add(new SqlParameter("@DataType", (int)dto.DataType));

            cmd.Parameters.Add(new SqlParameter("@Description",
                (object?)dto.Description ?? DBNull.Value));

            cmd.Parameters.Add(new SqlParameter("@IsActive", dto.IsActive));

            cmd.Parameters.Add(new SqlParameter("@Expression",
                (object?)dto.Expression ?? DBNull.Value));

            cmd.Parameters.Add(new SqlParameter("@JsonRule",
                (object?)dto.JsonRule ?? DBNull.Value));
        }


        private static void AddSaveParameters(System.Data.Common.DbCommand cmd, IndicatorDto dto)
        {
            //cmd.Parameters.Add(new SqlParameter("@LookUpId", (object?)dto.LookUpId ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@Code", dto.Code ?? ""));
            cmd.Parameters.Add(new SqlParameter("@Name", dto.Name ?? ""));
            cmd.Parameters.Add(new SqlParameter("@Level", (int)dto.Level));
            //cmd.Parameters.Add(new SqlParameter("@IndicatorType", (int)dto.IndicatorType));
            cmd.Parameters.Add(new SqlParameter("@DataType", (int)dto.DataType));
            cmd.Parameters.Add(new SqlParameter("@Description", (object?)dto.Description ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@IsActive", dto.IsActive));
            cmd.Parameters.Add(new SqlParameter("@Expression", (object?)dto.Expression ?? DBNull.Value));
            cmd.Parameters.Add(new SqlParameter("@JsonRule", (object?)dto.JsonRule ?? DBNull.Value));
        }


    }
}
