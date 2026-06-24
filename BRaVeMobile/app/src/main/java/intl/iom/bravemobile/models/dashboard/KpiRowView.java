package intl.iom.bravemobile.models.dashboard;

public final class KpiRowView {
    public final int householdCount;
    public final int individualCount;
    public final int requiredSurveysNotCollected;
    public final int assistanceHouseholdCount;
    public final int assistanceIndividualCount;

    public KpiRowView(int householdCount,
                      int individualCount,
                      int requiredSurveysNotCollected,
                      int assistanceHouseholdCount,
                      int assistanceIndividualCount) {
        this.householdCount = householdCount;
        this.individualCount = individualCount;
        this.requiredSurveysNotCollected = requiredSurveysNotCollected;
        this.assistanceHouseholdCount = assistanceHouseholdCount;
        this.assistanceIndividualCount = assistanceIndividualCount;
    }
}
