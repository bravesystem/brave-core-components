namespace BRaVe_Portal.Models.DTOs
{
    public class BiometricMatchWithIndicatorsDto
    {
        public BiometricMatchResultDto Match { get; set; }
        public List<DuplicateIndicatorChecklistDto> DuplicateIndicators { get; set; }

        public List<ProgrammaticDataDto> SourceProgrammaticData { get; set; } = new();

        public List<ProgrammaticDataDto> MatchedProgrammaticData { get; set; } = new();
    }
}
