namespace BRaVe_Management_Backend.DTOs
{
    public class TargetingRunDto
    {
        public int RunId { get; set; }
        public int RuleId { get; set; }
        public IndicatorLevel TargetingLevel { get; set; }
        public DateTime RunOn { get; set; } = DateTime.UtcNow;
        public string RunBy { get; set; } = "MOCK";
    }

}
