using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingDataOutput
    {
        public int AssessmentId { get; set; }
        public int DataOutputId { get; set; } //show select list with dummy data
        public int RecipientId { get; set; } = 99; //show select list with dummy data where 99 match with value Other
        public string? RecipientOfOutput { get; set; }
        public string? DataOutputAndUsageJustification { get; set; }
    }
}