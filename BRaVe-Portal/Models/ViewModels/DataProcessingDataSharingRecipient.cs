using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingDataSharingRecipient
    {
        public int AssessmentId { get; set; }
        public int RecipientId { get; set; } //show select list with dummy data where 99 match with value Other
        public string? RecipientOther { get; set; }
        public int TypeOfAgreementId { get; set; } //show select list with dummy data where 99 match with value Other
        public string? TypeOfAgreementOther { get; set; }
        public int StatusOfAgreementId { get; set; } //show select list with dummy
        public string? PurposeOfDataSharing { get; set; }
    }
}