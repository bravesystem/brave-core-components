using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using static BRaVe_Management_Backend.Helpers.KeyVaultSecretNames;

namespace BRaVe_Management_Backend.Services
{
    public class SqlDuplicateRulesetService : IDuplicateRulesetService
    {
        private readonly ILogger<SqlDataLoaderService> _logger;
        private readonly IDuplicateScoringEngine _scoringEngine;
        private readonly ISearchService _searchService;
        private readonly string _connectionString;
        // inject DbContext, etc.

        public SqlDuplicateRulesetService(ISecretProvider secretProvider, ISearchService searchService, IDuplicateScoringEngine scoringEngine, ILogger<SqlDataLoaderService> logger)
        {
            _connectionString = secretProvider.GetSecretAsync(Sql.PrimaryConnection).Result;
            _searchService = searchService;
            _scoringEngine = scoringEngine;
            _logger = logger;
        }

        private (string Prefix, int Number) SplitCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Input cannot be null or empty.", nameof(value));

            var parts = value.Split('_');
            if (parts.Length != 2)
                throw new FormatException("Input must contain exactly one underscore.");

            string prefix = parts[0];
            int number = int.Parse(parts[1]); // safely converts "01" → 1

            return (prefix, number);
        }


        public async Task<PredicateEvaluationViewModel> CompareIndividuals(int tenantId, IndividualPairDto dto)
        {
            PredicateEvaluationViewModel evaluations = new PredicateEvaluationViewModel();

            var criteriaDef = await GetCriteriaDefinitionFromJobIdAsync(tenantId, dto.JobId);

            var (hhId1, memNo1) = SplitCode(dto.MemberId1);
            var (hhId2, memNo2) = SplitCode(dto.MemberId2);

            var (hh1, hh2) =await _searchService.GetAllHouseholdPair(tenantId, hhId1, hhId2);

            var mem1 = hh1.Members.FirstOrDefault(m => m.MemberNo == memNo1);
            var mem2 = hh2.Members.FirstOrDefault(m => m.MemberNo == memNo2);

            evaluations.Evaluations = (await _scoringEngine.DuplicatePredicatesEvalResults(mem1, hh1, mem2, hh2, criteriaDef)).Values.ToList();

            return evaluations;

        }

        public async Task<List<DuplicateRulesetViewModel>> GetAll(int TenantId)
        {
            var list = new List<DuplicateRulesetViewModel>();
            const string sql = @"
            SELECT Id, Name, Description, IsActive, CreatedOn, TenantId
            FROM dbo.tbl_DuplicateRulesets
            WHERE TenantId IS NULL OR TenantId = @TenantId
            ORDER BY CASE WHEN TenantId IS NULL THEN 0 ELSE 1 END, Name";

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenantId", TenantId);

            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

            while (await reader.ReadAsync())
            {
                list.Add(new DuplicateRulesetViewModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                    TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"))
                });
            }

            return list;
        }

        /// <summary>
        /// Returns DuplicateCriteriaDefinition (JSON format) for a ruleset, suitable for use with DuplicateScoringEngine.
        /// </summary>
        public async Task<DuplicateCriteriaDefinition> GetCriteriaDefinitionAsync(int tenantId, int rulesetId)
        {
            var vm = await GetPredicates(tenantId, rulesetId);
            var criteria = new DuplicateCriteriaDefinition();

            foreach (var predicate in vm.Predicates)
            {
                var fields = predicate.Fields
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                criteria.Rules.Add(new DuplicateRule
                {
                    Id = predicate.Id,
                    Name = predicate.Name,
                    Description = predicate.Description,
                    Fields = fields,
                    Op = predicate.OperationName, // e.g. "eq", "fuzzy", "diff_lte"
                    Value = predicate.Value.HasValue ? (double)predicate.Value.Value : null,
                    Score = (double)predicate.Score
                });
            }

            return criteria;
        }

        public async Task<DuplicateCriteriaDefinition> GetCriteriaDefinitionFromJobIdAsync(int tenantId, int jobId)
        {
            var vm = await GetPredicatesFromJobId(tenantId, jobId);
            var criteria = new DuplicateCriteriaDefinition();

            foreach (var predicate in vm.Predicates)
            {
                var fields = predicate.Fields
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                criteria.Rules.Add(new DuplicateRule
                {
                    Id = predicate.Id,
                    Name = predicate.Name,
                    Description = predicate.Description,
                    Fields = fields,
                    Op = predicate.OperationName, // e.g. "eq", "fuzzy", "diff_lte"
                    Value = predicate.Value.HasValue ? (double)predicate.Value.Value : null,
                    Score = (double)predicate.Score
                });
            }

            return criteria;
        }


        /// <summary>
        /// Returns the ruleset and its predicates for the given tenant. Ruleset must be visible to tenant (BuiltIn or tenant-owned).
        /// </summary>
        public async Task<DuplicatePredicateViewModel> GetPredicates(int TenantId, int RulesetId)
        {
            var result = new DuplicatePredicateViewModel { RulesetId = RulesetId };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("sp_GetPredicatesFromRuleId", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@RulesetId", RulesetId);

            
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);


            if (await reader.ReadAsync())
            {

                result.RulesetName = reader.GetString(reader.GetOrdinal("Name"));
                result.TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"));
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Predicates.Add(new DuplicatePredicates
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        Fields = reader.GetString(reader.GetOrdinal("Fields")),
                        OperationCode = reader.GetInt32(reader.GetOrdinal("OperationCode")),
                        OperationName = reader.GetString(reader.GetOrdinal("OperationName")),
                        Value = reader.IsDBNull(reader.GetOrdinal("Value")) ? null : reader.GetDecimal(reader.GetOrdinal("Value")),
                        Score = reader.GetDecimal(reader.GetOrdinal("Score")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"))
                    });
                }
            }

            return result;
        }

        public async Task<DuplicatePredicateViewModel> GetPredicatesFromJobId(int TenantId, int JobId)
        {
            var result = new DuplicatePredicateViewModel { };

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("sp_GetPredicatesFromJobId", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@TenantId", TenantId);
            cmd.Parameters.AddWithValue("@JobId", JobId);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);


            if (await reader.ReadAsync())
            {

                result.RulesetId = reader.GetInt32(reader.GetOrdinal("Id"));
                result.RulesetName = reader.GetString(reader.GetOrdinal("Name"));
                result.TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"));
            }



            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Predicates.Add(new DuplicatePredicates
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                        Fields = reader.GetString(reader.GetOrdinal("Fields")),
                        OperationCode = reader.GetInt32(reader.GetOrdinal("OperationCode")),
                        OperationName = reader.GetString(reader.GetOrdinal("OperationName")),
                        Value = reader.IsDBNull(reader.GetOrdinal("Value")) ? null : reader.GetDecimal(reader.GetOrdinal("Value")),
                        Score = reader.GetDecimal(reader.GetOrdinal("Score")),
                        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId")) ? null : reader.GetInt32(reader.GetOrdinal("TenantId"))
                    });
                }
            }

            return result;
        }


    }
}
