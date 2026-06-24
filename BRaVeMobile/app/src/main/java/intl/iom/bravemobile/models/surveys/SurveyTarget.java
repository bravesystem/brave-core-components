package intl.iom.bravemobile.models.surveys;

public class SurveyTarget {

    public SurveyTarget(String activity_code,String household_id, int individual_id) {
        this.activity_code = activity_code;
        this.household_id = household_id;
        this.individual_id = individual_id;
    }

    public String activity_code;
    public String household_id;
    public int individual_id;
}
