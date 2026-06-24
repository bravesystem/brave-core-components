namespace BRaVe_Mobile_Backend.Models
{
    public class RegistrationActivity
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; } 
        public DateTime? EndDate { get; set; }
        public bool AllowRegistration { get; set; } = true;
        public bool AllowVerification { get; set; } = true;
        public bool AllowDistribution => Distributions.Count > 0;
        public string AdminAreas { get; set; }
        public List<AdminLevel> AdminLevels { get; set; } = new(); //ok
        public List<AdminLocation> AdminLocations { get; set; } = new(); //ok
        public List<CustomLookup>  Lookups { get; set; } = new(); //ok
        public List<CustomDataset>  Datasets { get; set; } //ok
        public List<Datapoint> Datapoints { get; set; } = new(); //ok
        public List<DatapointBinding> DatapointBindings { get; set; } = new(); //ok
        public List<Survey> Surveys { get; set; } = new(); //ok
        public List<SurveyBinding> SurveyBindings { get; set; } = new(); //ok
        public List<Distribution> Distributions { get; set; } = new();
        public List<DistributionBinding> DistributionBindings { get; set; } = new(); //ok
        public List<string> Whitelist { get; set; } = new(); //ok
        public List<ActivityPreference> Preferences { get; set; } = new();//ok
        public List<Consent> Consents { get; set; } = new();//ok
        public List<ConsentBinding> ConsentBindings { get; set; } = new(); //ok

    }
}
