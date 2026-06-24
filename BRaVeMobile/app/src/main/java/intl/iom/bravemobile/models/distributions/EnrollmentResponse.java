package intl.iom.bravemobile.models.distributions;

public class EnrollmentResponse {

    public EnrollmentResponse(){}
    public EnrollmentResponse(Family family){
        this.beneficiary = family;
    }

    public Family beneficiary;
}
