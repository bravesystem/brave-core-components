using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Models.data_payload;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class VerifyTemplatesPacket
    {
        public DataPacketHeader header { get; set; }
        public string templates  { get; set; }
    }
}
