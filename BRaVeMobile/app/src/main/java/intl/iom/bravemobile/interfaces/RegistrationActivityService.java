package intl.iom.bravemobile.interfaces;

import android.os.Build;

import java.security.cert.PKIXRevocationChecker;
import java.util.List;
import java.util.Date;
import java.util.Optional;

import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.PreferenceHelper;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.activities.DatapointBinding;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.activities.RegistrationActivityModel;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.statics.AnswerType;

public interface RegistrationActivityService {

    String getCurrent();
    void setCurrent(String code);

    SurveyTarget getSurveyTarget();
    void setSurveyTarget(SurveyTarget target);
    List<Survey> getSurveys(String code);
    List<Distribution> getDistributions(String code);
    List<DatasetColumn> getDatasetColumns(int datasetId);
    default Survey getSurveyById(String activityCode, int surveyCode){

        for(Survey s : getSurveys(activityCode))
        {
            if(s.code==surveyCode)
                return s;
        }

        return null;

    }
    default Distribution getDistributionById(String activityCode, int distributionId){

        for(Distribution d : getDistributions(activityCode))
        {
            if(d.distributionId==distributionId)
                return d;
        }

        return null;

    }
    List<RegistrationActivity> getAll();
    Optional<RegistrationActivity> getRegistrationActivity(String code) throws RegistrationActivityNotFound;

    default boolean isActive(String code) throws RegistrationActivityNotFound
    {
        Optional<RegistrationActivity> regActivity = getRegistrationActivity(code);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            if(regActivity.isPresent())
            {
                RegistrationActivity data = regActivity.get();
                Date now = new Date();
                return !data.endDate.isPresent() || now.before(data.endDate.get());
            }
        }

        return false;
    }

    default Object getPreference(String activityCode, String preferenceName,  Object defaultValue) throws RegistrationActivityNotFound, UnsupportedPreferenceType {

        Optional<RegistrationActivity> registrationActivity = getRegistrationActivity(activityCode);

        if(!registrationActivity.isPresent())
            return defaultValue;

        Optional<List<RegistrationPreference>> preferences = registrationActivity.get().preferences;

        if(!preferences.isPresent())
            return defaultValue;

        for(RegistrationPreference pref : preferences.get())
        {
            if(pref.name.equalsIgnoreCase(preferenceName))
                return PreferenceHelper.getParseValue(pref.value, pref.type);
        }
        return defaultValue;
    }

    default Object getPreference(String preferenceName,  Object defaultValue) throws UnsupportedPreferenceType,RegistrationActivityNotFound
    {
        String code = getCurrent();

        return getPreference(code, preferenceName, defaultValue);

        /*Optional<RegistrationActivity> registrationActivity = getRegistrationActivity(code);

        if(!registrationActivity.isPresent())
            return defaultValue;

        Optional<List<RegistrationPreference>> preferences = registrationActivity.get().preferences;

        if(!preferences.isPresent())
            return defaultValue;

        for(RegistrationPreference pref : preferences.get())
        {
            if(pref.name.equalsIgnoreCase(preferenceName))
                return PreferenceHelper.getParseValue(pref.value, pref.type);
        }
        return defaultValue;*/
    }


    void saveActivity(RegistrationActivityModel registrationActivity);

    List<RegistrationPreference> getRegPreferences(String activity_id);
    List<DataPoint> getDatapoints(List<DatapointBinding> bindings);

}
