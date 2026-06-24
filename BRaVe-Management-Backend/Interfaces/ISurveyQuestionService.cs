using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Numerics;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ISurveyQuestionService
    {
        //FOR SURVEY QUESTIONS
        Task<IEnumerable<SurveyQuestionDto>> GetAllQuestionsBySurveyId(int SurveyId);

        Task<IEnumerable<SurveyQuestionDto>> GetAllQuestionsBySurveyCode(string SurveyCode);

        Task<SurveyQuestionDto?> GetQuestionById(string SurveyCode, int questionId);

        Task CreateSurveyQuestion(string UserId, SurveyQuestionDto data);

        Task UpdateSurveyQuestion(string UserId, SurveyQuestionDto data, int Id, string SurveyCode);

        Task DeleteSurveyQuestion(string UserId,int TenantId, int id, string SurveyCode);


        //FOR SURVEY QUESTION TRANSLATIONS
        Task<List<SurveyQuestionTranslationDto>> GetAllSurveyQuestionTranslations(string surveyCode, int questionId, int tenantId);

        Task CreateSurveyQuestionTranslation(SurveyQuestionTranslationDto data);

        Task UpdateSurveyQuestionTranslation(SurveyQuestionTranslationDto data);

        Task DeleteSurveyQuestionTranslation(SurveyQuestionTranslationDto data);


    }
}
