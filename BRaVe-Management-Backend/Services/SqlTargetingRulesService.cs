using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SqlTargetingRulesService : ITargetingRulesService
    {
        private readonly string connectionString;
        private readonly ILogger<SqlTargetingRulesService> _logger;

        public SqlTargetingRulesService(ISecretProvider secretProvider, ILogger<SqlTargetingRulesService> logger)
        {
            _logger = logger;
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
        }

        // ===================== CREATE =====================
        public async Task<int> CreateAsync(TargetingRuleDto dto)
        {
            _logger.LogInformation(
                "Starting CreateAsync for TargetingRule. RuleName={RuleName}, TenantId={TenantId}",
                dto.RuleName,
                dto.TenantId);

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.sp_CreateTargetingRule", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            try
            {
                // Log DTO state
                _logger.LogDebug(
                    "CreateAsync DTO Data: {@Dto}",
                    dto);

                // Add parameters
                cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
                cmd.Parameters.AddWithValue("@ProgramId", dto.ProgramId);
                cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy ?? "");
                cmd.Parameters.AddWithValue("@RuleName", dto.RuleName ?? "");
                cmd.Parameters.AddWithValue("@Description", dto.Description ?? "");
                cmd.Parameters.AddWithValue("@RuleJson", dto.RuleJson ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@WeightBound", dto.WeightBound);
                cmd.Parameters.AddWithValue("@TotalWeight", dto.TotalWeight ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Code", dto.Code ?? "");

                // Log parameters
                foreach (SqlParameter param in cmd.Parameters)
                {
                    _logger.LogDebug(
                        "SQL Param: {Name} = {Value}",
                        param.ParameterName,
                        param.Value ?? "NULL");
                }

                _logger.LogInformation("Opening SQL connection...");
                await conn.OpenAsync();

                _logger.LogInformation("Executing stored procedure sp_CreateTargetingRule...");

                var result = await cmd.ExecuteScalarAsync();

                var ruleId = Convert.ToInt32(result);

                _logger.LogInformation(
                    "TargetingRule created successfully. RuleId={RuleId}",
                    ruleId);

                return ruleId;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL Error while creating TargetingRule. RuleName={RuleName}, TenantId={TenantId}",
                    dto.RuleName,
                    dto.TenantId);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error in CreateAsync for TargetingRule. RuleName={RuleName}",
                    dto.RuleName);

                throw;
            }
        }

        // ===================== READ =====================
        public async Task<IEnumerable<TargetingRuleDto>> GetAllAsync(int TenantId)
        {
            const string spName = "dbo.sp_GetTargetingRules";
            var rules = new List<TargetingRuleDto>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand(spName, conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@TenantId", TenantId);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new RuleItemConverter() }
                };

                while (await reader.ReadAsync())
                {
                    var rule = MapRule(reader);

                    if (!string.IsNullOrWhiteSpace(rule.RuleJson))
                    {
                        try
                        {
                            var wrappedJson = $"{{ \"combinator\": \"AND\", \"rules\": {rule.RuleJson} }}";
                            rule.Criteria = JsonSerializer.Deserialize<RuleGroupDto>(wrappedJson, options) ?? new RuleGroupDto();
                        }
                        catch
                        {
                            rule.Criteria = new RuleGroupDto();
                        }
                    }
                    else
                    {
                        rule.Criteria = new RuleGroupDto();
                    }

                    rules.Add(rule);
                }

                return rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching targeting rules");
                throw;
            }
        }

        public async Task<TargetingRuleDto?> GetByIdAsync(int ruleId)
        {
            _logger.LogInformation("Fetching TargetingRule by Id={RuleId}", ruleId);

            try
            {
                using var conn = new SqlConnection(connectionString);
                using var cmd = new SqlCommand("dbo.sp_GetTargetingRuleById", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Id", ruleId);

                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened for GetByIdAsync");

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("TargetingRule found for Id={RuleId}", ruleId);
                    return MapRule(reader);
                }

                _logger.LogWarning("No TargetingRule found for Id={RuleId}", ruleId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TargetingRule by Id={RuleId}", ruleId);
                throw;
            }
        }



        // ===================== UPDATE =====================
        public async Task UpdateAsync(int ruleId, TargetingRuleDto dto)
        {
            _logger.LogInformation("Updating TargetingRule Id={RuleId}", ruleId);
            _logger.LogDebug("Update payload: {@Dto}", dto);

            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand("dbo.sp_UpdateTargetingRule", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@RuleId", SqlDbType.Int).Value = ruleId;
            cmd.Parameters.Add("@RuleName", SqlDbType.NVarChar, 200).Value = dto.RuleName ?? string.Empty;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500).Value = dto.Description ?? string.Empty;
            cmd.Parameters.Add("@WeightBound", SqlDbType.Bit).Value = dto.WeightBound;
            cmd.Parameters.Add("@IsValid", SqlDbType.Bit).Value = dto.IsValid;
            cmd.Parameters.Add("@TotalWeight", SqlDbType.Decimal).Value = dto.TotalWeight ?? (object)DBNull.Value;
            cmd.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = dto.Code ?? string.Empty;

            try
            {
                await conn.OpenAsync();
                _logger.LogDebug("SQL connection opened for UpdateAsync");

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Update completed for RuleId={RuleId}. RowsAffected={RowsAffected}",
                    ruleId, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating RuleId={RuleId}", ruleId);
                throw;
            }
        }



        // ===================== DELETE =====================
        public async Task DeleteAsync(int ruleId)
        {
            _logger.LogInformation("Deleting TargetingRule Id={RuleId}", ruleId);

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.sp_DeleteTargetingRule", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@RuleId", ruleId);

            await conn.OpenAsync();
            _logger.LogDebug("SQL connection opened for DeleteAsync");

            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation("Delete completed for RuleId={RuleId}", ruleId);
        }


        // ===================== MAPPER =====================
        private static TargetingRuleDto MapRule(SqlDataReader reader)
        {
            var programIdOrdinal = reader.GetOrdinal("ProgramId");

            return new TargetingRuleDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),

                ProgramId = reader.IsDBNull(programIdOrdinal)
                    ? (int?)null
                    : reader.GetInt32(programIdOrdinal),

                Code = reader["Code"]?.ToString() ?? string.Empty,

                TenantId = reader.GetInt32(reader.GetOrdinal("TenantId")),
                
                WeightBound = reader["WeightBound"] != DBNull.Value && reader.GetBoolean(reader.GetOrdinal("WeightBound")),

                IsValid = reader["IsValid"] != DBNull.Value && (bool)reader["IsValid"],

                IsCustom = reader["IsCustom"] != DBNull.Value && (bool)reader["IsCustom"],

                RuleName = reader["RuleName"]?.ToString() ?? string.Empty,

                Description = reader["Description"]?.ToString(),

                CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),

                CreatedBy = reader["CreatedBy"]?.ToString() ?? string.Empty,

                TotalWeight = reader["TotalWeight"] as decimal?
            };
        }


        // ===================== OTHER HELPERS =====================
        public async Task<IEnumerable<string>> GetDistinctValuesForFieldAsync(string displayName)
        {
            var columnMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "FirstName", "i.firstName" },
                { "LastName", "i.lastName" },
                { "AgeInYears", "i.ageInYears" },
                { "Relationship", "i.relationship" },
                { "Gender", "i.gender" },
                { "HouseholdNo", "h.householdNo" },
                { "HouseholdSize", "h.householdSize" }
            };

            if (!columnMapping.TryGetValue(displayName, out var columnName))
                throw new ArgumentException($"Unknown field: {displayName}");

            var results = new List<string>();
            var sql = $@"
                SELECT DISTINCT {columnName} AS Value
                FROM dbo.tbl_Individuals i
                INNER JOIN dbo.tbl_Households h ON i.householdId = h.uuid
                WHERE {columnName} IS NOT NULL
                ORDER BY {columnName};
            ";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                results.Add(reader[0]?.ToString());

            return results;
        }

        public async Task<IEnumerable<TargetingPreviewDto>> PreviewAsync(JsonElement ruleJson)
        {
            var whereClause = TargetingRuleSqlBuilder.Build(ruleJson);

            var sql = $@"
                SELECT DISTINCT TOP 10
                    i.FirstName,
                    i.LastName,
                    i.ageInYears,
                    i.Relationship,
                    i.gender
                FROM dbo.tbl_Individuals i
                INNER JOIN dbo.tbl_Households h 
                    ON i.householdId = h.uuid
                WHERE {whereClause};
            ";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<TargetingPreviewDto>();
            while (await reader.ReadAsync())
            {
                results.Add(new TargetingPreviewDto
                {
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    AgeInYears = reader.GetInt32(reader.GetOrdinal("ageInYears")),
                    Relationship = reader["Relationship"].ToString()
                });
            }

            return results;
        }

        public async Task<List<TargetingFieldsDto>> GetAllTargetingFieldsAsync(int tenantId)
        {
            var sql = @"
    SELECT
        tf.FieldCode,
        tf.DisplayName,
        tf.DataType,
        tf.TenantId,
        tf.LookUpId,
        lv.Id AS LookupValueId,
        lv.Text AS LookupText,
        lv.[Order],
        lv.IsActive,
        lv.IsCustom,
        lv.CreatedOn
    FROM dbo.lkp_TargetingFields tf
    LEFT JOIN dbo.tbl_CustomLookupTableValues lv
        ON lv.LookupId = tf.LookUpId
       AND lv.TenantId = tf.TenantId
       AND lv.IsActive = 1
    WHERE tf.TenantId = @TenantId
    ORDER BY tf.FieldCode, lv.[Order], lv.Text;
";

            var tempDict = new Dictionary<string, TargetingFieldsDto>();

            await using var conn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenantId", tenantId);

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var fieldCode = reader["FieldCode"].ToString()!;
                if (!tempDict.TryGetValue(fieldCode, out var dto))
                {
                    dto = new TargetingFieldsDto
                    {
                        FieldCode = fieldCode,
                        DisplayName = reader["DisplayName"].ToString(),
                        DataType = (int)reader["DataType"],
                        TenantId = (int)reader["TenantId"],
                        LookUpId = reader["LookUpId"] as int?,
                        Values = new List<TargetingFieldValueDto>()
                    };
                    tempDict[fieldCode] = dto;
                }

                if (reader["LookupValueId"] != DBNull.Value)
                {
                    dto.Values.Add(new TargetingFieldValueDto
                    {
                        Id = (int)reader["LookupValueId"],
                        Name = reader["LookupText"]?.ToString() ?? string.Empty
                    });
                }
            }

            // Remove duplicates based on Id
            foreach (var dto in tempDict.Values)
            {
                dto.Values = dto.Values
                    .GroupBy(v => v.Id)
                    .Select(g => g.First())
                    .ToList();
            }

            return tempDict.Values.ToList();
        }
        public async Task EnrollBeneficiariesAsync(EnrollBeneficiariesDto dto)
        {
            _logger.LogInformation(
                "Starting enrollment. TenantId={TenantId}, DistributionId={DistributionId}, HouseholdCount={HouseholdCount}, CreatedBy={CreatedBy}",
                dto.TenantId,
                dto.DistributionId,
                dto.HouseholdIds?.Count ?? 0,
                dto.CreatedBy
            );

            using var conn = new SqlConnection(connectionString);
            //using var cmd = new SqlCommand("dbo.sp_ManualEnrollBeneficiaries", conn)
            using var cmd = new SqlCommand("dbo.sp_EnrollBeneficiaries", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Build TVP
            var table = new DataTable();
            table.Columns.Add("value", typeof(string));


            foreach (var id in dto.HouseholdIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning(
                        "Empty HouseholdId skipped during enrollment. TenantId={TenantId}",
                        dto.TenantId
                    );
                    continue;
                }

                table.Rows.Add(id);
            }

            if (table.Rows.Count == 0)
            {
                _logger.LogWarning(
                    "Enrollment aborted: all provided HouseholdIds were invalid. TenantId={TenantId}, DistributionId={DistributionId}",
                    dto.TenantId,
                    dto.DistributionId
                );

                throw new ValidationException("No valid household IDs provided.");
            }

            // Parameters
            cmd.Parameters.AddWithValue("@TenantId", dto.TenantId);
            cmd.Parameters.AddWithValue("@DistributionId", dto.DistributionId);
            cmd.Parameters.AddWithValue("@TargetingId", dto.TargetingId.HasValue? dto.TargetingId.Value: DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

            var tvp = cmd.Parameters.AddWithValue("@Households", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "dbo.StringList";


            try
            {
                await conn.OpenAsync();

                _logger.LogDebug(
                    "Executing sp_EnrollBeneficiaries. TenantId={TenantId}, DistributionId={DistributionId}, HouseholdCount={HouseholdCount}",
                    dto.TenantId,
                    dto.DistributionId,
                    table.Rows.Count
                );

                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                    "Enrollment completed successfully. TenantId={TenantId}, DistributionId={DistributionId}, EnrolledCount={HouseholdCount}",
                    dto.TenantId,
                    dto.DistributionId,
                    table.Rows.Count
                );
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "SQL error during enrollment. TenantId={TenantId}, DistributionId={DistributionId}, HouseholdCount={HouseholdCount}",
                    dto.TenantId,
                    dto.DistributionId,
                    table.Rows.Count
                );

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during enrollment. TenantId={TenantId}, DistributionId={DistributionId}",
                    dto.TenantId,
                    dto.DistributionId
                );

                throw;
            }
        }


 
    }
}
