package intl.iom.bravemobile.models.registrations;

import java.util.Map;
import java.util.UUID;

import intl.iom.bravemobile.models.activities.ConsentsFeedback;

public class db_staging_household
{
    public String householdId;
    public int householdNo;
    public UUID uuid;
    public String registrationToken;
    public Integer householdSize;
    public Integer householdType;

    public String gps_coordinates;
    public LocationAddress address;
    public Map<Integer,String> dpAnswers;
    public ConsentsFeedback feedback;

    public int individualNo;
    public String createdBy;
    public long createdOnMs;
    public String updatedBy;
    public long updatedOnMs;

    public db_staging_household(String householdId, int householdNo, UUID uuid, String registrationToken, Integer householdSize, Integer householdType, String gps_coordinates,LocationAddress address,Map<Integer, String> dpAnswers, ConsentsFeedback feedback,
                                int individualNo, String createdBy, String updatedBy, long createdOnMs, long updatedOnMs) {
        this.householdId = householdId;
        this.householdNo = householdNo;
        this.uuid = uuid;
        this.registrationToken = registrationToken;
        this.householdSize = householdSize;
        this.householdType = householdType;
        this.gps_coordinates = gps_coordinates;
        this.address = address;
        this.dpAnswers = dpAnswers;
        this.feedback = feedback;

        this.individualNo = individualNo;

        this.createdBy = createdBy;
        this.updatedBy = updatedBy;
        this.createdOnMs= createdOnMs;
        this.updatedOnMs =updatedOnMs;
    }
}
