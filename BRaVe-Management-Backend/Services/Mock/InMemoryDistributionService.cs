using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Services.Mock
{
    /// <summary>
    /// In-memory mock implementation of IDistributionservice for testing or development use.
    /// </summary>
    public class InMemoryDistributionService : IDistributionservice
    {
        // In-memory mock data store
     
        private readonly Dictionary<int, List<Distributions>> _db;
        public InMemoryDistributionService()
        {
            // Initialize with mock data
            _db = new Dictionary<int, List<Distributions>>
            {
                {
                    1, new List<Distributions>
                    {
                        new Distributions
                        {
                             Id = 1,
                             ActivityId = 101,
                             ProgramId = 1,
                             Title = "Food Distribution Drive",
                             Type = "Food",
                             Required = true,
                            CreatedByUser = "system",
                            CreatedOn = DateTime.UtcNow.AddDays(-5)
                        },
                        new Distributions
                        {
                            Id = 3,
                            ActivityId = 101,
                            ProgramId = 1,
                            Title = "Sanitation Kit Distribution",
                            Type = "Health",
                            Required = true,
                            CreatedByUser = "system",
                            CreatedOn = DateTime.UtcNow.AddDays(-5)
                        },
                        new Distributions
                        {
                            Id = 2,
                            ActivityId = 101,
                            ProgramId = 1,
                            Title = "School Supplies Distribution",
                            Type = "Education",
                             Required = true,
                            CreatedByUser = "system",
                            CreatedOn = DateTime.UtcNow.AddDays(-5)

                        }
                    }
                }
            };
        }
        

        public Task<IEnumerable<Distributions>> GetAllDistributions(int programId)
        {
            _db.TryGetValue(programId, out var Distributions);
            Distributions ??= new List<Distributions>();
            return Task.FromResult<IEnumerable<Distributions>>(Distributions);
        }


        public Task<Distributions?> GetDistributionsById(int id)
        {
            var Distributions = _db.Values.SelectMany(list => list).FirstOrDefault(d => d.Id == id);
            return Task.FromResult(Distributions);
        }

        public Task CreateDistributions(Distributions NewDistributions)
        {
            if (!_db.ContainsKey(NewDistributions.ProgramId))
                _db[NewDistributions.ProgramId] = new List<Distributions>();

            NewDistributions.Id = _db[NewDistributions.ProgramId].Any() ? _db[NewDistributions.ProgramId].Max(c => c.Id) + 1 : 1;
            NewDistributions.CreatedOn = DateTime.UtcNow;
            _db[NewDistributions.ProgramId].Add(NewDistributions);

            return Task.CompletedTask;
        }


        public Task UpdateDistributions(Distributions updateDistributions)
        {
            if (_db.ContainsKey(updateDistributions.ProgramId))
            {
                var list = _db[updateDistributions.ProgramId];
                var index = list.FindIndex(d => d.Id == updateDistributions.Id);
                if (index >= 0)
                {
                    updateDistributions.UpdatedOn = DateTime.UtcNow;
                    list[index] = updateDistributions;
                }
            }

            return Task.CompletedTask;
        }
    

        public Task DeleteDistributions(int id)
        {
        //    var existing = _db.FirstOrDefault(d => d.Id == id);
        //    if (existing != null)
        //    {
        //        _db.Remove(existing);
        //    }
        //    return Task.CompletedTask;
        throw new NotImplementedException();
        }
    }
}
