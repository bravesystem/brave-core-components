using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDuplicateScoringEngine
    {

        //Task<List<DuplicateMatchResult>> FindDuplicates(List<Household_Document> households,DuplicateCriteriaDefinition criteria);
        Task<Dictionary<int, PredicateEvaluation>> DuplicatePredicatesEvalResults(Member_Document memberA, Household_Document hhA, Member_Document memberB, Household_Document hhB, DuplicateCriteriaDefinition criteria);

        Task<DuplicateMatchResultsViewModel> FindDuplicates(List<Household_Document> households,DuplicateCriteriaDefinition criteria);
    }
}
