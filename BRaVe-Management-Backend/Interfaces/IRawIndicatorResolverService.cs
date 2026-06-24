using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IRawIndicatorResolverService
    {
        Task<decimal?> ResolveAsync(string indicatorCode, Guid entityId);
    }
}


