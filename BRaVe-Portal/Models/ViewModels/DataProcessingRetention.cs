using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingRetention
    {
        public int AssessmentId { get; set; }

        public int? BeneficiaryDataYearsAfterProjectClosure { get; set; }
        public int? BeneficiaryDataMonthsAfterProjectClosure { get; set; }
        public string BeneficiaryDataRetentionJustification { get; set; }

        public int? FinancialDataYearsAfterProjectClosure { get; set; }
        public int? FinancialDataMonthsAfterProjectClosure { get; set; }
        public string FinancialDataRetentionJustification { get; set; }

        public int? ConsentFormsYearsAfterProjectClosure { get; set; }
        public int? ConsentFormsMonthsAfterProjectClosure { get; set; }
        public string ConsentFormsRetentionJustification { get; set; }

        public int? MethodOfArchivingAfterProjectClosure { get; set; } //show select list with dummy data where 99 match with value Other
        public string MethodOfArchivingAfterProjectClosureOther { get; set; }
        public int? MethodOfDisposalAfterRetentionPeriod { get; set; } //show select list with dummy data where 99 match with value Other
        public string MethodOfDisposalAfterRetentionPeriodOther { get; set; }

        public bool HasData =>
    BeneficiaryDataYearsAfterProjectClosure.HasValue ||
    BeneficiaryDataMonthsAfterProjectClosure.HasValue ||
    !string.IsNullOrWhiteSpace(BeneficiaryDataRetentionJustification) ||
    FinancialDataYearsAfterProjectClosure.HasValue ||
    FinancialDataMonthsAfterProjectClosure.HasValue ||
    !string.IsNullOrWhiteSpace(FinancialDataRetentionJustification) ||
    ConsentFormsYearsAfterProjectClosure.HasValue ||
    ConsentFormsMonthsAfterProjectClosure.HasValue ||
    !string.IsNullOrWhiteSpace(ConsentFormsRetentionJustification);

    }
}