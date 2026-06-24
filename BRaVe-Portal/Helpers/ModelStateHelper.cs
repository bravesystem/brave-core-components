using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BRaVe_Portal.Helpers
{
    public static class ModelStateHelper
    {
        public static object[] GetErrors(ModelStateDictionary ModelState)
        {
            var errors = ModelState
                                    .Where(kvp => kvp.Value is { Errors.Count: > 0 })
                                    .Select(kvp => new
                                    {
                                        Field = kvp.Key,
                                        Messages = kvp.Value!.Errors
                                                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                                                                 ? e.Exception?.Message
                                                                 : e.ErrorMessage)
                                                    .Where(m => !string.IsNullOrWhiteSpace(m))
                                                    .ToArray()
                                    })
                                    .ToArray();

            return errors;
        }
    }
}
