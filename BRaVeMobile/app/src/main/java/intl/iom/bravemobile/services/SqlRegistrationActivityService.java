package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.sqlite.SQLiteStatement;
import android.os.Build;
import android.util.Log;

import java.util.ArrayList;
import java.util.Date;
import java.util.Dictionary;
import java.util.HashMap;
import java.util.Hashtable;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.CustomDataset;
import intl.iom.bravemobile.models.CustomKeyPairGroup;
import intl.iom.bravemobile.models.CustomLookup;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.DatasetModel;
import intl.iom.bravemobile.models.DeviceKeyEntity;
import intl.iom.bravemobile.models.activities.AdminLevelModel;
import intl.iom.bravemobile.models.activities.AdminLocationModel;
import intl.iom.bravemobile.models.activities.Consent;
import intl.iom.bravemobile.models.activities.ConsentBinding;
import intl.iom.bravemobile.models.activities.ConsentModel;
import intl.iom.bravemobile.models.activities.DatapointBinding;
import intl.iom.bravemobile.models.activities.DatapointModel;
import intl.iom.bravemobile.models.activities.DistributionBinding;
import intl.iom.bravemobile.models.activities.PreferenceModel;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.activities.RegistrationActivityModel;
import intl.iom.bravemobile.models.activities.RegistrationActivityStaging;
import intl.iom.bravemobile.models.activities.SurveyBinding;
import intl.iom.bravemobile.models.activities.SurveyModel;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.statics.AnswerType;

public class SqlRegistrationActivityService implements RegistrationActivityService {

    private static String TAG = SqlRegistrationActivityService.class.getSimpleName();

    private Dictionary<String, List<Survey>> activity_surveys = new Hashtable<>();
    private Dictionary<String, List<Distribution>> activity_distributions = new Hashtable<>();
    private String activityCode;
    private SurveyTarget surveyTarget;

    private Dictionary<Integer, List<DatasetColumn>> datasetColumns = new Hashtable<>();

    SecureStore secureStore;
    private DatabaseManager db;
    public  SqlRegistrationActivityService(Context context)
    {
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);
    }

    @Override
    public String getCurrent() {
        return activityCode;
    }

    @Override
    public void setCurrent(String code) {
        activityCode = code;
    }

    @Override
    public SurveyTarget getSurveyTarget() {
        return surveyTarget;
    }

    @Override
    public void setSurveyTarget(SurveyTarget target) {
            surveyTarget = target;
    }

    @Override
    public List<Survey> getSurveys(String code) {
        return activity_surveys.get(code);
    }

    @Override
    public List<Distribution> getDistributions(String code) {
        return activity_distributions.get(code);
    }

    @Override
    public List<DatasetColumn> getDatasetColumns(int datasetId) {

        if(((Hashtable)datasetColumns).containsKey(datasetId))
        {
            return datasetColumns.get(datasetId);
        }

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_datasets where id=?", new String[]{Integer.toString(datasetId)});

            if(cursor!=null && cursor.moveToNext()){

                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                DatasetModel restored = ObjectSerializer.deserialize(data, DatasetModel.class);

                datasetColumns.put(datasetId, restored.getDatasetColumns() );

                return restored.getDatasetColumns();

            }

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
    public List<RegistrationActivity> getAll() {

        List<RegistrationActivity> result = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_registration_activities order by updated_on desc");

            while(cursor!=null && cursor.moveToNext()) {

                String code = cursor.getString(cursor.getColumnIndexOrThrow("id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                RegistrationActivityStaging restored = ObjectSerializer.deserialize(data, RegistrationActivityStaging.class);

                activity_surveys.put(code, getSurveys(restored.surveyBindings));
                activity_distributions.put(code, getDistributions(code,restored.distributionBindings));

                RegistrationActivity registrationActivity = new RegistrationActivity(
                        restored.id,
                        restored.title,
                        restored.description,
                        DateUtils.parseIso8601(restored.startDate),
                        DateUtils.parseIso8601(restored.endDate),
                        getRegPreferences(restored.id),
                        getDatapoints(restored.datapointBindings),
                        getConsents(restored.consentBindings),
                        restored.whitelist,
                        activity_surveys.get(code),
                        activity_distributions.get(code)

                );


                registrationActivity.allowRegistration = restored.allowRegistration;
                registrationActivity.allowDistribution = restored.allowDistribution;
                registrationActivity.allowVerification = restored.allowVerification;

                result.add(registrationActivity);

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

        return result;
    }



    @Override
    public Optional<RegistrationActivity> getRegistrationActivity(String code) throws RegistrationActivityNotFound {
        for (RegistrationActivity a : getAll()) {
            if (a.code.equalsIgnoreCase(code)) if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                return Optional.of(a);
            }
        }
        throw new RegistrationActivityNotFound("The requested activity (ID %d) could not be found.");
    }

    @Override
    public void saveActivity(RegistrationActivityModel registrationActivity) {

        try
        {
            db.open();
            db.beginTransaction();

            //long current = System.currentTimeMillis();

            saveAdminLevels(registrationActivity.adminLevels);

            saveAdminLocations(registrationActivity.adminLocations);

            saveLookupNames(registrationActivity.lookups);

            List<CustomKeyPairGroup> lkps = new ArrayList<>();
            for(CustomLookup l : registrationActivity.lookups)
                lkps.addAll(l.getLkpValues());

            saveLookupValues(lkps);

            savePreferences(registrationActivity.id, registrationActivity.preferences);

            saveDatasets(registrationActivity.datasets);

            saveDatapoints(registrationActivity.datapoints);

            saveSurveys(registrationActivity.surveys);
            
            saveDistributions(registrationActivity.distributions);

            saveConsents(registrationActivity.consents);

            String data = ObjectSerializer.serialize( registrationActivity.getStagingData() );

            ContentValues cv = new ContentValues();
            cv.put("id",registrationActivity.id);
            cv.put("data", data);
            cv.put("updated_on", System.currentTimeMillis());
            db.replace("tbl_registration_activities", cv);

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

    private void saveDistributions(List<Distribution> distributions) {

        if(distributions==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_distributions (id, data) VALUES (?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (Distribution d : distributions)
        {
            String data = ObjectSerializer.serialize( d.getStaging());
            stmt.clearBindings();
            stmt.bindLong(1, d.distributionId);
            stmt.bindString(2, data );
            stmt.executeInsert();
        }
    }

    /*query = "CREATE TABLE IF NOT EXISTS tbl_preferences(activity_code TEXT, id INT, name TEXT, description TEXT, type INT, value TEXT,  primary key(activity_code,id));";
        db.execSQL(query);*/
    private List<Consent> getConsents(List<ConsentBinding> bindings) {

        List<Consent> consents = new ArrayList<>();

        Map<Integer, ConsentBinding> tmp = new HashMap<>();

        List<String> dpIds = new ArrayList<>();

        for (ConsentBinding d: bindings)
        {
            tmp.put(d.consentId, d);
            dpIds.add(Integer.toString(d.consentId));
        }

        String joined = String.join(",", dpIds);

        try
        {
            db.open();

            /*query = "CREATE TABLE IF NOT EXISTS tbl_consents(id INT primary key, data TEXT);";
        db.execSQL(query);*/

            Cursor cursor = db.rawQuery(String.format("Select * from tbl_consents where id in (%s)",joined));

            while(cursor!=null && cursor.moveToNext())
            {
                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                ConsentModel restored = ObjectSerializer.deserialize(data, ConsentModel.class);

                ConsentBinding binding = tmp.get(restored.id);

                boolean is_required =  binding.isRequired;
                int type  = binding.type;
                int order = binding.order;

                Consent consent = new Consent(
                        restored, order,type, is_required
                );

                consents.add(consent);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return consents;
    }

    private List<Survey> getSurveys(List<SurveyBinding> bindings) {

        List<Survey> surveys = new ArrayList<>();

        Map<Integer, SurveyBinding> tmp = new HashMap<>();

        List<String> dpIds = new ArrayList<>();

        for (SurveyBinding s: bindings)
        {
            tmp.put(s.surveyId, s);
            dpIds.add(Integer.toString(s.surveyId));
        }

        String joined = String.join(",", dpIds);

        try
        {
            db.open();

            /* query = "CREATE TABLE IF NOT EXISTS tbl_surveys(id INT primary key, data TEXT);";
        db.execSQL(query); */

            Cursor cursor = db.rawQuery(String.format("Select * from tbl_surveys where id in (%s)",joined));

            while(cursor!=null && cursor.moveToNext())
            {
                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                SurveyModel restored = ObjectSerializer.deserialize(data, SurveyModel.class);

                SurveyBinding binding = tmp.get(restored.surveyId);

                boolean is_required =  binding.isRequired;

                Survey survey = new Survey(
                        restored, is_required
                );

                surveys.add(survey);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        //return consents;


        return surveys;
    }

    private List<Distribution> getDistributions(String activityCode, List<DistributionBinding> bindings) {

        List<Distribution> distributions = new ArrayList<>();

        Map<Integer, DistributionBinding> tmp = new HashMap<>();

        List<String> dIds = new ArrayList<>();

        for (DistributionBinding d: bindings)
        {
            tmp.put(d.distributionId, d);
            dIds.add(Integer.toString(d.distributionId));
        }

        String joined = String.join(",", dIds);

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(String.format("Select * from tbl_distributions where id in (%s)",joined));

            while(cursor!=null && cursor.moveToNext())
            {
                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                Distribution restored = ObjectSerializer.deserialize(data, Distribution.class);

                restored.activityCode = activityCode;

                DistributionBinding binding = tmp.get(restored.distributionId);

                restored.distributionId = id;
                restored.mode = binding.mode;
                restored.type = binding.type;
                restored.photoReceiptRequired = binding.photoReceiptRequired;
                restored.biometricReceiptRequired = binding.biometricReceiptRequired;

                distributions.add(restored);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return distributions;
    }

    public List<RegistrationPreference> getRegPreferences(String activity_id) {

        List<RegistrationPreference> preferences = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_preferences where activity_code=?", new String[]{activity_id});

            while(cursor!=null && cursor.moveToNext())
            {
                String name = cursor.getString(cursor.getColumnIndexOrThrow("name"));
                String value = cursor.getString(cursor.getColumnIndexOrThrow("value"));
                int type = cursor.getInt(cursor.getColumnIndexOrThrow("type"));


                RegistrationPreference preference = new RegistrationPreference(
                        name,
                        AnswerType.fromCode(type),
                        value
                );

                preferences.add(preference);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return preferences;
    }

    @Override
    public List<DataPoint> getDatapoints(List<DatapointBinding> bindings) {

        List<DataPoint> dataPoints = new ArrayList<>();

        Map<Integer, DatapointBinding> tmp = new HashMap<>();

        List<String> dpIds = new ArrayList<>();

        for (DatapointBinding d: bindings)
        {
            tmp.put(d.datapointId, d);
            dpIds.add(Integer.toString(d.datapointId));
        }

        String joined = String.join(",", dpIds);

        try
        {
            db.open();

            Cursor cursor = db.rawQuery(String.format("Select * from tbl_datapoints where id in (%s)",joined));

            while(cursor!=null && cursor.moveToNext())
            {
                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                DatapointModel restored = ObjectSerializer.deserialize(data, DatapointModel.class);

                DatapointBinding binding = tmp.get(restored.id);

                boolean is_required =  binding.isRequired;
                int type  = binding.type;

                DataPoint dp = new DataPoint(
                        restored, type, is_required
                );

                dataPoints.add(dp);

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return dataPoints;
    }

    private void savePreferences(String activity_id, List<PreferenceModel> preferences) {

        if(preferences==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_preferences (activity_code, id, name, description, type, value) VALUES (?, ?, ?, ?, ?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (PreferenceModel p : preferences)
        {
            stmt.clearBindings();
            stmt.bindString(1, activity_id);
            stmt.bindLong(2, p.id );
            stmt.bindString(3, p.name );
            stmt.bindString(4, p.description );
            stmt.bindLong(5, p.type );
            stmt.bindString(6, p.value );
            stmt.executeInsert();
        }
    }

    private void saveConsents(List<ConsentModel> consents) {
        if(consents==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_consents (id, data) VALUES (?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (ConsentModel c : consents)
        {
            String data = ObjectSerializer.serialize( c );
            stmt.clearBindings();
            stmt.bindLong(1, c.id);
            stmt.bindString(2, data );
            stmt.executeInsert();
        }
    }

    private void saveSurveys(List<SurveyModel> surveys) {
        if(surveys==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_surveys (id, data) VALUES (?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (SurveyModel s : surveys)
        {
            String data = ObjectSerializer.serialize( s );
            stmt.clearBindings();
            stmt.bindLong(1, s.surveyId);
            stmt.bindString(2, data );
            stmt.executeInsert();
        }
    }

    private void saveDatapoints(List<DatapointModel> datapoints) {

        if(datapoints==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_datapoints (id, data) VALUES (?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (DatapointModel d : datapoints)
        {
            String data = ObjectSerializer.serialize( d );
            stmt.clearBindings();
            stmt.bindLong(1, d.id);
            stmt.bindString(2, data );
            stmt.executeInsert();
        }
    }

    private void saveDatasets(List<CustomDataset> datasets) {

        if(datasets==null)return;

        //get fresh dataset data in dictionary
        datasetColumns = new Hashtable<>();

        String sql = "INSERT OR REPLACE INTO tbl_datasets (id, data) VALUES (?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (CustomDataset d : datasets)
        {
            //String data = ObjectSerializer.serialize( m );
            stmt.clearBindings();
            stmt.bindLong(1, d.id);
            stmt.bindString(2, d.data );
            stmt.executeInsert();
        }

    }

    private void saveLookupNames(List<CustomLookup> lkp_names) {

        if(lkp_names==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_lookup_names (id, name, is_active) VALUES (?, ?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (CustomLookup l : lkp_names)
        {
            stmt.clearBindings();
            stmt.bindLong(1, l.id);
            stmt.bindString(2, l.name);
            stmt.bindLong(3, l.isActive ? 1 : 0);
            stmt.executeInsert();
        }
    }

    private void saveLookupValues(List<CustomKeyPairGroup> lkp_values) {

        if(lkp_values==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_lookup_values (id, lookup_id, name, is_active) VALUES (?, ?, ?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (CustomKeyPairGroup g: lkp_values)
        {
            stmt.clearBindings();
            stmt.bindLong(1, g.id);
            stmt.bindLong(2, g.group_id);
            stmt.bindString(3, g.name);
            stmt.bindLong(4, g.isActive ? 1 : 0);
            stmt.executeInsert();
        }
    }

    private void saveAdminLocations(List<AdminLocationModel> locations) {

        if(locations==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_admin_locations (id, name, level, parent, is_active) VALUES (?, ?, ?, ?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (AdminLocationModel location : locations)
        {
            stmt.clearBindings();
            stmt.bindLong(1, location.id);
            stmt.bindString(2, location.name);
            stmt.bindLong(3, location.level);
            // assuming parent is a Long (or Integer) that can be null
            if (location.parent != null) {
                stmt.bindLong(4, location.parent);   // or location.parent.longValue()
            } else {
                stmt.bindNull(4);
            }
            stmt.bindLong(5, location.isActive ? 1 : 0);
            stmt.executeInsert();
        }
    }

    private void saveAdminLevels(List<AdminLevelModel> adminLevels) {

        if(adminLevels==null)return;

        String sql = "INSERT OR REPLACE INTO tbl_admin_levels (id, name, is_active) VALUES (?, ?, ?)";
        SQLiteStatement stmt = db.compileStatement(sql);
        for (AdminLevelModel level : adminLevels)
        {
            stmt.clearBindings();
            stmt.bindLong(1, level.id);
            stmt.bindString(2, level.name);
            stmt.bindLong(3, level.isActive ? 1 : 0);
            stmt.executeInsert();
        }
    }




}
