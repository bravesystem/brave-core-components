using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Helpers
{
    public class DomainHelper
    {

        public static AgeDto CalculateApproximateAge(
            int yearsAtRegistration,
            int monthsAtRegistration,
            int daysAtRegistration,
            DateTime registeredOn,
            DateTime today
        ){
            if (registeredOn > today)
                throw new InvalidOperationException("RegisteredOn cannot be in the future.");

            var tempDob = registeredOn
                .AddYears(-yearsAtRegistration)
                .AddMonths(-monthsAtRegistration)
                .AddDays(-daysAtRegistration);

            return CalculateAgeFromDob(tempDob, today);
        }

        public static AgeDto CalculateAgeFromDob(DateTime dob, DateTime today)
        {
            if (dob > today)
                throw new InvalidOperationException("DateOfBirth cannot be in the future.");

            int years = today.Year - dob.Year;
            int months = today.Month - dob.Month;
            int days = today.Day - dob.Day;

            if (days < 0)
            {
                months--;
                var prevMonth = today.AddMonths(-1);
                days += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            return new AgeDto
            {
                Years = years,
                Months = months,
                Days = days
            };
        }
    }
}
