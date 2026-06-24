using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Numerics;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IItemsService
    {

        Task<IEnumerable<Surveys>> GetAllSurveyByProgramId(int programId);

        Task CreateSurvey(string UserId, SurveyDto data);

        Task UpdateSurvey(string UserId, SurveyDto data);

        Task DeleteSurvey(string UserId, DeleteSurveyDto data);


    }
}
