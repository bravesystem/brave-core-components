namespace BRaVe_Mobile_Backend.Models.data_payload
{
    public class FamilyProfile
    {
        public string householdUuid { get; set; }
        public string hohid { get; set; }
        public string type { get; set; }
        public List<MemberInfo> members { get; set; }
    }

    public class MemberInfo
    {
        public string householdId { get; set; }
        public string uuid { get; set; }
        public int memno { get; set; }
        public string relationship { get; set; }
        public string fullName { get; set; }
        public string gender { get; set; }
        public int age { get; set; }
        public string photoB64 { get; set; }
        public bool has_biometric { get; set; }
        //public string biometricB64 { get; set; } //leave empty for now
        public byte[] template { get; set; } = [];

        public string biometricB64 =>
                (template != null && template.Length > 0)
                    ? Convert.ToBase64String(template)
                    : null;

    }
}
