using BRaVe_Mobile_Backend.Models;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class EnrollmentPacket
    {
        public DataPacketHeader header { get; set; }
        public string activityCode { get; set; }
        public int distributionId { get; set; }
        public string beneficiaryId { get; set; }
    }
}
