using BRaVe_Mobile_Backend.Models;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class DataPacket
    {
        public DataPacketHeader header { get; set; }
        public string payload { get; set; }
    }
}
