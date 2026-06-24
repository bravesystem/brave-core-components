public class ConsentBinding
{
    public int ConsentId { get; set; }

    public int Order { get; set; }
    public int Type { get; set; }
    public bool IsRequired { get; set; }

    public ConsentBinding() { }

    public ConsentBinding(int consentId, int order, int type, bool isRequired)
    {
        ConsentId = consentId;
        Order = order;
        Type = type;
        IsRequired = isRequired;
    }

}
