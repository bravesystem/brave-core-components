namespace BRaVe_Management_Backend.DTOs
{
    public enum IndicatorLevel { Household = 0, Individual = 1 }
    public enum IndicatorType { Custom = 1, BuiltIn = 0 }
    public enum IndicatorDataType
    {
        Number = 1, Numeric = 2, String = 3, Boolean = 5, Date = 4, Select_One = 6, Select_Multiple = 7

    }

    public class IndicatorDto
    {
        public int IndicatorId { get; set; }

        public int? ProgramId { get; set; }
        public int? LookUpId { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public IndicatorLevel Level { get; set; }
        public IndicatorType IndicatorType { get; set; }
        public IndicatorDataType DataType { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Expression { get; set; }
        public string? JsonRule { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}