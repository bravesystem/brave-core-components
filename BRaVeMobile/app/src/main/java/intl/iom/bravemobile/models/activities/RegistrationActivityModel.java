package intl.iom.bravemobile.models.activities;

import android.content.ContentValues;
import android.util.Base64;

import java.util.Date;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.models.CustomDataset;
import intl.iom.bravemobile.models.CustomLookup;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.surveys.Survey;

public class RegistrationActivityModel {

    public String id;
    public String title;
    public String description;
    public String startDate;
    public String endDate;
    public boolean allowRegistration ;
    public boolean allowVerification ;
    public boolean allowDistribution;



    public List<AdminLevelModel> adminLevels; //ok
    public List<AdminLocationModel> adminLocations; //ok
    public List<CustomLookup> lookups; //ok
    public List<CustomDataset> datasets; //ok

    public List<DatapointModel> datapoints; //ok
    public List<DatapointBinding> datapointBindings;

    public List<SurveyModel> surveys; //ok

    public List<Distribution> distributions; //ok
    public List<SurveyBinding> surveyBindings;
    public List<DistributionBinding> distributionBindings;
    public List<String> whitelist;

    public List<PreferenceModel> preferences;
    public List<ConsentModel> consents; //ok
    public List<ConsentBinding> consentBindings;

    public String adminAreas;

    public RegistrationActivityStaging getStagingData()
    {
        RegistrationActivityStaging stg = new RegistrationActivityStaging();

        stg.id = id;
        stg.title = title;
        stg.description = description;
        stg.startDate = startDate;
        stg.endDate = endDate;
        stg.allowRegistration = allowRegistration;
        stg.allowVerification = allowVerification;
        stg.allowDistribution = allowDistribution;
        stg.datapointBindings= datapointBindings;
        stg.surveyBindings = surveyBindings;
        stg.whitelist = whitelist;
        stg.preferences = preferences;
        stg.consentBindings = consentBindings;
        stg.distributionBindings = distributionBindings;
        stg.adminAreas = adminAreas;

        return stg;
    }


}
