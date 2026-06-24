using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IScoringEngine
    {
        Task<List<ScoredResult>> SearchAsync(List<Household_Document> documents, RuleDefinition criteria);
    

    }
}
