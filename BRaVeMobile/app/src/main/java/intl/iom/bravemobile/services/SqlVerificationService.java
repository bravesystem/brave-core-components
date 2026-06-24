package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.Dictionary;
import java.util.Hashtable;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.AESHelper;
import intl.iom.bravemobile.helpers.AESPacket;
import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.helpers.NonceUtil;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.DataPacketHeader;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.VerificationExtra;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.models.registrations.VerificationResponse;
import intl.iom.bravemobile.models.registrations.VerifyTemplatesRequest;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import retrofit2.Response;
import retrofit2.Retrofit;

public class SqlVerificationService implements VerificationService {

    private static String TAG = SqlVerificationService.class.getSimpleName();
    SecureStore secureStore;
    private DatabaseManager db;

    private Context _context;

    private Retrofit client;

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();

    public  SqlVerificationService(Context context)
    {
        _context = context;
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);

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
    public String getJobId(String activity_code) {

        String jobId = null;

        try {
            db.open();
            db.beginTransaction();

            // 1) Try to fetch existing jobid
            try (Cursor cursor = db.rawQuery(
                    "SELECT jobid FROM tbl_tmp_verification_requests WHERE activity_code = ?",
                    new String[]{ activity_code })) {

                if (cursor!=null && cursor.moveToFirst()) {
                    jobId = cursor.getString(0);
                }
            }

            // 2) If not found, create and insert new one
            if (jobId == null) {
                jobId = UUID.randomUUID().toString();

                ContentValues cv = new ContentValues();
                cv.put("activity_code", activity_code);
                cv.put("jobid", jobId);

                db.insert("tbl_tmp_verification_requests", cv);
            }

            db.setTransactionSuccessful();
            return jobId;

        } catch (Exception e) {
            Log.e("DB", "Failed to get or create jobId", e);
            return null;

        } finally {
            if (db != null) {
                db.endTransaction();
            }
        }


    }

    @Override
    public List<Verification> getAll(String activity_code) {
        List<Verification> verifications = new ArrayList<>();

        try
        {

            db.open();
            //tbl_distr_verif_mapping
            //tbl_enrolled_beneficiaries
            //tbl_verifications

           /* db.execSQL("delete from tbl_distr_verif_mapping");
            db.execSQL("delete from tbl_enrolled_beneficiaries");
            db.execSQL("delete from tbl_verifications");*/


            Cursor cursor = db.rawQuery("Select * from tbl_verifications where activity_code=? and auth=0", new String[]{activity_code});

            while (cursor.moveToNext()) {

                int matched_found = cursor.getInt(cursor.getColumnIndexOrThrow("matched_found"));
                int is_processed = cursor.getInt(cursor.getColumnIndexOrThrow("is_processed"));
                int gender = cursor.getInt(cursor.getColumnIndexOrThrow("gender"));

                String uuid = cursor.getString(cursor.getColumnIndexOrThrow("uuid"));
                String matched_uuid = cursor.getString(cursor.getColumnIndexOrThrow("matched_uuid"));
                String template = cursor.getString(cursor.getColumnIndexOrThrow("template"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));

                int colIndex = cursor.getColumnIndexOrThrow("updated_on");

                Long updated_on = null;
                if (!cursor.isNull(colIndex)) {
                    updated_on = cursor.getLong(colIndex);
                }else{
                    updated_on = inserted_on;
                }



                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));

                //Family restored = ObjectSerializer.deserialize(data, Family.class);


                verifications.add(
                        new Verification(uuid, gender, template, inserted_on, updated_on, inserted_by,is_processed==1, matched_found==1,matched_uuid, data)
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

       Collections.sort(verifications, (v1, v2) -> {
            if (v1.updated_on == null && v2.updated_on == null) return 0;
            if (v1.updated_on == null) return 1;   // nulls last
            if (v2.updated_on == null) return -1;
            return v2.updated_on.compareTo(v1.updated_on); // DESC
        });

        return verifications;
    }

    @Override
    public BiometricCheckModel getVerification(String uuid) {
        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select uuid,gender,template, inserted_by, inserted_on from tbl_verifications where uuid=? and is_processed=0 ", new String[]{uuid});

            if (cursor!=null && cursor.moveToNext()) {

                return  new BiometricCheckModel(
                                cursor.getString(0),
                                cursor.getInt(1),
                                cursor.getString(2),
                                cursor.getString(3),
                                cursor.getLong(4)
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

        return null;
    }

    @Override
    public void verifyTemplates(String activityCode,String extra, List<BiometricCheckModel> templates, BasicCallback callback) {

        IO.execute(() -> {

            try
            {

                //VerificationExtra d = getVerificationExtra(extra);

                VerifyTemplatesRequest request = new VerifyTemplatesRequest();

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
                header.BatchId = getJobId(activityCode);  //use this to store the job grouping
                header.DPoP = signature;
                header.Timestamp = timestamp;
                header.Extra = extra;

                request.header = header;
                request.templates = ObjectSerializer.serialize(templates);

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Response<List<VerificationResponse>> call = endpoints.verifyTemplates(request).execute();

                if(call.isSuccessful()){

                    List<VerificationResponse> result =  call.body();

                    saveVerificationResponses(result);

                    callback.onSuccess();
                }
                else
                {
                    callback.onFailure(new Exception(_context.getString(R.string.brave_server_error)));
                }


            }
            catch (Exception e)
            {
                callback.onFailure(new Exception(_context.getString(R.string.brave_network_error)));
            }

        });

    }

    private VerificationExtra getVerificationExtra(String extra) {

        try{

            if(!StringUtils.isBlank(extra))
            {
                return ObjectSerializer.deserialize(extra, VerificationExtra.class);
            }

        }
        catch (Exception e)
        {

        }

        return new VerificationExtra();
    }


    //make sure to always call in a transaction
    private void updateVerification(String uuid, boolean is_processed, boolean matched_found, String matched_uuid, int score, String data) {

            //if(!is_processed)
            //    return;

            long current = System.currentTimeMillis();

            ContentValues cv = new ContentValues();

            cv.put("uuid", uuid);
            cv.put("is_processed", is_processed?1:0);
            cv.put("matched_found", matched_found?1:0);
            cv.put("matched_uuid", matched_uuid);
            cv.put("score", score);
            cv.put("data", data);
            cv.put("updated_on", current);

            db.update( "tbl_verifications", cv,"uuid = ?",  new String[]{ uuid });
    }

    private void saveVerificationResponses(List<VerificationResponse> result) {

        if(result==null || result.size()==0)
            return;

        try
        {
            db.open();
            db.beginTransaction();

            for(VerificationResponse r : result)
            {
                //if(!r.is_processed)
                //    continue;

                String data = "";

                if(r.matched!=null)
                {
                    r.matched.set_enrolled(r.is_enrolled);
                    data = ObjectSerializer.serialize(r.matched);
                }

                updateVerification(r.uuid, r.is_processed, r.match_found, r.matched_uuid, r.score, data);

            }

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
    public Family getMatched(String uuid) {

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select matched_uuid,data from tbl_verifications where uuid=? and is_processed=1", new String[]{uuid});

            if (cursor!=null && cursor.moveToNext()) {

                String matched_uuid = cursor.getString(0);
                String data = cursor.getString(1);
                Family restored = ObjectSerializer.deserialize(data, Family.class);
                restored.setMatch_uuid(matched_uuid);
                return restored;
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

        return null;
    }

    @Override
    public void saveVerification(String activity_code, boolean auth, Verification verification) {  //auth - for authentication purpose

        try
        {
            db.open();
            db.beginTransaction();

            long current = System.currentTimeMillis();

            String enumerator = secureStore.getEnumerator();

            ContentValues cv = verification.getInsertValues(enumerator, current);

            cv.put("activity_code", activity_code);

            cv.put("auth", auth?1:0);

            db.insert("tbl_verifications", cv);

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
    public List<Verification> getPendingVerification(String code) {

        List<Verification> result = new ArrayList<>();

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_verifications where activity_code=? and auth=1 LIMIT 1;", new String[]{code});

            if (cursor!=null && cursor.moveToNext()) {

                int matched_found = cursor.getInt(cursor.getColumnIndexOrThrow("matched_found"));
                int is_processed = cursor.getInt(cursor.getColumnIndexOrThrow("is_processed"));
                int gender = cursor.getInt(cursor.getColumnIndexOrThrow("gender"));

                String uuid = cursor.getString(cursor.getColumnIndexOrThrow("uuid"));
                String matched_uuid = cursor.getString(cursor.getColumnIndexOrThrow("matched_uuid"));
                String template = cursor.getString(cursor.getColumnIndexOrThrow("template"));
                String data = cursor.getString(cursor.getColumnIndexOrThrow("data"));

                Long inserted_on = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));

                int colIndex = cursor.getColumnIndexOrThrow("updated_on");

                Long updated_on = null;
                if (!cursor.isNull(colIndex)) {
                    updated_on = cursor.getLong(colIndex);
                }else{
                    updated_on = inserted_on;
                }

                String inserted_by = cursor.getString(cursor.getColumnIndexOrThrow("inserted_by"));

                result.add( new Verification(uuid, gender, template, inserted_on, updated_on, inserted_by,is_processed==1, matched_found==1,matched_uuid, data) );

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
    public boolean hasPendingVerification(String code) {

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select count(*) from tbl_verifications where activity_code=? and auth=1", new String[]{code});

            if (cursor!=null && cursor.moveToNext()) {

                int count = cursor.getInt(0);

                return  count > 0;

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

        return false;
    }

    @Override
    public String getMatchingId(String code, String uuid)
    {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("select household_id from tbl_distr_verif_mapping where activity_code=? and uuid=?", new String[]{code, uuid});

            if(cursor!=null && cursor.moveToNext())
            {
                return cursor.getString(0);
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

        return null;
    }

    @Override
    public void deleteVerification(String code, String uuid, BasicCallback callback) {
        try
        {
            db.open();

            db.beginTransaction();

            db.execSQL("delete from tbl_verifications where activity_code=? and uuid=?", new String[]{code, uuid});

            db.execSQL("delete from tbl_distr_verif_mapping where activity_code=? and uuid=?", new String[]{code, uuid});

           db.setTransactionSuccessful();

           callback.onSuccess();

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
            callback.onFailure(e);
        }
        finally
        {
            db.endTransaction();
            db.close();
        }

    }
}
