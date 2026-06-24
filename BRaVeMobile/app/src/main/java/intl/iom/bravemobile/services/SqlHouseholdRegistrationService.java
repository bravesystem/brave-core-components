package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import androidx.core.util.Consumer;

import com.google.gson.reflect.TypeToken;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import org.json.JSONArray;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.IndividualError;
import intl.iom.bravemobile.exceptions.LookupItemNotFound;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.FormValidator;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.activities.ConsentsFeedbackModel;
import intl.iom.bravemobile.models.activities.DeletionLog;
import intl.iom.bravemobile.models.activities.DistributionAssistance;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.activities.SurveyAnswerModel;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.FlaggedData;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.statics.DataPointType;

public class SqlHouseholdRegistrationService implements HouseholdRegistrationService {

    private static String TAG = HouseholdRegistrationService.class.getSimpleName();

    SecureStore secureStore;
    RegistrationActivityService registrationActivityService;
    LookupService lookupService;
    private DatabaseManager db;
    Household current;

    public SqlHouseholdRegistrationService(Context context)
    {
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);
        registrationActivityService = ServiceLocator.registrationActivityService(context);
        lookupService = ServiceLocator.lookupService(context);
    }

    @Override
    public Household getUnsavedHousehold() {
        return current;
    }

    @Override
    public void setUnsavedHousehold(Household household) {
        current = household;
    }

    @Override
    public int getNextHouseholdId()
    {
        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select current_id from tbl_current_household_id");

            cursor.moveToNext();

            return cursor.getInt(0)+1;
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return 0;

    }

    @Override
    public void moveToNextHouseholdId() {

        try
        {
            db.open();

           db.execSQL("update tbl_current_household_id set current_id = current_id + 1;");
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }finally {
            db.close();
        }

    }

    @Override
    public int getNextIndividualId(String householdId) {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select max(individual_id) max_id from tbl_individuals where household_id=?", new String[]{householdId});

            cursor.moveToNext();

            return cursor.getInt(cursor.getColumnIndexOrThrow("max_id")) + 1;

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return 1;
    }

    @Override
    public void saveHousehold(Household household) {

        try
        {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();

            String data = ObjectSerializer.serialize( household.getStagingData() );

            String enumerator = secureStore.getEnumerator();

            ContentValues cv = new ContentValues();
            cv.put("activity_code",household.activityCode);
            cv.put("household_id",household.householdId);
            cv.put("individual_no",household.individualNo);
            cv.put("from_server",household.fromServer?1:0);
            cv.put("read_only",household.isReadOnly?1:0);
            cv.put("data",data);
            cv.put("inserted_on",current);
            cv.put("inserted_by", enumerator );

            db.insert("tbl_households", cv);
            db.setTransactionSuccessful();

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.endTransaction();
            db.close();
        }

    }

    @Override
    public List<String> deleteHousehold(String activity_code, String householdId, String justification) throws HouseholdError {

        try
        {
            db.open();
            db.beginTransaction();

            // Check if household_id exists in tbl_distribution_assistances
            Cursor c = db.rawQuery(
                    "SELECT 1 FROM tbl_distribution_assistances WHERE household_id = ? LIMIT 1",
                    new String[]{householdId});
            if (c.moveToFirst()) {
                c.close();
                throw new HouseholdError("Cannot delete this household.");
            }
            c.close();

            // Check if household_id exists in tbl_enrolled_beneficiaries
            c = db.rawQuery(
                    "SELECT 1 FROM tbl_enrolled_beneficiaries WHERE household_id = ? LIMIT 1",
                    new String[]{householdId});
            if (c.moveToFirst()) {
                c.close();
                throw new HouseholdError("Cannot delete this household.");
            }
            c.close();

            // Get all individuals in the household and build list of IDs before deleting
            List<String> individualIds = new ArrayList<>();
            c = db.rawQuery(
                    "SELECT individual_id FROM tbl_individuals WHERE household_id = ?",
                    new String[]{householdId});
            while (c.moveToNext()) {
                int individualId = c.getInt(c.getColumnIndexOrThrow("individual_id"));
                individualIds.add(DataCollectionUtils.getIndividualId(householdId, individualId));
            }
            c.close();

            // Delete in dependency order: survey_answers, individuals, households
            db.delete("tbl_survey_answers", "household_id = ?", new String[]{householdId});
            db.delete("tbl_individuals", "household_id = ?", new String[]{householdId});
            db.delete("tbl_households", "household_id = ?", new String[]{householdId});


            ContentValues cv = new ContentValues();
            cv.put("activity_code", activity_code);
            cv.put("deletion_type", "household");
            cv.put("household_id", householdId);
            cv.putNull("individual_id");
            cv.put("deleted_individual_ids", new JSONArray(individualIds).toString());
            cv.put("justification", justification);
            cv.put("deleted_at", System.currentTimeMillis());
            cv.put("deleted_by", secureStore.getEnumerator());
            db.insert("tbl_deleted_records", cv);


            db.setTransactionSuccessful();
            return individualIds;

        } catch (HouseholdError e) {
            Log.e(TAG, e.getMessage());
            throw e;
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.endTransaction();
            db.close();
        }

        return null;
    }

    @Override
    public Individual getIndividual( String householdId, int individualId) throws HouseholdError, IndividualError {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_individuals where household_id=? and individual_id=?", new String[]{householdId, Integer.toString(individualId)});

            cursor.moveToNext();

            int deleted = cursor.getInt(cursor.getColumnIndexOrThrow("deleted"));
            int individual_id = cursor.getInt(cursor.getColumnIndexOrThrow("individual_id"));
            String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
            //String biometric = cursor.getString(cursor.getColumnIndexOrThrow("biometric"));
            Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
            Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));
            // ...

            long epochMillis= inserted_on;

            if(updated_on>0)
                epochMillis  = updated_on;

            Individual restored = ObjectSerializer.deserialize(data, Individual.class);

            restored.individualId = individual_id;  //individual_no == individualId
            //restored.biometricBase64 = biometric;
            restored.deleted = deleted;
            restored.updated_on = new Date( epochMillis );

            return restored;

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return null;
    }

    @Override
    public List<Individual> getIndividuals(String householdId) throws HouseholdError
    {
        List<Individual> individuals = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_individuals where household_id=?", new String[]{householdId});

            while (cursor.moveToNext()) {

                int deleted = cursor.getInt(cursor.getColumnIndexOrThrow("deleted"));
                int individual_id = cursor.getInt(cursor.getColumnIndexOrThrow("individual_id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                //String biometric = cursor.getString(cursor.getColumnIndexOrThrow("biometric"));
                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
                Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));
                // ...

                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));
                String updated_by = cursor.getString(cursor.getColumnIndexOrThrow("updated_by"));


                Individual restored = ObjectSerializer.deserialize(data, Individual.class);

                restored.householdId = householdId;

                restored.individualId = individual_id;  //individual_no == individualId
                //restored.biometricBase64 = biometric;
                restored.deleted = deleted;

                long epochMillis= inserted_on;

                restored.createdOnMs = epochMillis;
                restored.createdBy= inserted_by;

                if(updated_on>0)
                    epochMillis  = updated_on;

                restored.updatedOnMs= updated_on;
                restored.updatedBy = updated_by;


                restored.updated_on = new Date( epochMillis );



                individuals.add(restored);
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
            throw new HouseholdError(e.getMessage());
        }
        finally {
            db.close();
        }

        Collections.sort(individuals, (h1, h2) -> {
            if (h1.updated_on == null && h2.updated_on == null) return 0;
            if (h1.updated_on == null) return 1;   // nulls last
            if (h2.updated_on == null) return -1;
            return h2.updated_on.compareTo(h1.updated_on); // DESC
        });


        return individuals;
    }

    @Override
    public void addIndividual(Household household, Individual individual) throws HouseholdError {

        try
        {
            db.open();

            db.beginTransaction();

            long current = System.currentTimeMillis();

            String data = ObjectSerializer.serialize( individual.getStagingData() );

            String enumerator = secureStore.getEnumerator();

            ContentValues cv = new ContentValues();
            cv.put("activity_code",household.activityCode);
            cv.put("household_id",household.householdId);
            cv.put("individual_id",individual.individualId);
            cv.put("deleted",individual.deleted);
            cv.put("data",data);
            cv.put("inserted_on",current);
            cv.put("inserted_by", enumerator);

            db.insert("tbl_individuals", cv);

            db.rawQuery("update tbl_households set individual_no=individual_no+1, updated_on=?, updated_by=? where household_id=?",new String[]{Long.toString(current),enumerator,household.householdId});

            db.setTransactionSuccessful();

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }

    }

    @Override
    public void deleteIndividual(String activityCode, String householdId, int individualId, String justification) throws IndividualError {

        try
        {
            db.open();
            db.beginTransaction();

            // Check if (household_id, individual_id) exists in tbl_distribution_assistances
            Cursor c = db.rawQuery(
                    "SELECT 1 FROM tbl_distribution_assistances WHERE household_id = ? AND individual_id = ? LIMIT 1",
                    new String[]{householdId, String.valueOf(individualId)});
            if (c.moveToFirst()) {
                c.close();
                throw new IndividualError("Cannot delete this individual.");
            }
            c.close();

            // Check if (household_id, individual_id) exists in tbl_enrolled_beneficiaries
            c = db.rawQuery(
                    "SELECT 1 FROM tbl_enrolled_beneficiaries WHERE household_id = ? AND individual_id = ? LIMIT 1",
                    new String[]{householdId, String.valueOf(individualId)});
            if (c.moveToFirst()) {
                c.close();
                throw new IndividualError("Cannot delete this individual.");
            }
            c.close();

            List<String> individualIds = new ArrayList<>();
            individualIds.add(DataCollectionUtils.getIndividualId(householdId, individualId));

            // Delete in dependency order: survey_answers, then individuals
            db.delete("tbl_survey_answers", "household_id = ? AND individual_id = ?",
                    new String[]{householdId, String.valueOf(individualId)});
            db.delete("tbl_individuals", "household_id = ? AND individual_id = ?",
                    new String[]{householdId, String.valueOf(individualId)});

            long current = System.currentTimeMillis();

            ContentValues cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("deletion_type", "individual");
            cv.put("household_id", householdId);
            cv.put("individual_id", individualId);
            cv.put("deleted_individual_ids", new JSONArray(individualIds).toString());
            cv.put("justification", justification);
            cv.put("deleted_at", current);
            cv.put("deleted_by", secureStore.getEnumerator());
            db.insert("tbl_deleted_records", cv);

            db.rawQuery("update tbl_households set individual_no=individual_no-1, updated_on=?, updated_by=? where household_id=?",new String[]{Long.toString(current), secureStore.getEnumerator(), householdId});

            db.setTransactionSuccessful();

        } catch (IndividualError e) {
            Log.e(TAG, e.getMessage());
            throw e;
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.endTransaction();
            db.close();
        }

    }

    @Override
    public void updateIndividual(Household household, Individual individual) throws HouseholdError {
        try
        {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();

            String data = ObjectSerializer.serialize( individual.getStagingData() );

            String enumerator =secureStore.getEnumerator();

            ContentValues cv = new ContentValues();
            cv.put("data",data);
            cv.put("updated_on",current);
            cv.put("updated_by", enumerator);

            db.update("tbl_individuals", cv, "household_id=? and individual_id=?", new String[]{household.householdId, Integer.toString(individual.individualId)});


            cv = new ContentValues();
            cv.put("updated_on",current);
            cv.put("updated_by", enumerator);

            db.update("tbl_households", cv, "household_id=? ", new String[]{household.householdId});


            /*if(!StringUtils.isBlank(individual.biometricBase64))
            {
                cv = new ContentValues();
                cv.put("data",individual.biometricBase64);
                cv.put("updated_on",current);
                cv.put("updated_by", enumerator);

                db.update("tbl_biometrics", cv,"household_id=? and individual_id=?", new String[]{household.householdId, Integer.toString(individual.individualId)} );
            }*/

            db.setTransactionSuccessful();
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }
    }


    @Override
    public void clearActivityData(String activityCode) {

        try
        {

            db.open();
            db.beginTransaction();


            /*db.execSQL("delete from tbl_admin_levels");
            db.execSQL("delete from tbl_admin_locations");
            db.execSQL("delete from tbl_lookup_names");
            db.execSQL("delete from tbl_lookup_values");
            db.execSQL("delete from tbl_datasets");
            db.execSQL("delete from tbl_consents");
            db.execSQL("delete from tbl_surveys");
            db.execSQL("delete from tbl_datapoints");

            db.execSQL("delete from tbl_registration_activities where id=?", new String[]{activityCode});
            db.execSQL("delete from tbl_preferences where activity_code=?", new String[]{activityCode});
            */

            db.execSQL("delete from tbl_survey_answers where activity_code=?", new String[]{activityCode});


            db.execSQL("delete from tbl_consent_feedbacks where activity_code=?", new String[]{activityCode});
            db.execSQL("delete from tbl_individuals where activity_code=?", new String[]{activityCode});
            db.execSQL("delete from tbl_households where activity_code=?", new String[]{activityCode});
            db.execSQL("delete from tbl_verifications where activity_code=?", new String[]{activityCode});
            db.execSQL("delete from tbl_tmp_verification_requests where activity_code=?", new String[]{activityCode});

            db.execSQL("delete from tbl_distribution_assistances where activity_code=?", new String[]{activityCode});

            db.execSQL("delete from tbl_enrolled_beneficiaries where activity_code=?", new String[]{activityCode});

            db.execSQL("delete from tbl_deleted_records where activity_code=?", new String[]{activityCode});

            db.setTransactionSuccessful();

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }


    }

    @Override
    public void saveTmpSubject(String activityCode, String SubjectId) {
        try
        {
            db.open();

            ContentValues cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("uuid", SubjectId);

            db.replace("tbl_tmp_biometrics",cv);
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }
    }

    @Override
    public void deleteTmpSubject(String SubjectId) {
        try
        {
            db.open();

            db.execSQL("delete from tbl_tmp_biometrics where uuid=?", new String[]{SubjectId});

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }
    }

    @Override
    public boolean deleteTmpSubject(String SubjectId, BiometricFlow flow) {

        int deletedRows = 0;

        try
        {
            db.open();

            deletedRows = db.delete("tbl_tmp_biometrics","uuid=?", new String[]{SubjectId});
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        if (deletedRows > 0) {
            flow.delete(SubjectId);
            return true;
        }

        return false;

    }

    @Override
    public void deleteTmpSubjects(String activityCode) {
        try
        {
            db.open();

            db.execSQL("delete from tbl_tmp_biometrics where activity_code=?", new String[]{activityCode});
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }
    }


    private void collectSubjects(
            DatabaseManager db,
            String sql,
            String[] args,
            Consumer<Cursor> mapper) {

        try (Cursor c = db.rawQuery(sql, args)) {
            while (c.moveToNext()) {
                mapper.accept(c);
            }
        }
    }

    @Override
    public List<String> getAllTmpSubjects(String activityCode) {

        List<String> subjects = new ArrayList<>();

        try
        {
            db.open();
            db.beginTransaction();

            collectSubjects(db,
                    "SELECT uuid FROM tbl_tmp_biometrics WHERE activity_code = ?",
                    new String[]{activityCode},
                    c -> subjects.add(c.getString(0)));

            //new table that is keeping track of saved verifications during distribution
            collectSubjects(db,
                    "SELECT uuid FROM tbl_distr_verif_mapping WHERE activity_code = ?",
                    new String[]{activityCode},
                    c -> subjects.add(c.getString(0)));

            collectSubjects(db,
                    "SELECT household_id, individual_id FROM tbl_individuals WHERE activity_code = ?",
                    new String[]{activityCode},
                    c -> subjects.add(
                            DataCollectionUtils.getIndividualId(
                                    c.getString(0),
                                    c.getInt(1))));


            collectSubjects(db,
                    "SELECT uuid FROM tbl_verifications WHERE activity_code = ?",
                    new String[]{activityCode},
                    c -> subjects.add(c.getString(0)));

            db.setTransactionSuccessful();
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());

            db.close();
        }
        finally {
            db.endTransaction();
        }

        return subjects;
    }

    @Override
    public List<String> getEnrolledHouseholds(String activityCode) {

        List<String> households = new ArrayList<>();
        try
        {
            db.open();

            collectSubjects(db,
                    "SELECT household_id \n" +
                            "FROM tbl_enrolled_beneficiaries \n" +
                            "GROUP BY household_id \n" +
                            "HAVING COUNT(DISTINCT activity_code) = 1 \n" +
                            "   AND MIN(activity_code) = ?;",
                    new String[]{activityCode},
                    c -> households.add(c.getString(0)));


        }
        catch (Exception e)
        {
         Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }
        return households;
    }

    @Override
    public int hohCount(String householdId) {

        int count = 0;

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("SELECT data FROM tbl_individuals WHERE household_id = ? ", new String[]{householdId});

            while(cursor!=null && cursor.moveToNext())
            {
                String data = cursor.getString(0);
                Individual restored = ObjectSerializer.deserialize(data, Individual.class);

                if(restored.relationship==0)
                    count++;
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();;
        }
        return count;
    }

    @Override
    public List<DistributionAssistance> getAssistancesByActivity(String activityCode)
    {
        List<DistributionAssistance> households = new ArrayList<>();

        //query = "CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(
        // activity_code TEXT, id INT, household_id TEXT,
        // individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
        //        db.execSQL(query);
        try
        {
            db.open();
            Cursor cursor = db.rawQuery("SELECT id,household_id,individual_id,data FROM tbl_distribution_assistances WHERE activity_code = ? ", new String[]{activityCode});

            while(cursor!=null && cursor.moveToNext())
            {
                households.add(
                        new DistributionAssistance(
                                cursor.getInt(0),
                                cursor.getString(1),
                                cursor.getInt(2),
                                cursor.getString(3)
                        )
                );
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return households;
    }

    @Override
    public boolean IsAllRequiredSurveysCollected( Household household, String activityCode) {

        try {

            int indCount = individualCount(household.householdId);

            RegistrationActivity activity = registrationActivityService.getRegistrationActivity(activityCode).get();

            Map<Integer, DataPointType> surveysInUsed = new HashMap<>();

            int total = 0;

            for(Survey s : activity.surveys)
            {
                int collected = countSurveyCollected( household.householdId, s.code);

                // assume individual-level; use hhCount if survey is HH-level
                int universe = s.surveyType== DataPointType.INDIVIDUAL
                        ?indCount
                        :1;

                total += Math.max(0, universe - collected);
            }

            /*for (int surveyId : surveyIds) {

                int collected = countSurveyCollected( household.householdId, surveyId);

                // assume individual-level; use hhCount if survey is HH-level
                int universe = activity.getSurveyType(surveyId)== DataPointType.INDIVIDUAL
                        ?indCount
                        :1;

                total += Math.max(0, universe - collected);
            }*/

            return total == 0;

        }catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }


        return false;
    }

    @Override
    public boolean isSurveyCompleted(String householdId, Integer individualId, int surveyId) {

        String sql;
        String[] args;

        if (individualId == null) {
            sql = "SELECT COUNT(*) FROM tbl_survey_answers " +
                    "WHERE household_id = ? AND individual_id IS NULL AND id = ?";
            args = new String[]{ householdId, String.valueOf(surveyId) };
        } else {
            sql = "SELECT COUNT(*) FROM tbl_survey_answers " +
                    "WHERE household_id = ? AND individual_id = ? AND id = ?";
            args = new String[]{ householdId, String.valueOf(individualId), String.valueOf(surveyId) };
        }

        try {
            db.open();

            Cursor cursor = db.rawQuery(sql, args);

            if (cursor.moveToFirst()) {
                return cursor.getInt(0) > 0;
            }
        }
        catch (Exception e) {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return false;
    }
    @Override
    public boolean IsAllRequiredSurveysCollected(Individual individual, String activityCode) {
        try {

            //List<Integer> surveyIds = getSurveyIdsInUse(individual.householdId, activityCode); //ok

            RegistrationActivity activity = registrationActivityService.getRegistrationActivity(activityCode).get();

            for(Survey s: activity.surveys)
            {
                if(s.surveyType== DataPointType.INDIVIDUAL)
                {
                    if(!isSurveyCompleted(individual.householdId, individual.individualId, s.code))
                        return false;
                }
            }

            return true;


        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }


        return false;
    }

    private int countSurveyCollected( String householdId, int surveyId)
    {
        String sql = "SELECT COUNT(*) FROM tbl_survey_answers WHERE household_id = ? AND id = ?";

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{householdId, String.valueOf(surveyId)});

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

    private List<Integer> getSurveyIdsInUse(String householdId, String activityCode) {

        List<Integer> out = new ArrayList<>();

        String sql = "SELECT DISTINCT id FROM tbl_survey_answers WHERE activity_code = ?";

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(sql, new String[]{householdId, activityCode});

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

    @Override
    public boolean IsComplete(Household household) {

        try {

            Individual head = getHeadOfHousehold(household.householdId);

            return head!=null;

        } catch (HouseholdError e) {
            Log.e(TAG, e.getMessage());
            throw new RuntimeException(e);
        } catch (IndividualError e) {
            Log.e(TAG, e.getMessage());
        }

        return false;

    }

    @Override
    public boolean IsAllBiometricCollected(Household household, boolean is_biometric_required, int age_threshold) {

        if(!is_biometric_required)
            return true;

        try {

            List<Individual> individuals = getIndividuals(household.householdId);

            for(Individual i: individuals){

                if(!IsBiometricCollected(i, is_biometric_required,age_threshold))
                    return false;
            }


        } catch (HouseholdError e) {
            throw new RuntimeException(e);
        }



        return true;
    }

    @Override
    public boolean IsBiometricCollected(Individual i, boolean is_biometric_required, int age_threshold) {

        boolean ok = FormValidator.all(
                () -> FormValidator.validateSelection(i.biometricNotCollected==null || i.biometricNotCollected.selectedReason== 1 , is_biometric_required, i.isBiometricCollected()),
                () -> FormValidator.requireTextIfOther(i.biometricNotCollected!=null && i.biometricNotCollected.selectedReason== 99, i.biometricNotCollected!=null?i.biometricNotCollected.reasonIfOther:"")
        );

        if(!ok)
            return false;


        /*if(!is_biometric_required)
            return true;*/

        if(is_biometric_required && DataCollectionUtils.computeAgeInYearsUsingDeviceZone(i.dob, i.ageInYears, i.ageInMonths, i.ageInDays) < age_threshold)
        {
            return true;
        }

        /*if(i.isBiometricCollected())
            return true;*/

        return ok;

    }

    @Override
    public boolean HasExactlyOneHead(Household household) {

        try {

            List<Individual> individuals = getIndividuals(household.householdId);

            int headCount = 0;
            for(Individual i : individuals)
            {
                if(i.relationship==0)
                    headCount++;
            }

            return headCount==1;

        } catch (HouseholdError e) {
            Log.e(TAG, e.getMessage());
        }

        return false;
    }

    @Override
    public List<DeletionLog> getDeletionLog(String activityCode)
    {
        List<DeletionLog> list = new ArrayList<>();

        try
        {
            db.open();

            Cursor c = db.rawQuery("select * from tbl_deleted_records where activity_code=?", new String[]{activityCode});

            while (c != null && c.moveToNext()) {
                list.add(DeletionLog.fromCursor(c));
            }
            return list;
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return list;
    }

    @Override
    public int consentCount(String activityCode) {
        try
        {
            db.open();

            Cursor c = db.rawQuery("select count(*) from tbl_consent_feedbacks where activity_code=?", new String[]{activityCode});

            if (c != null && c.moveToNext()) {
               return c.getInt(0);
            }
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return 0;
    }

    @Override
    public int deletionLogCount(String activityCode) {
        try
        {
            db.open();

            Cursor c = db.rawQuery("select count(*) from tbl_deleted_records where activity_code=?", new String[]{activityCode});

            if (c != null && c.moveToNext()) {
                return c.getInt(0);
            }
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return 0;
    }

    private Household getHousehold(String activityCode, String householdId)
    {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_households where activity_code=? and household_id=?", new String[]{activityCode, householdId});

            cursor.moveToNext();

            int from_server = cursor.getInt(cursor.getColumnIndexOrThrow("from_server"));
            int read_only = cursor.getInt(cursor.getColumnIndexOrThrow("read_only"));
            String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
            String activitycode = cursor.getString(cursor.getColumnIndexOrThrow("activity_code"));
            Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
            Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));

            long epochMillis= inserted_on;

            if(updated_on!=null)
                epochMillis  = updated_on;

            Household restored = ObjectSerializer.deserialize(data, Household.class);

            restored.fromServer = from_server>0;
            restored.isReadOnly = read_only>0;
            restored.activityCode = activitycode;
            restored.updated_on = new Date( epochMillis );

            return  restored;


        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return null;
    }

    @Override
    public Family getDistributionTemplate(String activityCode, String hhId) throws HouseholdError {

        Household hh = getHousehold(activityCode, hhId);

        if(hh == null)
            return null;

        List<Individual> individuals = getIndividuals(hhId);

        List<Family.Member> members = new ArrayList<>();

        for(Individual i:individuals)
        {
            String relationship = "unspecify";
            String gender = "unspecify";

            try {
                gender = lookupService.getLookupItemLabel(i.gender, 1);
                relationship = lookupService.getLookupItemLabel(i.relationship, 2);

            } catch (LookupItemNotFound e) {
                throw new RuntimeException(e);
            }


            members.add(new Family.Member(i.uuid.toString(),
                    i.individualId,
                    relationship,
                    DataCollectionUtils.fullname(i.firstName, i.middleName, i.lastName),
                    gender,
                    DataCollectionUtils.computeAgeInYearsUsingDeviceZone(i.dob, i.ageInYears, i.ageInMonths, i.ageInDays),
                    i.photoBase64,
                    i.biometricBase64 ));

        }

        String hhType = "unspecify";
        try {
            hhType = lookupService.getLookupItemLabel(hh.householdType, 3);
        } catch (LookupItemNotFound e) {
            throw new RuntimeException(e);
        }
        return new Family(hh.uuid.toString(), hhId, hhType, members);
    }

    @Override
    public void saveDnld(String activityCode, FlaggedData data) {

        saveDnldDataBatch(activityCode, data);

    }

    public void saveDnldDataBatch(String activityCode, FlaggedData data) {
        try {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();
            String enumerator = secureStore.getEnumerator();

            // ✅ Insert households
            for (Household household : data.households) {
                String hhdata = ObjectSerializer.serialize(household.getStagingData());

                ContentValues cv = new ContentValues();
                cv.put("activity_code", activityCode);
                cv.put("household_id", household.householdId);
                cv.put("individual_no", 0); // will recalc later
                cv.put("from_server", household.fromServer ? 1 : 0);
                cv.put("read_only", household.isReadOnly ? 1 : 0);
                cv.put("data", hhdata);
                cv.put("inserted_on", current);
                cv.put("inserted_by", enumerator);

                db.insertWithOnConflict("tbl_households",  cv);
            }

            // ✅ Insert individuals
            for (Individual individual : data.individuals) {
                try {
                    String inddata = ObjectSerializer.serialize(individual.getStagingData());

                    ContentValues cv = new ContentValues();
                    cv.put("activity_code", activityCode);
                    cv.put("household_id", individual.householdId);
                    cv.put("individual_id", individual.individualId);
                    cv.put("deleted", individual.deleted);
                    cv.put("data", inddata);
                    cv.put("inserted_on", current);
                    cv.put("inserted_by", enumerator);

                    db.insertWithOnConflict("tbl_individuals", cv);

                } catch (Exception ex) {
                    Log.e(TAG, "Failed to insert individual " + individual.individualId, ex);
                }
            }

            // ✅ Insert surveys
            for (SurveyAnswerModel survey : data.surveys) {
                try {

                    ContentValues cv = new ContentValues();
                    cv.put("activity_code", activityCode);
                    cv.put("id", survey.survey_id);
                    cv.put("household_id", survey.household_id);
                    cv.put("individual_id", survey.individual_id);
                    cv.put("data", survey.data);

                    db.insertWithOnConflict("tbl_survey_answers", cv);

                } catch (Exception ex) {
                    Log.e(TAG, "Failed to insert survey " + survey.survey_id, ex);
                }
            }

            // ✅ Single bulk update
            /*db.execSQL(
                    "UPDATE tbl_households " +
                            "SET individual_no = (" +
                            "    SELECT COUNT(*) FROM tbl_individuals i " +
                            "    WHERE i.household_id = tbl_households.household_id" +
                            ") " +
                            "WHERE activity_code = ?",
                    new String[]{activityCode}
            );*/

            db.setTransactionSuccessful();

        } catch (Exception e) {
            Log.e(TAG, e.getMessage(), e);
            throw new RuntimeException("Batch insert failed", e);
        } finally {
            db.endTransaction();
            db.close();
        }
    }

    @Override
    public List<Household> getAll(String activitycode) {

        List<Household> households = new ArrayList<>();

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_households where activity_code=?", new String[]{activitycode});

            while (cursor.moveToNext()) {

                int from_server = cursor.getInt(cursor.getColumnIndexOrThrow("from_server"));
                int individual_no = cursor.getInt(cursor.getColumnIndexOrThrow("individual_no"));
                int read_only = cursor.getInt(cursor.getColumnIndexOrThrow("read_only"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));
                Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));
                String updated_by = cursor.getString(cursor.getColumnIndexOrThrow("updated_by"));
                // ...

                long epochMillis= inserted_on;

                if(updated_on>0)
                    epochMillis  = updated_on;

                Household restored = ObjectSerializer.deserialize(data, Household.class);

                restored.individualNo = individual_no;
                restored.fromServer = from_server>0;
                restored.isReadOnly = read_only>0;
                restored.activityCode = activitycode;
                restored.updated_on = new Date( epochMillis );




                restored.createdBy = inserted_by;
                restored.createdOnMs = epochMillis;
                restored.updatedBy= updated_by;
                restored.updatedOnMs = updated_on;


                households.add(restored);
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        Collections.sort(households, (h1, h2) -> {
            if (h1.updated_on == null && h2.updated_on == null) return 0;
            if (h1.updated_on == null) return 1;   // nulls last
            if (h2.updated_on == null) return -1;
            return h2.updated_on.compareTo(h1.updated_on); // DESC
        });

        return households;
    }

    @Override
    public List<BiometricCheckModel> getVerificationsByActivity(String activitycode) {

        List<BiometricCheckModel> verifications = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select uuid,gender,template, inserted_by, inserted_on from tbl_verifications where activity_code=? and is_processed=0 ", new String[]{activitycode});

            while (cursor.moveToNext()) {

                verifications.add(
                        new BiometricCheckModel(
                                cursor.getString(0),
                                cursor.getInt(1),
                                cursor.getString(2),
                                cursor.getString(3),
                                cursor.getLong(4)
                        )
                );

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return verifications;
    }

    @Override
    public List<Individual> getIndividualsByActivity(String activitycode) {
        List<Individual> individuals = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_individuals where activity_code=?", new String[]{activitycode});

            while (cursor.moveToNext()) {

                int deleted = cursor.getInt(cursor.getColumnIndexOrThrow("deleted"));
                int individual_id = cursor.getInt(cursor.getColumnIndexOrThrow("individual_id"));
                String householdId = cursor.getString(cursor.getColumnIndexOrThrow("household_id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
                Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));

                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));
                String updated_by = cursor.getString(cursor.getColumnIndexOrThrow("updated_by"));

                Individual restored = ObjectSerializer.deserialize(data, Individual.class);

                restored.householdId = householdId;

                restored.individualId = individual_id;
                restored.deleted = deleted;

                long epochMillis= inserted_on;

                restored.createdOnMs = epochMillis;
                restored.createdBy= inserted_by;

                if(updated_on>0)
                    epochMillis  = updated_on;

                restored.updatedOnMs= updated_on;
                restored.updatedBy = updated_by;

                restored.updated_on = new Date( epochMillis );

                individuals.add(restored);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }


        return individuals;
    }

    @Override
    public List<ConsentsFeedbackModel> getConsentFeedbacksByActivity(String activitycode) {

        List<ConsentsFeedbackModel> feedbacks = new ArrayList<>();

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_consent_feedbacks where activity_code=?", new String[]{activitycode});

            while (cursor.moveToNext()) {

                String consent_id = cursor.getString(cursor.getColumnIndexOrThrow("consent_id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                int inserted_on = cursor.getInt(cursor.getColumnIndexOrThrow("inserted_on"));
                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));

                ConsentsFeedbackModel consentFeedback = new ConsentsFeedbackModel();

                consentFeedback.consent_id = consent_id;
                consentFeedback.data = data;
                consentFeedback.inserted_on= inserted_on;
                consentFeedback.inserted_by= inserted_by;

                feedbacks.add(consentFeedback);
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return feedbacks;
    }

    @Override
    public List<SurveyAnswerModel> getSurveyAnswersByActivity(String activitycode) {

        List<SurveyAnswerModel> answers = new ArrayList<>();

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_survey_answers where activity_code=?", new String[]{activitycode});

            while (cursor.moveToNext()) {

                int survey_id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String household_id = cursor.getString(cursor.getColumnIndexOrThrow("household_id"));
                int individual_id = cursor.getInt(cursor.getColumnIndexOrThrow("individual_id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                SurveyAnswerModel answer = new SurveyAnswerModel();

                answer.survey_id = survey_id;
                answer.household_id = household_id;
                answer.individual_id = individual_id;
                answer.data = data;

                answers.add(answer);
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return answers;
    }

    @Override
    public Household getHousehold(String householdId) throws HouseholdError
    {

        Household restored = null;

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_households where household_id=?", new String[]{householdId});

            cursor.moveToNext();

            int from_server = cursor.getInt(cursor.getColumnIndexOrThrow("from_server"));
            int read_only = cursor.getInt(cursor.getColumnIndexOrThrow("read_only"));
            String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
            String activitycode = cursor.getString(cursor.getColumnIndexOrThrow("activity_code"));
            Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
            Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));
            // ...

            long epochMillis= inserted_on;

            if(updated_on!=null)
                epochMillis  = updated_on;

            restored = ObjectSerializer.deserialize(data, Household.class);

            restored.fromServer = from_server>0;
            restored.isReadOnly = read_only>0;
            restored.activityCode = activitycode;
            restored.updated_on = new Date( epochMillis );

            return  restored;


        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return restored;
    }

    @Override
    public Individual getHeadOfHousehold(String householdId) throws HouseholdError, IndividualError
    {
        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_individuals  where household_id=?", new String[]{householdId});

            while (cursor!=null && cursor.moveToNext()) {

                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                Individual restored = ObjectSerializer.deserialize(data, Individual.class);

                if(restored.relationship!=0)
                    continue;

                int deleted = cursor.getInt(cursor.getColumnIndexOrThrow("deleted"));
                int individual_id = cursor.getInt(cursor.getColumnIndexOrThrow("individual_id"));
                //String biometric = cursor.getString(cursor.getColumnIndexOrThrow("biometric"));
                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));
                Long updated_on = cursor.getLong(cursor.getColumnIndexOrThrow("updated_on"));
                // ...

                long epochMillis= inserted_on;

                if(updated_on>0)
                    epochMillis  = updated_on;

                restored.individualId = individual_id;  //individual_no == individualId
                //restored.biometricBase64 = biometric;
                restored.deleted = deleted;
                restored.updated_on = new Date( epochMillis );

                return restored;
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
            throw new HouseholdError(e.getMessage());
        }
        finally {
            db.close();
        }

        return null;
    }

    @Override
    public void setIndividualAsHeadOfHousehold(String householdId, int individualId) throws HouseholdError, IndividualError {

    }

    @Override
    public void updateHousehold(Household household)  {

        try
        {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();

            String data = ObjectSerializer.serialize( household.getStagingData() );

            String enumerator = secureStore.getEnumerator();

            ContentValues cv = new ContentValues();
            cv.put("data",data);
            cv.put("updated_on",current);
            cv.put("updated_by", enumerator);

            db.update("tbl_households", cv, "household_id=?", new String[]{household.householdId});

            db.setTransactionSuccessful();
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }

    }

    @Override
    public void saveConsent(String activityCode, String data) {

        try
        {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();

            String enumerator = secureStore.getEnumerator();

            ContentValues cv = new ContentValues();
            cv.put("activity_code",activityCode);
            cv.put("consent_id", UUID.randomUUID().toString());
            cv.put("data",data);
            cv.put("inserted_on",current);
            cv.put("inserted_by", enumerator );

            db.insert("tbl_consent_feedbacks", cv);

            db.setTransactionSuccessful();
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }

    }

    @Override
    public void saveSurveyAnswers(SurveyTarget surveyTarget, int survey_id, Map<Integer, String> answers) {
        try
        {
            db.open();
            db.beginTransaction();

            String data = ObjectSerializer.serialize(answers);

            ContentValues cv = new ContentValues();
            cv.put("activity_code", surveyTarget.activity_code);
            cv.put("id",survey_id);
            cv.put("household_id",surveyTarget.household_id);
            cv.put("individual_id",surveyTarget.individual_id);
            cv.put("data",data);

            db.replace("tbl_survey_answers", cv);

            db.setTransactionSuccessful();
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.endTransaction();
            db.close();
        }
    }

    @Override
    public Map<Integer, String> getSurveyAnswers(SurveyTarget surveyTarget, int survey_id) {
        Map<Integer, String> answers = new HashMap<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select data from tbl_survey_answers  where activity_code=? and id=? and household_id=? and individual_id=?", new String[]{
                    surveyTarget.activity_code,
                    Integer.toString(survey_id),
                    surveyTarget.household_id,
                    Integer.toString(surveyTarget.individual_id)
            });

            if (cursor!=null && cursor.moveToNext())
            {
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));
                Type mapType = new TypeToken<Map<Integer, String>>() {}.getType();

                answers = ( Map<Integer, String>)ObjectSerializer.deserialize(data, mapType);
            }
        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return answers;
    }



}
