package intl.iom.bravemobile.interfaces;

import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.IndividualError;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.activities.ConsentsFeedbackModel;
import intl.iom.bravemobile.models.activities.DeletionLog;
import intl.iom.bravemobile.models.activities.DistributionAssistance;
import intl.iom.bravemobile.models.activities.RegistrationActivityModel;
import intl.iom.bravemobile.models.activities.SurveyAnswerModel;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.FlaggedData;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.SurveyTarget;

public interface HouseholdRegistrationService {
    Household getUnsavedHousehold();
    void setUnsavedHousehold(Household household);

    int getNextHouseholdId();
    void moveToNextHouseholdId();

    int getNextIndividualId(String householdId);
    default Household createEmptyHousehold(String deviceSignature)
    {
        Household hh = new Household();

        int householdNo = getNextHouseholdId();

        hh.householdNo = householdNo;

        hh.householdId = DataCollectionUtils.householdId(
                deviceSignature,
                householdNo
        );

        return hh;
    }

    default Individual createIndividual()
    {
        Household household = getUnsavedHousehold();

        Individual individual = new Individual();

        individual.individualId = getNextIndividualId(household.householdId);

        return individual;
    }

    default int householdCount(String activity_code){
        List<Household> households = getAll(activity_code);
        if(households==null)
            return 0;
        return households.size();
    }
    default int individualCount(String householdId) throws HouseholdError {

        List<Individual> individuals = getIndividuals(householdId);

        if(individuals==null)
            return 0;

        return individuals.size();

    }

    default int individualCountByActivityCode(String activityCode) {

        List<Individual> individuals = getIndividualsByActivity(activityCode);

        if(individuals==null)
            return 0;

        return individuals.size();

    }

    void saveHousehold(Household household);

    List<String> deleteHousehold(String activityCode, String householdId, String justification) throws HouseholdError;

    Individual getIndividual(String householdId, int individualId) throws HouseholdError,IndividualError;


    List<Individual> getIndividuals(String householdId) throws HouseholdError;

    void addIndividual(Household household, Individual individual) throws HouseholdError;

    void deleteIndividual(String activityCode, String householdid, int individualId, String justification) throws IndividualError;

    void updateIndividual(Household household, Individual individual) throws HouseholdError;
    List<Household> getAll(String activitycode);
    List<BiometricCheckModel> getVerificationsByActivity(String activitycode);
    List<Individual> getIndividualsByActivity(String activitycode);
    List<ConsentsFeedbackModel> getConsentFeedbacksByActivity(String activitycode);
    List<SurveyAnswerModel> getSurveyAnswersByActivity(String activitycode);


    Household getHousehold(String householdId) throws HouseholdError;

    Individual getHeadOfHousehold(String householdId) throws HouseholdError,IndividualError;

    void setIndividualAsHeadOfHousehold(String householdId, int individualId) throws HouseholdError,IndividualError;

    void updateHousehold(Household household);

    void saveConsent(String activityCode, String data);

    void saveSurveyAnswers(SurveyTarget surveyTarget,int survey_id, Map<Integer, String> answers);

    Map<Integer, String> getSurveyAnswers(SurveyTarget surveyTarget, int survey_id);

    void clearActivityData(String activityCode);

    void saveTmpSubject(String activityCode,String SubjectId);
    void deleteTmpSubject(String SubjectId);
    boolean deleteTmpSubject(String SubjectId, BiometricFlow flow);
    void deleteTmpSubjects(String activityCode);
    List<String> getAllTmpSubjects(String activityCode);
    List<String> getEnrolledHouseholds(String activityCode);
    int hohCount(String householdId);
    List<DistributionAssistance> getAssistancesByActivity(String code);

    boolean IsAllRequiredSurveysCollected(Household household, String activityCode);
    boolean isSurveyCompleted(String householdId, Integer individualId, int surveyId);
    boolean IsAllRequiredSurveysCollected(Individual individual, String activityCode);
    boolean IsComplete(Household household);

    boolean IsAllBiometricCollected(Household household, boolean is_biometric_required, int age_threshold);
    boolean IsBiometricCollected(Individual individual, boolean is_biometric_required, int age_threshold);

    boolean HasExactlyOneHead(Household household);
    List<DeletionLog> getDeletionLog(String code);

    int consentCount(String code);

    int deletionLogCount(String code);

    Family getDistributionTemplate(String activityCode, String householdId) throws HouseholdError;

    void saveDnld(String activityCode, FlaggedData data);
}
