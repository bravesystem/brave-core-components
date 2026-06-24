namespace BRaVe_Portal.Models.DTOs
{
    public class DataProcessingDto
    {
        public int ProgramId { get; set; }
        public bool RequireApprovals { get; set; }
        public string ProgamManagerName { get; set; }
        public string ProgamManagerContacts { get; set; }
    }
}
