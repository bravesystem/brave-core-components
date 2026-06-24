package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import com.auth0.jwt.JWT;
import com.auth0.jwt.interfaces.DecodedJWT;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.ActivationService;
import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.models.DeviceKeyEntity;
import intl.iom.bravemobile.models.jwt.ClaimRequest;
import intl.iom.bravemobile.models.jwt.ClaimResponse;
import intl.iom.bravemobile.models.jwt.NonceResponse;
import intl.iom.bravemobile.ui.SplashScreen;
import retrofit2.Call;
import retrofit2.Response;
import retrofit2.Retrofit;

public class SqlActivationService implements ActivationService {

    private static String TAG = SqlActivationService.class.getSimpleName();

    private DatabaseManager db;
    //private Retrofit client;

    private SecureStore secureStore;

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();
    public SqlActivationService(Context context)
    {
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);

        /*RetrofitService retrofitService = null;
        try
        {

            retrofitService = new RetrofitService(context);

            client = retrofitService.getClient();

        } catch (GeneralSecurityException e) {
            throw new RuntimeException(e);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }*/


    }

    @Override
    public void cleardata() {
        try
        {
            db.open();
            db.beginTransaction();

            db.execSQL("delete from tbl_enumerators");
            //db.execSQL("delete from tbl_jwt_tokens");
            //db.execSQL("delete from tbl_current_household_id");

            db.execSQL("delete from tbl_admin_locations");
            db.execSQL("delete from tbl_admin_levels");
            db.execSQL("delete from tbl_lookup_names");
            db.execSQL("delete from tbl_lookup_values");
            db.execSQL("delete from tbl_datasets");
            db.execSQL("delete from tbl_consents");
            db.execSQL("delete from tbl_surveys");
            db.execSQL("delete from tbl_datapoints");
            db.execSQL("delete from tbl_preferences");

            ///
            db.execSQL("delete from tbl_registration_activities");
            db.execSQL("delete from tbl_survey_answers");
            db.execSQL("delete from tbl_consent_feedbacks");
            db.execSQL("delete from tbl_individuals");
            db.execSQL("delete from tbl_households");

            db.setTransactionSuccessful();;
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
    public boolean hasData() {
        String[] tablesToCheck = {
                "tbl_households",
                "tbl_individuals",
                "tbl_consent_feedbacks",
                "tbl_survey_answers",
                "tbl_verifications",
                "tbl_distribution_assistances"
        };

        Cursor cursor = null;

        try {
            db.open();

            for (String table : tablesToCheck) {
                cursor = db.rawQuery(
                        "SELECT 1 FROM " + table + " LIMIT 1",
                        null
                );

                if (cursor != null && cursor.moveToFirst()) {
                    return true; // Data found in at least one table
                }

                if (cursor != null) {
                    cursor.close();
                    cursor = null;
                }
            }
        } catch (Exception e) {
            Log.e(TAG, "hasData error: " + e.getMessage());
        } finally {
            if (cursor != null) {
                cursor.close();
            }
            db.close();
        }

        return false; // No data in all checked tables
    }


    @Override
    public DeviceActivationResult activate(Retrofit client, String env, String claim) {

        // Export public key (PEM) to send to server
        String publicBase64 = null;
        try {
            publicBase64 = KeyStoreHelper.getPublicKeyPem();
        } catch (Exception e) {
            throw new RuntimeException(e);
        }

        EnrolledApis endpoints = client.create(EnrolledApis.class);

        ClaimRequest request = new ClaimRequest(claim, secureStore.getDeviceId(), publicBase64);

        Call<ClaimResponse> call = endpoints.enroll(
                request
        );

        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {

            CompletableFuture<DeviceActivationResult> future = CompletableFuture.supplyAsync(() -> {
                try
                {
                    Response<ClaimResponse> response = call.execute();

                    if(response.isSuccessful())
                    {
                        ClaimResponse result = response.body();

                        secureStore.setServerPublicKey(env, result.pubRsaKeyB64);  //?  --ok

                        secureStore.setHouseholdPrefix(env, result.householdPrefix); //ok

                        String token = result.jwsToken; //ok

                        DecodedJWT jwt = JWT.decode(token);
                        Integer tenantId = jwt.getClaim("tenantId").asInt();

                        secureStore.setTenantId(env,tenantId);  //? --ok

                        secureStore.saveTokens(env, token, result.refreshToken); //ok

                        if(StringUtils.isBlank(result.refreshToken)) //ok
                        {
                            throw new RuntimeException();
                        }

                        save(env,result.initialId); //ok

                        secureStore.setProvisioned(env,true);

                        return new DeviceActivationResult(
                                DeviceActivationResult.Status.SUCCESS,
                                result.householdPrefix,
                                result.initialId,
                                result.pubRsaKeyB64, // sample public key
                                "Device successfully activated."
                        );
                    }

                    return GetActivationErrorResult(response.code());

                }
                catch (IOException e) {
                    return new DeviceActivationResult(
                            DeviceActivationResult.Status.NETWORK_ERROR,
                            "Unable to connect to the server. Please check your internet connection and try again."
                    );
                }
            });

            return future.join(); // blocks until result is ready
        }



        return new DeviceActivationResult(
                DeviceActivationResult.Status.UNKNOWN_ERROR,
                "Some features are not supported on your device. Please update to Android 7.0 or later."
        );



    }

    private void save(String env, int initialId) {

        try
        {
            db.open();

            ContentValues cv = new ContentValues();
            cv.put("environment", env);
            cv.put("current_id", initialId);
            cv.put("updated_on",System.currentTimeMillis());

            db.replace("tbl_current_household_id",cv);

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

    private DeviceActivationResult GetActivationErrorResult(int code) {

        if(code==404)
            return new DeviceActivationResult(
                    DeviceActivationResult.Status.INVALID_CLAIM_CODE,
                    "Unknown or Expired claim code"
            );

        if(code==409)
            return new DeviceActivationResult(
                    DeviceActivationResult.Status.DEVICE_ALREADY_ACTIVATED,
                    "Device already claimed"
            );


        if(code==410)
            return new DeviceActivationResult(
                    DeviceActivationResult.Status.CAPACITY_EXCEEDED,
                    "Session capacity exceeded."
            );

        if(code==430)
            return new DeviceActivationResult(
                    DeviceActivationResult.Status.HOUSEHOLD_BINDING_POOL_EXHAUSTED,
                    "Household binding pool exhausted."
            );

        return new DeviceActivationResult(
                DeviceActivationResult.Status.NETWORK_ERROR,
                "Unexpected error occurred during claim."
        );
    }

    @Override
    public boolean isActive(String env) {

        boolean result = false;

        try
        {
            db.open();

            //db.execSQL("delete from tbl_current_household_id");

            Cursor cursor = db.rawQuery("Select count(*) from tbl_current_household_id where environment=?", new String[]{env});

            cursor.moveToNext();

            int cnt = cursor.getInt(0);

            result = cnt>0;

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
}
