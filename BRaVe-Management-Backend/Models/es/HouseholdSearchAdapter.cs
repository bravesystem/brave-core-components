namespace BRaVe_Management_Backend.Models.es
{
    public static class HouseholdSearchAdapter
    {
        public static object ToSearchModel(Household_Document h)
        {
            return new
            {
                tenant_id = h.TenantId,
                household_id = h.HouseholdId,
                household_location = h.HouseholdLocation,
                address = h.Address,
                household_size = h.HouseholdSize,
                residence_status = h.ResidenceStatus,
                recipient_type = h.RecipientType,
                registered_on = h.RegisteredOn,
                updated_on = h.UpdatedOn,

                member_first_names = h.Members.Select(m => m.FirstName),
                member_last_names = h.Members.Select(m => m.LastName),
                member_genders = h.Members.Select(m => m.Gender),
                member_ages_years = h.Members.Select(m => m.AgeInYears ?? 0),

                datapoint_questions = h.DataPoints.Select(d => d.QuestionText),
                datapoint_answers = h.DataPoints.Select(d => d.Answer),

                //survey_titles = h.Surveys.Select(s => s.Title),

                distribution_titles = h.Members
                    .SelectMany(m => m.Distributions)
                    .Select(d => d.Title)
            };
        }
    }

}
