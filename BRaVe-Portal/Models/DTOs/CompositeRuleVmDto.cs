
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{

    public class CompositeRuleVmDto
    {
        public string LeftExpression { get; set; } = "";
        public string Operator { get; set; } = "<";
        public decimal CompareValue { get; set; }
        public decimal ResultValue { get; set; }

    }
}