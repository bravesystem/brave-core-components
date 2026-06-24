namespace BRaVe_Mobile_Backend.Models
{
    public class RegistrationActivityStaging
    {
        public string DeviceId { get; set; }
        public string ActivityCode { get; set; }
        public int TenantId { get; set; }
        public Guid BatchId { get; set; }
        public StagingType Type { get; set; } // household, individual, consent, survey, distribution
        public string Payload { get; set; } // raw JSON

    }

    public enum StagingType
    {
        household = 1, 
        individual = 2, 
        consent = 3, 
        survey = 4, 
        distribution = 5,
        verification = 6,
        del_audit =  7

    }

}
