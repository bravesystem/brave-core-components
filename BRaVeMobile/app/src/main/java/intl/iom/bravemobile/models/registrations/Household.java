package intl.iom.bravemobile.models.registrations;

import android.content.ContentValues;
import android.os.Build;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;

import intl.iom.bravemobile.models.activities.ConsentsFeedback;

public class Household
{
    public String householdId;
    public int householdNo;
    public int individualNo;
    public UUID uuid = UUID.randomUUID();
    public String activityCode;
    public String registrationToken;
    public Integer householdSize;
    public Integer householdType;
    public String gps_coordinates;
    public LocationAddress address = new LocationAddress();
    public Map<Integer,String> dpAnswers;
    public ConsentsFeedback feedback;
    public String createdBy;
    public long createdOnMs;
    public String updatedBy;
    public long updatedOnMs;

    public db_staging_household getStagingData()
    {
        return new db_staging_household(householdId,householdNo,uuid,registrationToken,householdSize,householdType, gps_coordinates, address,dpAnswers, feedback, individualNo, createdBy, updatedBy, createdOnMs, updatedOnMs);
    }

    //non-db fields
    public List<Individual> individuals = new ArrayList<>();
    public boolean fromServer = false; //To know when downloaded from server
    public boolean isReadOnly = false; //If download from server and don't want alteration of the data
    public Date updated_on;
    //private Individual head = null;

    public boolean collect_head_only = false;


    public int individualCount()
    {
        if(individuals==null)
            return 0;

        return individuals.size();
    }

    public Individual getHead()
    {
        if(individuals.size()==0)
            return null;

        for (Individual i: individuals ) {

            if(i.relationship==0)
                return i;

        }

        return null;
    }

    public int getHouseholdSize()
    {

        if(householdSize==null)
            return 0;

        return householdSize;
    }


}
