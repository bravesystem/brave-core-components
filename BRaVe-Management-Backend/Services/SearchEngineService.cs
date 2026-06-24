using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using BRaVe_Management_Backend.Services.Mock;
using System.Data;
using System.Security.Cryptography.Xml;
using System.Text.Json;

namespace BRaVe_Management_Backend.Services
{
    public class SearchEngineService : ISearchService
    {
        //return HouseholdDocumentSeedData.GetDummyHouseholds();

        private readonly string connectionString;
        private readonly ILogger<SqlRegionService> _logger;

        public SearchEngineService(ISecretProvider secretProvider, ILogger<SqlRegionService> logger)
        {
            connectionString = secretProvider.GetSecretAsync(KeyVaultSecretNames.Sql.PrimaryConnection).Result;
            _logger = logger;

            //_logger.LogInformation("SearchEngineService initialized with connection string from KeyVault");
        }
        public async Task<List<Household_Document>> GetAllHouseholds(int tenantId, DateTime from, DateTime to)
        {
            var households = new Dictionary<string, Household_Document>();
            var membersByHousehold = new Dictionary<string, List<Member_Document>>();

            var surveysByHousehold = new Dictionary<string, List<Survey_Document>>();
            var surveysByMember = new Dictionary<(string HhId, int IndNo), List<Survey_Document>>();

            try
            {

                using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                await connection.OpenAsync();

                using var cmd = connection.CreateCommand();
                cmd.CommandText = "sp_GetHouseholdsDataByDateRange";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", tenantId));
                cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@StartDate", from.Date));
                cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@EndDate", to.Date));

                using var reader = await cmd.ExecuteReaderAsync();

                // Result set 1: Households
                while (await reader.ReadAsync())
                {
                    var hhId = GetString(reader, "HouseholdId");
                    if (string.IsNullOrEmpty(hhId)) continue;

                    households[hhId] = new Household_Document
                    {
                        ActivityId = GetString(reader, "ActivityId"),
                        TenantId = tenantId,
                        HouseholdId = hhId,
                        HouseholdLocation = GetString(reader, "HouseholdLocation"),
                        Address = GetString(reader, "Address"),
                        HouseholdSize = GetInt(reader, "HouseholdSize") ?? 0,
                        ResidenceStatus = GetString(reader, "ResidenceStatus"),
                        RecipientType = GetString(reader, "RecipientType"),
                        GpsCoordinates = null,
                        DataPoints = new List<Datapoint_Document>(),
                        RegisteredOn = GetDateTime(reader, "RegisteredOn"),
                        UpdatedOn = GetDateTime(reader, "UpdatedOn"),
                        Members = new List<Member_Document>(),
                        Surveys = new List<Survey_Document>()
                    };
                    membersByHousehold[hhId] = new List<Member_Document>();
                    surveysByHousehold[hhId] = new List<Survey_Document>();
                }

                // Result set 2: Members
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var hhId = GetString(reader, "HouseholdId");
                        if (string.IsNullOrEmpty(hhId) || !households.TryGetValue(hhId, out _)) continue;

                        var indNo = GetInt(reader, "IndividualNo") ?? 0;
                        var ageVal = GetInt(reader, "Age");

                        var member = new Member_Document
                        {
                            UUID = GetGuiud(reader, "uuid"),
                            MemberNo = indNo,
                            MemberId = GetString(reader, "MemberId") ?? "",
                            LastName = GetString(reader, "LastName") ?? "",
                            FirstName = GetString(reader, "FirstName") ?? "",
                            MiddleName = GetString(reader, "MiddleName"),
                            HouseholdRole = GetString(reader, "HouseholdRole") ?? "",
                            Gender = GetString(reader, "Gender") ?? "",
                            DateOfBirth = GetDateTime(reader, "DateOfBirth"),
                            AgeInYears = ageVal,
                            AgeInMonths = null,
                            AgeInDays = null,
                            PhotoCollected = GetInt(reader, "PhotoCollected") == 1,
                            Photo = GetString(reader, "Photo"),
                            BiometricCollected = GetInt(reader, "BiometricCollected") == 1,
                            Fingerprints = GetString(reader, "Fingerprints"),
                            DataPoints = new List<Datapoint_Document>(),
                            IsDedupChecked = GetInt(reader, "IsDedupChecked") == 1,
                            IsDuplicate = GetInt(reader, "IsDuplicate") == 1,
                            MatchingId = reader["MatchingId"]?.ToString(),
                            MatchedOn = GetDateTime(reader, "MatchedOn"),
                            RegisteredOn = GetDateTime(reader, "RegisteredOn"),
                            UpdatedOn = GetDateTime(reader, "UpdatedOn"),
                            Surveys = new List<Survey_Document>(),
                            Distributions = new List<Distribution_Document>()
                        };

                        membersByHousehold[hhId].Add(member);
                        surveysByMember[(hhId, indNo)] = new List<Survey_Document>();
                    }
                }

                // Result set 3: Survey answers (group by surveyId, HouseholdId, IndividualNo)
                if (await reader.NextResultAsync())
                {
                    var surveyAnswers = new Dictionary<(string HhId, int IndNo, int SurveyId), List<Question_Answer_Document>>();

                    while (await reader.ReadAsync())
                    {
                        var hhId = GetString(reader, "HouseholdId");
                        var indNo = GetInt(reader, "IndividualNo") ?? 0;
                        var surveyId = GetInt(reader, "surveyId") ?? 0;

                        if (string.IsNullOrEmpty(hhId)) continue;

                        var key = (hhId, indNo, surveyId);
                        if (!surveyAnswers.ContainsKey(key))
                            surveyAnswers[key] = new List<Question_Answer_Document>();

                        surveyAnswers[key].Add(new Question_Answer_Document
                        {
                            QuestionId = GetInt(reader, "QuestionId") ?? 0,
                            QuestionText = GetString(reader, "QuestionText") ?? "",
                            Type = GetString(reader, "Type") ?? "",
                            Answer = GetString(reader, "Answer")
                        });
                    }

                    foreach (var (key, answers) in surveyAnswers)
                    {
                        if (key.IndNo == 0) 
                        {
                            if (surveysByHousehold.TryGetValue(key.HhId, out var surveyHHList))
                            {
                                surveyHHList.Add(new Survey_Document
                                {
                                    Id = key.SurveyId,
                                    Title = $"Survey {key.SurveyId}",
                                    Type = answers.FirstOrDefault()?.Type ?? "",
                                    Answers = answers
                                });
                            }

                            continue;
                        }


                        var memberKey = (key.HhId, key.IndNo);
                        
                        if (surveysByMember.TryGetValue(memberKey, out var surveyList))
                        {
                            surveyList.Add(new Survey_Document
                            {
                                Id = key.SurveyId,
                                Title = $"Survey {key.SurveyId}",
                                Type = answers.FirstOrDefault()?.Type ?? "",
                                Answers = answers
                            });
                        }
                    }
                }

                // Attach surveys and to households
                foreach (var (hhId, surveyList) in surveysByHousehold)
                {
                    if (households.TryGetValue(hhId, out var hh))
                    {
                        foreach (var s in surveyList)
                        {
                            hh.Surveys.Add(s);
                        }
                    }
                }

                // Attach members and their surveys to households
                foreach (var (hhId, memberList) in membersByHousehold)
                {
                    if (households.TryGetValue(hhId, out var hh))
                    {
                        foreach (var m in memberList)
                        {
                            var key = (hhId, m.MemberNo);
                            if (surveysByMember.TryGetValue(key, out var surveys))
                                m.Surveys = surveys;
                            hh.Members.Add(m);
                        }
                    }
                }

                return households.Values.ToList();

            }
            catch (Exception e)
            {
                return new List<Household_Document>();
            }
        }
        private static string? GetString(System.Data.Common.DbDataReader reader, string name)
        {
            var i = GetOrdinal(reader, name);
            if (i < 0) return null;
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }

        public async Task<(Household_Document, Household_Document)> GetAllHouseholdPair(int TenantId, string householdIdA, string householdIdB)
        {
            var households = new Dictionary<string, Household_Document>();
            var membersByHousehold = new Dictionary<string, List<Member_Document>>();
            var surveysByMember = new Dictionary<(string HhId, int IndNo), List<Survey_Document>>();

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_GetHouseholdsPair";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", TenantId));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@HouseholdId1", householdIdA));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@HouseholdId2", householdIdB));

            using var reader = await cmd.ExecuteReaderAsync();

            // Result set 1: Households
            while (await reader.ReadAsync())
            {
                var hhId = GetString(reader, "HouseholdId");
                if (string.IsNullOrEmpty(hhId)) continue;

                households[hhId] = new Household_Document
                {
                    ActivityId = GetString(reader, "ActivityId"),
                    TenantId = TenantId,
                    HouseholdId = hhId,
                    HouseholdLocation = GetString(reader, "HouseholdLocation"),
                    Address = GetString(reader, "Address"),
                    HouseholdSize = GetInt(reader, "HouseholdSize") ?? 0,
                    ResidenceStatus = GetString(reader, "ResidenceStatus"),
                    RecipientType = GetString(reader, "RecipientType"),
                    GpsCoordinates = null,
                    DataPoints = new List<Datapoint_Document>(),
                    RegisteredOn = GetDateTime(reader, "RegisteredOn"),
                    UpdatedOn = GetDateTime(reader, "UpdatedOn"),
                    Members = new List<Member_Document>()
                };
                membersByHousehold[hhId] = new List<Member_Document>();
            }

            // Result set 2: Members
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var hhId = GetString(reader, "HouseholdId");
                    if (string.IsNullOrEmpty(hhId) || !households.TryGetValue(hhId, out _)) continue;

                    var indNo = GetInt(reader, "IndividualNo") ?? 0;
                    var ageVal = GetInt(reader, "Age");

                    var member = new Member_Document
                    {
                        UUID = GetGuiud(reader, "uuid"),
                        MemberNo = indNo,
                        MemberId = GetString(reader, "MemberId") ?? "",
                        LastName = GetString(reader, "LastName") ?? "",
                        FirstName = GetString(reader, "FirstName") ?? "",
                        MiddleName = GetString(reader, "MiddleName"),
                        HouseholdRole = GetString(reader, "HouseholdRole") ?? "",
                        Gender = GetString(reader, "Gender") ?? "",
                        DateOfBirth = GetDateTime(reader, "DateOfBirth"),
                        AgeInYears = ageVal,
                        AgeInMonths = null,
                        AgeInDays = null,
                        PhotoCollected = GetInt(reader, "PhotoCollected") == 1,
                        Photo = GetString(reader, "Photo"),
                        BiometricCollected = GetInt(reader, "BiometricCollected") == 1,
                        Fingerprints = GetString(reader, "Fingerprints"),
                        DataPoints = new List<Datapoint_Document>(),
                        IsDedupChecked = GetInt(reader, "IsDedupChecked") == 1,
                        IsDuplicate = GetInt(reader, "IsDuplicate") == 1,
                        MatchingId = reader["MatchingId"]?.ToString(),
                        MatchedOn = GetDateTime(reader, "MatchedOn"),
                        RegisteredOn = GetDateTime(reader, "RegisteredOn"),
                        UpdatedOn = GetDateTime(reader, "UpdatedOn"),
                        Surveys = new List<Survey_Document>(),
                        Distributions = new List<Distribution_Document>()
                    };

                    membersByHousehold[hhId].Add(member);
                    surveysByMember[(hhId, indNo)] = new List<Survey_Document>();
                }
            }

            // Result set 3: Survey answers (group by surveyId, HouseholdId, IndividualNo)
            if (await reader.NextResultAsync())
            {
                var surveyAnswers = new Dictionary<(string HhId, int IndNo, int SurveyId), List<Question_Answer_Document>>();

                while (await reader.ReadAsync())
                {
                    var hhId = GetString(reader, "HouseholdId");
                    var indNo = GetInt(reader, "IndividualNo") ?? 0;
                    var surveyId = GetInt(reader, "surveyId") ?? 0;
                    if (string.IsNullOrEmpty(hhId)) continue;

                    var key = (hhId, indNo, surveyId);
                    if (!surveyAnswers.ContainsKey(key))
                        surveyAnswers[key] = new List<Question_Answer_Document>();

                    surveyAnswers[key].Add(new Question_Answer_Document
                    {
                        QuestionId = GetInt(reader, "QuestionId") ?? 0,
                        QuestionText = GetString(reader, "QuestionText") ?? "",
                        Type = GetString(reader, "Type") ?? "",
                        Answer = GetString(reader, "Answer")
                    });
                }

                foreach (var (key, answers) in surveyAnswers)
                {
                    var memberKey = (key.HhId, key.IndNo);
                    if (surveysByMember.TryGetValue(memberKey, out var surveyList))
                    {
                        surveyList.Add(new Survey_Document
                        {
                            Id = key.SurveyId,
                            Title = $"Survey {key.SurveyId}",
                            Type = answers.FirstOrDefault()?.Type ?? "",
                            Answers = answers
                        });
                    }
                }
            }

            // Attach members and their surveys to households
            foreach (var (hhId, memberList) in membersByHousehold)
            {
                if (households.TryGetValue(hhId, out var hh))
                {
                    foreach (var m in memberList)
                    {
                        var key = (hhId, m.MemberNo);
                        if (surveysByMember.TryGetValue(key, out var surveys))
                            m.Surveys = surveys;
                        hh.Members.Add(m);
                    }
                }
            }



            return (households[householdIdA], households[householdIdB]);
        }

        private static int? GetInt(System.Data.Common.DbDataReader reader, string name)
        {
            var i = GetOrdinal(reader, name);
            if (i < 0) return null;
            if (reader.IsDBNull(i)) return null;
            var val = reader.GetValue(i);
            if (val is int iVal) return iVal;
            if (val is long lVal) return (int)lVal;
            if (val is decimal dVal) return (int)dVal;
            return int.TryParse(val?.ToString(), out var parsed) ? parsed : null;
        }

        private static DateTime? GetDateTime(System.Data.Common.DbDataReader reader, string name)
        {
            var i = GetOrdinal(reader, name);
            if (i < 0) return null;
            return reader.IsDBNull(i) ? null : reader.GetDateTime(i);
        }

        private static Guid GetGuiud(System.Data.Common.DbDataReader reader, string name)
        {
            var i = GetOrdinal(reader, name);
            return reader.GetGuid(i);
        }

        private static int GetOrdinal(System.Data.Common.DbDataReader reader, string name)
        {
            try
            {
                return reader.GetOrdinal(name);
            }
            catch (IndexOutOfRangeException)
            {
                return -1;
            }
        }

        public async Task<RuleDefinition> GetRuleDefinition(int targetingId)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_GetTargetingRuleset";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TargetingId", targetingId));

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return new RuleDefinition();

            var json = reader["CriteriaDefinition"]?.ToString();
            if (string.IsNullOrWhiteSpace(json))
                return new RuleDefinition();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<RuleDefinition>(json, options) ?? new RuleDefinition();

        }

        public async Task<List<TargetingResultDto>> GetSavedScorings(int tenantId, long jobId)
        {
            var results = new List<TargetingResultDto>();

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_GetHouseholdsDataByTargetingJobId";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@TenantId", tenantId));
            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@JobId", jobId));

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(new TargetingResultDto
                {
                    Program = GetString(reader, "Program"),
                    ActivityCode = GetString(reader, "Activity"),
                    HouseholdId = GetString(reader, "HouseholdId"),
                    HouseholdSize = GetInt(reader, "HouseholdSize") ?? 0,
                    FullName = GetString(reader, "fullname"),
                    Age = GetInt(reader, "Age") ?? 0,
                    Gender = GetString(reader, "Gender"),
                    Score = reader["Score"] != DBNull.Value ? Convert.ToDouble(reader["Score"]) : 0,
                    Rank = GetInt(reader, "Rank") ?? 0,
                    DenseRank = GetInt(reader, "DenseRank") ?? 0,
                    RowNumber = GetInt(reader, "RowNumber") ?? 0
                });
            }

            return results;
        }
    }



}
