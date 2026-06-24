package intl.iom.bravemobile.models.registrations;

import java.util.Date;
import java.util.Map;
import java.util.UUID;

import intl.iom.bravemobile.models.activities.ConsentsFeedback;

public class db_staging_individual {
    public int individualId;
    public UUID uuid;
    public String firstName;
    public String middleName;
    public String lastName;
    public Date dob;
    public Integer ageInYears;
    public Integer ageInMonths;
    public Integer ageInDays;
    public Integer relationship;
    public Integer gender;
    public String photoBase64;

    public BiometricNotCollected biometricNotCollected;
    public String biometricBase64;

    public Map<Integer,String> dpAnswers;


    public String householdId;
    public ConsentsFeedback feedback;
    public String createdBy;
    public long createdOnMs;
    public String updatedBy;
    public long updatedOnMs;

    public db_staging_individual(String householdId, int individualId, UUID uuid, String firstName, String middleName, String lastName, Date dob, Integer ageInYears, Integer ageInMonths, Integer ageInDays, Integer relationship, Integer gender, String photoBase64, String biometricBase64, BiometricNotCollected biometricNotCollected, Map<Integer, String> dpAnswers
            ,ConsentsFeedback feedback, String createdBy, String updatedBy, long createdOnMs, long updatedOnMs) {
        this.householdId = householdId;
        this.individualId = individualId;
        this.uuid = uuid;
        this.firstName = firstName;
        this.middleName = middleName;
        this.lastName = lastName;
        this.dob = dob;
        this.ageInYears = ageInYears;
        this.ageInMonths = ageInMonths;
        this.ageInDays = ageInDays;
        this.relationship = relationship;
        this.gender = gender;
        this.photoBase64 = photoBase64;
        this.biometricBase64 = biometricBase64;
        this.biometricNotCollected = biometricNotCollected;
        this.dpAnswers = dpAnswers;

        this.feedback = feedback;
        this.createdBy = createdBy;
        this.updatedBy = updatedBy;
        this.createdOnMs= createdOnMs;
        this.updatedOnMs =updatedOnMs;
    }
}
