using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Numerics;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ISurveyService
    {
    
        Task<IEnumerable<Surveys>> GetAllSurveyByProgramId(int tenantId, int programId);

        Task<IEnumerable<Surveys>> GetAllSurveys(string userId, int tenantId);

        Task<Surveys?> GetSurveyById(int surveyId);

        Task CreateSurvey(string UserId, SurveyDto data);

        Task UpdateSurvey(string UserId, SurveyDto data);

        Task ActivateSurvey(string UserId, int SurveyId);

        Task DeleteSurvey(string UserId, DeleteSurveyDto data);


    }
}
