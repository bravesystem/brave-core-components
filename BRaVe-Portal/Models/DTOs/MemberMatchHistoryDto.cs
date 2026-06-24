namespace BRaVe_Portal.Models.DTOs
{
    public class MemberMatchHistoryDto
    {
        public Guid SelectedMemberUuid { get; set; }

        public Guid OtherMemberUuid { get; set; }

        public string OtherMemberName { get; set; } = string.Empty;

        public int? OtherMemberAge { get; set; }

        public string OtherMemberGender { get; set; } = string.Empty;

        public string OtherMemberStatus { get; set; } = string.Empty;

        public string? OtherMemberGps { get; set; } = string.Empty;

        public string? ActivityCode { get; set; } = string.Empty;

        public long AdjudicationId { get; set; }

        public DateTime AdjudicatedOn { get; set; }
    }
}
