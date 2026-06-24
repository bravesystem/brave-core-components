package intl.iom.bravemobile.models.activities;

import java.util.Date;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.models.CustomLookup;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Location;
import intl.iom.bravemobile.models.surveys.Survey;

public class RegistrationActivityStaging {

    public String id;
    public String title;
    public String description;
    public String startDate;
    public String endDate;

    public boolean allowRegistration ;
    public boolean allowVerification ;
    public boolean allowDistribution;
    public String adminAreas;
    public List<DatapointBinding> datapointBindings;
    public List<SurveyBinding> surveyBindings;
    public List<DistributionBinding> distributionBindings;
    public List<String> whitelist;

    public List<PreferenceModel> preferences;
    public List<ConsentBinding> consentBindings;

}
