package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.HashSet;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.helpers.MockUtils;
import intl.iom.bravemobile.helpers.NonceUtil;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.EnrollmentCallback;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DataPacketHeader;
import intl.iom.bravemobile.models.distributions.DataPacket;
import intl.iom.bravemobile.models.distributions.DistAssistance;
import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.distributions.DistItem;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.distributions.EnrollmentRequest;
import intl.iom.bravemobile.models.distributions.EnrollmentResponse;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.distributions.Kit;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.statics.ActivityMode;
import intl.iom.bravemobile.statics.DistributionType;
import retrofit2.Response;
import retrofit2.Retrofit;

public class SqlDistributionService implements DistributionService {

    private static String TAG = SqlDistributionService.class.getSimpleName();
    SecureStore secureStore;
    private DatabaseManager db;
    private Context _context;
    private DistDto dto;

    private int current_distribution;
    private RegistrationActivityService registrationActivityService;

    private Retrofit client;

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();

    private final BiometricFlow flow = new ClientBiometricFlow();

    public SqlDistributionService(Context context) {
        _context = context;
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);
        registrationActivityService = ServiceLocator.registrationActivityService(context);
        flow.init(context);

        RetrofitService retrofitService = null;
        try
        {
            retrofitService = new RetrofitService(context);
            client = retrofitService.getClient();
        }
        catch (GeneralSecurityException e)
        {
            throw new RuntimeException(e);
        }
        catch (IOException e)
        {
            throw new RuntimeException(e);
        }
    }

    @Override
    public void setCurrent(int distribution_id) {
        current_distribution = distribution_id;
    }

    @Override
    public int getCurrent() {
        return current_distribution;
    }

    @Override
    public DistDto getCurItems() {
        return dto;
    }

    @Override
    public void setCurItems(DistDto dto) {
        this.dto = dto;
    }

    @Override
    public List<Distribution> getAll(String activity_id) {

        return distributions;

    }

    @Override
    public void enrollMatch(String activityCode, int distributionId, String uuid, Family result) {

        try
        {
            db.open();
            db.beginTransaction();

            //add to uuid household_id mapping table
            ContentValues cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("uuid", distributionId);
            cv.put("household_id", result.getHohid());
            cv.put("inserted_on", System.currentTimeMillis());
            db.replace("tbl_distr_verif_mapping",cv);


            //add to enrollment table
            cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("distribution_id", distributionId);
            cv.put("household_id", result.getHohid());
            cv.put("individual_id", 0);
            String data = ObjectSerializer.serialize( result );
            cv.put("data", data);
            db.replace("tbl_enrolled_beneficiaries",cv);

            //enrollBiometric(beneficiaryId,result.beneficiary.getMembers());
            //enrollBiometric(beneficiaryId,result.beneficiary.getMembers());

            db.setTransactionSuccessful();


        }catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }finally {
            db.endTransaction();
            db.close();
        }


    }


    List<DistAssistance> assistances = new ArrayList<>();
    List<Distribution> distributions = new ArrayList<>();


    @Override
    public boolean isEnrolled(String activityCode, int distribution, String beneficiaryId) {

       try
       {
           db.open();

           //CREATE TABLE IF NOT EXISTS tbl_enrolled_beneficiaries(activity_code TEXT,
           // distribution_id INT, household_id TEXT, individual_id INT, data TEXT,
           // primary key(activity_code, distribution_id, household_id, individual_id));

           //db.execSQL("delete from tbl_enrolled_beneficiaries");
           //db.execSQL("delete from tbl_distribution_assistances");

           Cursor cursor = db.rawQuery("Select * from tbl_enrolled_beneficiaries where " +
                   "activity_code=? and  distribution_id=? and  household_id=? and individual_id=0", new String[]{activityCode,  Integer.toString(distribution), beneficiaryId});

           if(cursor!=null && cursor.moveToNext()) {

               return true;
           }


       }
       catch (Exception e)
       {
           Log.e(TAG, e.getMessage());
       }
       finally {
           db.close();
       }
        return  false;
    }





    @Override
    public Family getEnrolledBeneficiary(String activityCode, int distribution, String beneficiaryId) {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_enrolled_beneficiaries where " +
                    "activity_code=? and  distribution_id=? and  household_id=? and individual_id=0", new String[]{activityCode,  Integer.toString(distribution), beneficiaryId});

            if(cursor!=null && cursor.moveToNext()) {

                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                Family restored = ObjectSerializer.deserialize(data, Family.class);

                return restored;
            }


        }catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return null;
    }


    @Override
    public void checkEnrollment(String activitycode,int distribution, String beneficiaryId, EnrollmentCallback callback) {

        Distribution  d = registrationActivityService.getDistributionById(activitycode, distribution);

        if(d == null)
        {
            callback.onFailure(new Exception(_context.getString(R.string.distribution_not_found)));
            return;
        }

        switch (ActivityMode.fromCode(d.mode))
        {
            case OFFLINE:
                if( isEnrolled(activitycode,distribution, beneficiaryId))
                    callback.onSuccess();
                else
                    callback.onFailure(new Exception(_context.getString(R.string.offline_dist_beneficiary_not_enrolled)));
                break;
            case ONLINE:
            case HYBRID:
                if(!isEnrolled(activitycode,distribution, beneficiaryId))
                {
                    callback.onFailureCheckOnline(new Exception(_context.getString(R.string.online_dist_beneficiary_not_enrolled)));
                }
                else
                    callback.onSuccess();
                break;
            default:
                callback.onFailure(new Exception(_context.getString(R.string.wrong_distribution_mode)));
                break;

        }
    }




    @Override
    public boolean saveAssistance(String activityCode,int distribution, String beneficiaryId, int individualId, String data) {

        Distribution d =  registrationActivityService.getDistributionById(activityCode, distribution);

        //CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(activity_code TEXT, id INT,
        // household_id TEXT, individual_id INT, data TEXT,
        // primary key(activity_code, id, household_id, individual_id));
        try
        {
            db.open();

            if(DistributionType.FAMILY == DistributionType.fromCode(d.type))
            {
                if(assistanceReceived(activityCode, distribution, beneficiaryId))
                    return false;

                //query = "CREATE TABLE IF NOT EXISTS tbl_distribution_assistances
                // (activity_code TEXT,
                // id INT,
                // household_id TEXT,
                // individual_id INT,
                // data TEXT,
                // primary key(activity_code, id, household_id, individual_id));";
                //            db.execSQL(query);

                ContentValues cv = new ContentValues();
                cv.put("activity_code",activityCode);
                cv.put("id",distribution);
                cv.put("household_id",beneficiaryId);
                cv.put("individual_id",0);
                cv.put("data",data);

                db.replace("tbl_distribution_assistances", cv);

                return true;

            }

            if(assistanceReceived(activityCode, distribution, beneficiaryId,individualId))
                return false;

            ContentValues cv = new ContentValues();
            cv.put("activity_code",activityCode);
            cv.put("id",distribution);
            cv.put("household_id",beneficiaryId);
            cv.put("individual_id",individualId);
            cv.put("data",data);

            db.replace("tbl_distribution_assistances", cv);


        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally
        {
            db.close();
        }

        return true;
    }

    @Override
    public int assistanceCount(String activity_code) {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select count(*) from tbl_distribution_assistances where activity_code=?",
                    new String[]{ activity_code});

            if(cursor!=null && cursor.moveToNext()){

                return cursor.getInt(0);
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


    private boolean assistanceReceived(String activityCode, int distribution, String beneficiaryId, int individualId) {

        Cursor cursor = db.rawQuery("Select * from tbl_distribution_assistances " +
                        "where activity_code=? and id=? and household_id=? and individual_id=?",
                new String[]{ activityCode, Integer.toString(distribution), beneficiaryId, Integer.toString(individualId)});

        //CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(activity_code TEXT, id INT,
        // household_id TEXT, individual_id INT, data TEXT,
        // primary key(activity_code, id, household_id, individual_id));

        if(cursor!=null && cursor.moveToNext()){

            return true;
        }

        return false;

    }

    private boolean assistanceReceived(String activityCode, int distribution, String beneficiaryId) {

       return assistanceReceived(activityCode,distribution,beneficiaryId,0);

    }

    @Override
    public void fetchEnrollmentData(String activityCode, int distributionId, String beneficiaryId, BasicCallback basicCallback) {

        IO.execute(() -> {

            try
            {
                DataPacket request = new DataPacket();
                EnrollmentRequest payload =  new EnrollmentRequest(distributionId,beneficiaryId);

                String nonce = NonceUtil.generate();

                String timestamp = String.valueOf(System.currentTimeMillis());

                String encryptedKeyBase64 = "aaaaa";

                byte[] b = NonceUtil.decode(nonce);
                byte[] bytes = KeyStoreHelper.sign( b );
                String signature = NonceUtil.b64u( bytes );

                DataPacketHeader header = new DataPacketHeader();

                header.Nonce = nonce;
                header.EncryptedKey = encryptedKeyBase64;
                header.ActivityCode = activityCode;
                header.BatchId = UUID.randomUUID().toString(); //use this to store the job grouping
                header.DPoP = signature;
                header.Timestamp = timestamp;

                request.header = header;
                request.payload = ObjectSerializer.serialize(payload);



                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Response<EnrollmentResponse> call = endpoints.fetchEnrollmentData(request).execute();

                if(call.isSuccessful()){

                    EnrollmentResponse result =  call.body();

                    saveEnrollment(activityCode, distributionId, beneficiaryId, result.beneficiary);

                    basicCallback.onSuccess();
                }
                else
                {
                    int status = call.code();

                    if(status == 404)
                        basicCallback.onFailure(new Exception(_context.getString(R.string.beneficiary_no_enrolled)));
                    else if(status == 401)
                        basicCallback.onFailure(new Exception(_context.getString(R.string.device_unauthorized)));
                    else
                        basicCallback.onFailure(new Exception(_context.getString(R.string.brave_server_error)));
                }


            }
            catch (Exception e)
            {
                basicCallback.onFailure(new Exception(_context.getString(R.string.brave_network_error)));
            }

        });

    }

    @Override
    public int getTotalEnrollments(String activityCode, int distributionId) {

        try
        {
            db.open();

            String query="SELECT    activity_code,  distribution_id,   COUNT(DISTINCT COALESCE(household_id, '∅')) AS total_households FROM tbl_enrolled_beneficiaries " +
                    "WHERE activity_code=? AND distribution_id=? " +
                    "GROUP BY activity_code, distribution_id;";

            Cursor cursor = db.rawQuery(query,  new String[]{ activityCode, Integer.toString(distributionId)});

            if(cursor!=null && cursor.moveToNext()) {

                int total = cursor.getInt(cursor.getColumnIndexOrThrow("total_households"));

                return total;
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
        return 0;
    }

    @Override
    public boolean mapHousehold(String activityCode, int distributionId, String uuid, Family result) {

        try
        {
            db.open();
            db.beginTransaction();

            //add to uuid household_id mapping table
            ContentValues cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("uuid", uuid);
            cv.put("household_id", result.getHohid());
            cv.put("inserted_on", System.currentTimeMillis());
            db.replace("tbl_distr_verif_mapping",cv);

            cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("distribution_id", distributionId);
            cv.put("household_id", result.getHohid());
            cv.put("individual_id", 0);
            String data = ObjectSerializer.serialize( result );
            cv.put("data", data);
            db.replace("tbl_enrolled_beneficiaries",cv);

            db.execSQL("delete from tbl_verifications where activity_code=? and uuid=?", new String[]{ activityCode,uuid});

            db.setTransactionSuccessful();

            return true;

        }catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }finally {
            db.endTransaction();
            db.close();
        }


        return true;
    }

    public boolean saveEnrollment(String activityCode, int distributionId, String beneficiaryId, Family family) {

        //CREATE TABLE IF NOT EXISTS tbl_enrolled_beneficiaries(activity_code TEXT,
        // distribution_id INT, household_id TEXT, individual_id INT, data TEXT,
        // primary key(activity_code, distribution_id, household_id, individual_id));

        try
        {
            db.open();
            db.beginTransaction();

            ContentValues cv = new ContentValues();
            cv.put("activity_code", activityCode);
            cv.put("distribution_id", distributionId);
            cv.put("household_id", beneficiaryId);
            cv.put("individual_id", 0);
            String data = ObjectSerializer.serialize( family );
            cv.put("data", data);

            db.replace("tbl_enrolled_beneficiaries",cv);

            enrollBiometric(beneficiaryId,family.getMembers());

            db.setTransactionSuccessful();

            return true;


        }catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }finally {
            db.endTransaction();
            db.close();
        }

        return false;

    }

    private void enrollBiometric(String beneficiaryId, List<Family.Member> members) {

        List<BiometricFlow.BiometricSubject> subjects = new ArrayList<>();

        for ( Family.Member m: members ) {

            if(m.isBiometricCollected()) {

                String dbid = DataCollectionUtils.getIndividualId(beneficiaryId, m.getMemno());
                byte[] b64 = Bytes.fromBase64(m.getBiometricB64());
                subjects.add(
                        new BiometricFlow.BiometricSubject(dbid, b64 )
                );
            }

        }

        if(subjects.size()>0)
            flow.enrollBatch(subjects);


    }


}
