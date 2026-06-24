package intl.iom.bravemobile.helpers;

import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.services.SqlDashboardService;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.DefinedPreferences;

public class RegistrationActivityValidator {

    private static String TAG = RegistrationActivityValidator.class.getSimpleName();


    private DatabaseManager db;

    private DistributionService distributionService;
    private RegistrationActivityService registrationActivityService;
    private HouseholdRegistrationService householdRegistrationService;
    private VerificationService verificationService;

    public RegistrationActivityValidator(Context context)
    {
        db = new DatabaseManager(context);
        distributionService = ServiceLocator.distributionService(context);
        registrationActivityService = ServiceLocator.registrationActivityService(context);
        householdRegistrationService = ServiceLocator.householdRegistrationService(context);
        verificationService = ServiceLocator.verificationService(context);
    }

    public boolean IsActivityDataCollected(String activity_code)
    {

        if(householdRegistrationService.householdCount(activity_code) > 0)
            return true;

        if(householdRegistrationService.consentCount(activity_code) > 0)
            return true;

        if(householdRegistrationService.deletionLogCount(activity_code) > 0)
            return true;

        if(verificationService.verificationCount(activity_code) > 0)
            return true;

        if(verificationService.getPendingVerification(activity_code).size()>0)
            return true;
        //getPendingVerification(String code)

        if(distributionService.assistanceCount(activity_code)>0)
            return true;

        return false;
    }

    /*public CustomValidationResponse IsActivityDataCollected(String activity_code)
    {

        if(householdRegistrationService.householdCount(activity_code) > 0)
            return new CustomValidationResponse(true, "");

        if(householdRegistrationService.consentCount(activity_code) > 0)
            return new CustomValidationResponse(true, "");

        if(householdRegistrationService.deletionLogCount(activity_code) > 0)
            return new CustomValidationResponse(true, "");

        if(verificationService.verificationCount(activity_code) > 0)
            return new CustomValidationResponse(true, "");

        if(verificationService.getPendingVerification(activity_code).size()>0)
            return new CustomValidationResponse(true, "");
        //getPendingVerification(String code)

        if(distributionService.assistanceCount(activity_code)>0)
            return new CustomValidationResponse(true, "");

        return new CustomValidationResponse(false, "");
    }*/

    public boolean IsAllBiometricsCollected(String activityCode)  {

        try
        {
            boolean is_biometric_required =
                    (boolean)registrationActivityService.getPreference(
                            activityCode,
                            DefinedPreferences.BIOMETRIC_COLLECTION_ENABLED,
                            false
                    );

            int age_threshold =
                    (int)registrationActivityService.getPreference(
                            activityCode,
                            DefinedPreferences.BIOMETRIC_AGE_THRESHOLD,
                            5
                    );

            /*if(!is_biometric_required)
                return true;*/

            List<Individual> individuals = householdRegistrationService.getIndividualsByActivity(activityCode);

            int indCount = individuals.size();

            int biometricAll = 0;

            for(Individual i: individuals)
            {

                boolean ok = FormValidator.all(
                        () -> FormValidator.validateSelection(i.biometricNotCollected==null || i.biometricNotCollected.selectedReason== 1, is_biometric_required, i.isBiometricCollected()),
                        () -> FormValidator.requireTextIfOther(i.biometricNotCollected!=null && i.biometricNotCollected.selectedReason== 99, i.biometricNotCollected==null?"":i.biometricNotCollected.reasonIfOther)
                );

                // FormValidator.validateSelection(spNoBioReason, 1, biometric_collection_enabled, individual.isBiometricCollected(), "*"),

                if(!ok)
                    return false;

                if(is_biometric_required && DataCollectionUtils.computeAgeInYearsUsingDeviceZone(i.dob, i.ageInYears, i.ageInMonths, i.ageInDays) < age_threshold)
                {
                    biometricAll++;
                    continue;
                }

                /*if(i.isBiometricCollected())
                    biometricAll++;*/

                if(ok)
                    biometricAll++;
            }

            int biometricSomeMissing = Math.max(0, indCount - biometricAll);

            return biometricSomeMissing == 0;

        } catch (RegistrationActivityNotFound e) {
            Log.e(TAG, e.getMessage());
            throw new RuntimeException(e);
        }
        catch (UnsupportedPreferenceType e) {
            Log.e(TAG, e.getMessage());
            throw new RuntimeException(e);
        }
        catch (Exception e) {
            Log.e(TAG, e.getMessage());

        }

        return false;


    }

    public boolean IsAllRequiredSurveysCollected(String activityCode)
    {
        int hhCount = householdRegistrationService.householdCount(activityCode);

        int indCount = householdRegistrationService.individualCountByActivityCode(activityCode);

        try
        {
            int requiredSurveysNotCollected = countRequiredSurveysNotCollected(activityCode, hhCount, indCount);

            return requiredSurveysNotCollected == 0;

        }
        catch (RegistrationActivityNotFound e)
        {
            Log.e(TAG, e.getMessage());
            throw new RuntimeException(e);
        }

    }

    public void retainMatchingSurveyIds(List<Integer> listA, List<Survey> listB) {
        if (listA == null || listB == null) {
            return;
        }

        Set<Integer> surveyCodes = listB.stream()
                .map(Survey::getCode)
                .collect(Collectors.toSet());

        listA.removeIf(id -> !surveyCodes.contains(id));
    }

    private int countRequiredSurveysNotCollected(String activityCode, int hhCount, int indCount) throws RegistrationActivityNotFound {
        RegistrationActivity activity = registrationActivityService.getRegistrationActivity(activityCode).get();

        List<Integer> surveyIds = getSurveyIdsInUse(activityCode); //ok

        retainMatchingSurveyIds(surveyIds, activity.surveys);

        int total = 0;

        for (int surveyId : surveyIds) {

            int collected = countSurveyCollected( activityCode, surveyId);

            // assume individual-level; use hhCount if survey is HH-level
            int universe = activity.getSurveyType(surveyId)== DataPointType.INDIVIDUAL
                    ?indCount
                    :hhCount;

            total += Math.max(0, universe - collected);
        }

        return total;
    }

    private List<Integer> getSurveyIdsInUse( String activityCode)
    {
        List<Integer> out = new ArrayList<>();

        String sql = "SELECT DISTINCT id FROM tbl_survey_answers WHERE activity_code = ?";

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{activityCode});

            while (cursor!=null && cursor.moveToNext())
                out.add(cursor.getInt(0));

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return out;
    }


    private int countSurveyCollected( String activityCode, int surveyId)
    {
        String sql = "SELECT COUNT(*) FROM tbl_survey_answers WHERE activity_code = ? AND id = ?";

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{activityCode, String.valueOf(surveyId)});

            cursor.moveToNext();

            return cursor.getInt(0);
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
            return 0;
        }
        finally
        {
            db.close();
        }
    }

    @SafeVarargs
    public static boolean all(FormValidator.Validator... validators) {
        boolean allOk = true;
        for (FormValidator.Validator v : validators) {
            if (v != null) {
                boolean ok = v.validate();
                if (!ok)
                    return false;
                //allOk = false;
            }
        }
        return allOk;
    }

    public boolean AllHouseholdsHaveOnlyOneHead(String code) {

        List<Household> households = householdRegistrationService.getAll(code);

        for(Household h: households)
        {
            if(!householdRegistrationService.HasExactlyOneHead(h))
                return false;
        }

        return true;
    }
}
