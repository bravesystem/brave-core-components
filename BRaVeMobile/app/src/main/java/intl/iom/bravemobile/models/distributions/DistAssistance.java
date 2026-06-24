package intl.iom.bravemobile.models.distributions;

public class DistAssistance {
    public int distribution;
    public String familyId;
    public int individualId;
    public String data;

    public DistAssistance(int distribution, String familyId, int individualId, String data) {
        this.distribution = distribution;
        this.familyId = familyId;
        this.individualId = individualId;
        this.data = data;
    }
}
