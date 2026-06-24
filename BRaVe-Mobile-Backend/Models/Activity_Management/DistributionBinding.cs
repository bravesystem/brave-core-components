namespace BRaVe_Mobile_Backend.Models
{
    public class DistributionBinding
    {
        public int DistributionId { get; set; }
        public int type { get; set; }
        public int mode { get; set; }

        public bool photoConfirmation { get; set; } = true;

        public bool biometricConfirmation { get; set; } = true;

        public DistributionBinding() { }

        /*public DistributionBinding(int distributionId, int type)
        {
            DistributionId = distributionId;
            this.type = type;
        }*/
    }
}