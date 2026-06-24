using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Services.Mock
{


    public class HouseholdDocumentSeedData
    {
        public static List<Household_Document> GetDummyHouseholds()
        {
            var baseDate = new DateTime(2024, 1, 15);
            var random = new Random(42);

            var households = new List<Household_Document>();

            var lastNames = new[] { "Johnson", "Smith", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson" };
            var firstNames = new[] { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa", "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra" };
            var locations = new[] { "Sector A", "Sector B", "Sector C", "Block 1", "Block 2", "Zone North", "Zone South", "Camp Alpha", "Camp Beta", "Settlement East", "Settlement West", "Area 1", "Area 2", "District 3", "District 4" };
            var residenceStatuses = new[] { "Rented", "Owned", "Temporary", "Displaced", "Host family", "Camp resident" };
            var recipientTypes = new[] { "Beneficiary", "IDP", "Refugee", "Returnee", "Host community" };
            var householdRoles = new[] { "Head", "Spouse", "Child", "Parent", "Sibling", "Other" };
            var genders = new[] { "M", "F" };

            for (int i = 1; i <= 30; i++)
            {
                var tenantId = (i % 3) + 1;
                var householdId = $"HH-{tenantId:D2}-{i:D4}";
                var householdSize = random.Next(1, 6);
                var regDate = baseDate.AddDays(random.Next(-180, 90));

                var members = new List<Member_Document>();
                for (int m = 0; m < householdSize; m++)
                {
                    var lastName = lastNames[(i + m) % lastNames.Length];
                    var firstName = firstNames[(i * 2 + m) % firstNames.Length];
                    var dob = regDate.AddYears(-random.Next(5, 60)).AddMonths(-random.Next(0, 12));

                    members.Add(new Member_Document
                    {
                        MemberNo = m + 1,
                        MemberId = $"{householdId}-M{m + 1}",
                        LastName = lastName,
                        FirstName = firstName,
                        MiddleName = m == 0 ? null : "A.",
                        HouseholdRole = householdRoles[Math.Min(m, 5)],
                        Gender = genders[m % 2],
                        DateOfBirth = dob,
                        AgeInYears = null,
                        AgeInMonths = null,
                        AgeInDays = null,
                        PhotoCollected = false,
                        Photo = null,
                        BiometricCollected = false,
                        Fingerprints = null,
                        DataPoints = new List<Datapoint_Document>(),
                        IsDuplicate = false,
                        MatchingId = null,
                        MatchedOn = null,
                        RegisteredOn = regDate,
                        UpdatedOn = regDate.AddDays(random.Next(0, 30)),
                        //Surveys = new List<Survey_Document>(),
                        Surveys = GetMemberSurveys(random),
                        Distributions = new List<Distribution_Document>()
                    });
                }

                households.Add(new Household_Document
                {
                    TenantId = tenantId,
                    HouseholdId = householdId,
                    HouseholdLocation = $"{locations[i % locations.Length]} - Unit {i}",
                    Address = $"{100 + i} Main Street, {locations[i % locations.Length]}",
                    HouseholdSize = householdSize,
                    ResidenceStatus = residenceStatuses[i % residenceStatuses.Length],
                    RecipientType = recipientTypes[i % recipientTypes.Length],
                    GpsCoordinates = new Coordinates
                    {
                        Latitude = 12.0 + (random.NextDouble() * 5),
                        Longitude = 8.0 + (random.NextDouble() * 5),
                        Accuracy = random.NextDouble() * 10
                    },
                    DataPoints = new List<Datapoint_Document>(),
                    RegisteredOn = regDate,
                    UpdatedOn = regDate.AddDays(random.Next(0, 30)),
                    Members = members,
                   // Surveys = new List<Survey_Document>()
                });
            }

            return households;
        }

        private static List<Survey_Document> GetMemberSurveys(Random random)
        {
            return new List<Survey_Document>
            {
                new Survey_Document
                {
                    Id = 101,
                    Title = "Survey 101",
                    Type = "Boolean",
                    Answers = new List<Question_Answer_Document>
                    {
                        new Question_Answer_Document
                        {
                            QuestionId = 10101,
                            QuestionText = "Do you have access to clean water?",
                            Type = "Boolean",
                            Answer = random.Next(2) == 1 ? "true" : "false"
                        }
                    }
                },
                new Survey_Document
                {
                    Id = 102,
                    Title = "Survey 102",
                    Type = "Mixed",
                    Answers = new List<Question_Answer_Document>
                    {
                        new Question_Answer_Document
                        {
                            QuestionId = 10201,
                            QuestionText = "What is your occupation?",
                            Type = "Text",
                            Answer = new[] { "Farmer", "Teacher", "Trader", "Unemployed", "Student", "Health worker" }[random.Next(6)]
                        },
                        new Question_Answer_Document
                        {
                            QuestionId = 10202,
                            QuestionText = "How many years have you lived here?",
                            Type = "Numeric",
                            Answer = random.Next(1, 25).ToString()
                        }
                    }
                }
            };
        }
        public static List<ScoredResult> GetRandomScoredResults(List<Household_Document> households)
        {
            if (households == null || households.Count == 0)
                return new List<ScoredResult>();

            var random = new Random();
            var results = households
                .Select(h => new ScoredResult
                {
                    DocumentId = h.HouseholdId,
                    Score = random.NextDouble() * 100
                })
                .OrderByDescending(r => r.Score)
                .ToList();

            int rank = 0;
            int denseRank = 0;
            double? prevScore = null;

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                r.RowNumber = i + 1;

                if (prevScore == null || r.Score < prevScore)
                {
                    rank = i + 1;
                    denseRank++;
                    prevScore = r.Score;
                }
                r.Rank = rank;
                r.DenseRank = denseRank;
            }

            return results.OrderBy(_ => random.Next()).ToList();
        }


    }
}
