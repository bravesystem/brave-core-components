package intl.iom.bravemobile.models.registrations;

import android.os.Build;

import java.util.Date;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

import intl.iom.bravemobile.models.activities.ConsentsFeedback;

public class Individual
{
    public int individualId;
    public UUID uuid = UUID.randomUUID();
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

    public int  deleted;
    public Date updated_on;

    public String householdId;
    public ConsentsFeedback feedback;
    public String createdBy;
    public long createdOnMs;
    public String updatedBy;
    public long updatedOnMs;


    public db_staging_individual getStagingData()
    {
        return new db_staging_individual(householdId,individualId,uuid,firstName,middleName,lastName,dob,ageInYears,ageInMonths,ageInDays,relationship,gender,photoBase64, biometricBase64, biometricNotCollected ,dpAnswers,feedback, createdBy, updatedBy, createdOnMs, updatedOnMs);
    }

    public boolean isBiometricCollected()
    {
        return biometricBase64!=null && !biometricBase64.isEmpty();
    }

    public boolean isPhotoCollected()
    {
        return photoBase64!=null && !photoBase64.isEmpty();
    }


    public static Individual getEmpty()
    {
        Individual ind = new Individual();
        ind.firstName = "-";
        ind.middleName ="-";
        ind.lastName = "-";
        ind.relationship = -1;
        ind.dob = null;
        ind.ageInYears = null;
        ind.ageInMonths = null;
        ind.ageInDays = null;
        ind.gender = -1;
        ind.photoBase64 = null;
        ind.biometricBase64 = null;
        return ind;
    }

}