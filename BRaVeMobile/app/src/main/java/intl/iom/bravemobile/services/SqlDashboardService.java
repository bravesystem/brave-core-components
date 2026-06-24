package intl.iom.bravemobile.services;

import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import java.util.ArrayList;
import java.util.List;
import java.util.Set;
import java.util.stream.Collectors;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.interfaces.DashboardService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.dashboard.BiometricPhotoComplianceSummary;
import intl.iom.bravemobile.models.dashboard.BiometricVerificationSummary;
import intl.iom.bravemobile.models.dashboard.CoreDashboardView;
import intl.iom.bravemobile.models.dashboard.KpiRowView;
import intl.iom.bravemobile.models.dashboard.SurveyCompletionItem;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.statics.DataPointType;

public class SqlDashboardService implements DashboardService {

    private static String TAG = SqlDashboardService.class.getSimpleName();

    private DatabaseManager db;
    //private Retrofit client;

    private SecureStore secureStore;
    private HouseholdRegistrationService householdRegistrationService;
    private RegistrationActivityService registrationActivityService;
    public SqlDashboardService(Context context)
    {
        db = new DatabaseManager(context);
        householdRegistrationService = ServiceLocator.householdRegistrationService(context);
        registrationActivityService = ServiceLocator.registrationActivityService(context);
    }
    @Override
    public CoreDashboardView getViewData(RegistrationActivity activity) {
        KpiRowView kpi = buildKpiRow( activity);
        BiometricVerificationSummary verification = buildVerificationSummary( activity.code);
        List<SurveyCompletionItem> surveys = buildSurveyCompletion( activity);
        BiometricPhotoComplianceSummary compliance = buildBiometricPhotoCompliance( activity.code);
        return new CoreDashboardView(kpi, verification, surveys, compliance);

    }

    private BiometricVerificationSummary buildVerificationSummary(String activityCode) {
        int create = count("tbl_verifications", "activity_code = ? AND is_processed=0 AND updated_on IS NULL", activityCode);
        int processing = count( "tbl_verifications", "activity_code = ? AND is_processed = 0 AND updated_on > inserted_on", activityCode);
        //int processing = count( "tbl_verifications", "activity_code = ? AND COALESCE(is_processed, -1) = 0", activityCode);
        int noMatch = count( "tbl_verifications", "activity_code = ? AND is_processed = 1 AND (matched_found IS NULL OR matched_found = 0)", activityCode);
        int match = count( "tbl_verifications", "activity_code = ? AND is_processed = 1 AND matched_found = 1", activityCode);
        return new BiometricVerificationSummary(create, processing, noMatch, match);
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

    private List<SurveyCompletionItem> buildSurveyCompletion( RegistrationActivity activity)
    {
        List<Integer> surveyIds = getSurveyIdsInUse( activity.code);

        retainMatchingSurveyIds(surveyIds, activity.surveys);

        int hhCount = count( "tbl_households", "activity_code = ?", activity.code);
        int indCount = count( "tbl_individuals", "activity_code = ?", activity.code);
        List<SurveyCompletionItem> out = new ArrayList<>();
        for (int surveyId : surveyIds) {
            int collected = countSurveyCollected( activity.code, surveyId);
            // default individual-level

            DataPointType type = activity.getSurveyType(surveyId);

            int universe = type == DataPointType.INDIVIDUAL
                    ?indCount
                    :hhCount;
            int notCollected = Math.max(0, universe - collected);
            out.add(new SurveyCompletionItem(surveyId, true, type.getCode(), collected, notCollected));
        }
        return out;
    }

    private BiometricPhotoComplianceSummary buildBiometricPhotoCompliance( String activityCode) {
        //int indCount = count( "tbl_individuals", "activity_code = ?", activityCode);
        List<Individual> individuals = householdRegistrationService.getIndividualsByActivity(activityCode);

        int indCount = individuals.size();

        int biometricAll = 0; int photoAll = 0;

        for(Individual i: individuals)
        {
            if(i.isBiometricCollected())
                biometricAll++;

            if(i.isPhotoCollected())
                photoAll++;
        }

        int biometricSomeMissing = Math.max(0, indCount - biometricAll);

        int photoMissing = Math.max(0, indCount - photoAll);;

        return new BiometricPhotoComplianceSummary(biometricAll, biometricSomeMissing, photoAll, photoMissing);
    }

    private KpiRowView buildKpiRow( RegistrationActivity activity) {

        String activityCode = activity.code;

        int hhCount = count( "tbl_households", "activity_code = ?", activityCode);
        int indCount = count( "tbl_individuals",
                "activity_code = ?", activityCode);

        int assistanceHh = countDistinct( "tbl_distribution_assistances",
                "household_id", "activity_code = ?", activityCode);
        int assistanceInd = countDistinctPairs( "tbl_distribution_assistances",
                "household_id", "individual_id", "activity_code = ?", activityCode);

        int requiredNotCollected = countRequiredSurveysNotCollected( activity, hhCount, indCount);

        return new KpiRowView(hhCount, indCount, requiredNotCollected, assistanceHh, assistanceInd);
    }

    private int count( String table, String where, String arg)
    {
        String sql = "SELECT COUNT(*) FROM " + table + " WHERE " + where;

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{arg});

            cursor.moveToNext();

            return cursor.getInt(0);
        }
        catch (Exception e)
        {
            return 0;
        }
        finally
        {
            db.close();
        }
    }

    private int countDistinct( String table, String column, String where, String arg)
    {
        String sql = "SELECT COUNT(DISTINCT " + column + ") FROM " + table + " WHERE " + where;

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{arg});

            cursor.moveToNext();

            return cursor.getInt(0);
        }
        catch (Exception e)
        {
            return 0;
        }
        finally
        {
            db.close();
        }
    }

    private int countDistinctPairs( String table, String col1, String col2, String where, String arg)
    {
        String sql = "SELECT COUNT(DISTINCT " + col1 + " || '|' || " + col2 + ") FROM " + table + " WHERE " + where;

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{arg});

            cursor.moveToNext();

            return cursor.getInt(0);
        }
        catch (Exception e)
        {
            return 0;
        }
        finally
        {
            db.close();
        }
    }

    private int countRequiredSurveysNotCollected( RegistrationActivity activity, int hhCount, int indCount)
    {

        //List<Integer> surveyIds = getSurveyIdsInUse(activity.code); //ok

        List<Survey> surveys = registrationActivityService.getSurveys(activity.code);

        int total = 0;

        for (Survey s : surveys) {

            int surveyId = s.code;

            int collected = countSurveyCollected( activity.code, surveyId);

            // assume individual-level; use hhCount if survey is HH-level
            int universe = s.surveyType== DataPointType.INDIVIDUAL
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
}
