namespace BRaVe_Portal.Models.DTOs
{
    public class DistributedItemDto
    {
        public int ItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public bool Received { get; set; }
    }
}
