namespace BRaVe_Mobile_Backend.Models
{
    public class DatapointBinding
    {
        public int DatapointId { get; set; }
        public int type { get; set; }
        public bool IsRequired { get; set; }

        public DatapointBinding(int datapointId, int type, bool isRequired)
        {
            DatapointId = datapointId;
            this.type = type;
            IsRequired = isRequired;
        }
    }
}