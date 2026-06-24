package intl.iom.bravemobile.models.activities;

public class DistributionAssistance {

    public int distributionId;
    public String householdId;
    public int individualId;
    public String data;

    public DistributionAssistance(int distributionId, String householdId, int individualId, String data) {
        this.distributionId = distributionId;
        this.householdId = householdId;
        this.individualId = individualId;
        this.data = data;
    }

    //query = "CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(
    // activity_code TEXT, id INT, household_id TEXT,
    // individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
    //        db.execSQL(query);
}
