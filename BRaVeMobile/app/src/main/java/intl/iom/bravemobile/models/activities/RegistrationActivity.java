package intl.iom.bravemobile.models.activities;

import android.os.Build;

import java.util.Date;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.statics.DataPointType;

public class RegistrationActivity
{
    public RegistrationActivity(String code, String title, String description, Date startDate, Date endDate,
                                List<RegistrationPreference> preferences, List<DataPoint> dataPoints, List<Consent> consents, List<String> whitelist, List<Survey> surveys, List<Distribution> distributions) {
        this.code = code;
        this.title = title;
        this.description = description;
        this.startDate = startDate;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            this.endDate = Optional.ofNullable(endDate);
            this.datapoints = Optional.ofNullable(dataPoints);
            this.preferences = Optional.ofNullable(preferences);
            this.consents = Optional.ofNullable(consents);
        }
        this.whitelist = whitelist;
        this.surveys = surveys;
        this.distributions = distributions;

    }

    public DataPointType getSurveyType(int surveyId)
    {
        for(Survey s : surveys)
            if(s.code == surveyId)
                return s.surveyType;

        return null;
    }

    public String code;
    public String title;
    public String description;
    public Date  startDate;
    public Optional<Date>  endDate;
    //public RegistrationPreferences preferences;
    public Optional<List<RegistrationPreference>> preferences;

    public Optional<List<DataPoint>> datapoints;
    //public Optional<List<Distribution>> distributions;
    public List<Distribution> distributions;
    public Optional<List<Consent>> consents;
    public List<String> whitelist;
    public List<Survey> surveys;


    public boolean allowRegistration ;
    public boolean allowVerification ;
    public boolean allowDistribution;

}
