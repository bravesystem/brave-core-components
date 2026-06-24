package intl.iom.bravemobile.models.distributions;

public class EnrollmentRequest
{
    public int distributionId;
    public String beneficiaryId;

    public EnrollmentRequest( int distributionId, String beneficiaryId)
    {
        this.distributionId = distributionId;
        this.beneficiaryId = beneficiaryId;
    }
}
