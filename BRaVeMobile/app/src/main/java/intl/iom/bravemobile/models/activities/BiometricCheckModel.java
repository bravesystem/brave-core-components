package intl.iom.bravemobile.models.activities;

public class BiometricCheckModel {

    public String uuid;

    public int gender;
    public String template;

    public String createdBy;
    public long createdOnMs;

    public BiometricCheckModel(String uuid, int gender, String template, String createdBy, long createdOnMs) {
        this.uuid = uuid;
        this.gender = gender;
        this.template = template;
        this.createdBy = createdBy;
        this.createdOnMs = createdOnMs;
    }
}


