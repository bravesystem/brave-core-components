namespace BRaVe_Portal.Models
{
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public bool IsValid => Errors.Count == 0;
    }
}
