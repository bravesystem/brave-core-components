namespace BRaVe_Management_Backend.Models
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


        public int? MethodOfArchivingAfterProjectClosure { get; set; }
        public string MethodOfArchivingAfterProjectClosureOther { get; set; }
        public int? MethodOfDisposalAfterRetentionPeriod { get; set; }
        public string MethodOfDisposalAfterRetentionPeriodOther { get; set; }


    }
}
