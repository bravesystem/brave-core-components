package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;
import android.widget.Toast;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.security.NoSuchAlgorithmException;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.ConstantTimeUtils;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.helpers.SecretHasher;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.DeviceKeyEntity;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.ExtendExpiryRequest;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.models.SetPinRequest;
import intl.iom.bravemobile.models.jwt.ClaimRequest;
import intl.iom.bravemobile.models.jwt.ClaimResponse;
import intl.iom.bravemobile.ui.activities.ActivityListPage;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;

public class SqlEnumeratorService implements EnumeratorService {

    private static final int DELTA_HOURS = 16; //8 hours
    private static String TAG = SqlEnumeratorService.class.getSimpleName();

    private DatabaseManager db;
    private Retrofit client;

    private Context _context;

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();
    public SqlEnumeratorService(Context context)
    {

        _context = context;

        db = new DatabaseManager(context);

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
    public List<Enumerator> listAll() {

        List<Enumerator> enumerators = new ArrayList<>();
        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_enumerators");

            while (cursor.moveToNext())
            {

                Enumerator enumerator = new Enumerator();
                enumerator.code = cursor.getString(cursor.getColumnIndexOrThrow("code"));
                enumerator.fullName = cursor.getString(cursor.getColumnIndexOrThrow("fullName"));
                enumerator.photoBase64 = cursor.getString(cursor.getColumnIndexOrThrow("photoBase64"));
                enumerator.note = cursor.getString(cursor.getColumnIndexOrThrow("note"));
                enumerator.isSupervisor = cursor.getInt(cursor.getColumnIndexOrThrow("isSupervisor"))==1;
                enumerator.isActive = cursor.getInt(cursor.getColumnIndexOrThrow("isActive"))==1;
                enumerator.isPinUpdated = cursor.getInt(cursor.getColumnIndexOrThrow("isPinUpdated"))==1;
                enumerator.enumeratorPin = cursor.getBlob(cursor.getColumnIndexOrThrow("enumeratorPin"));
                enumerator.lastUpdated = cursor.getLong(cursor.getColumnIndexOrThrow("UpdatedOnMs"));
                enumerator.expiredAt = DateUtils.addHoursToTimestamp(enumerator.lastUpdated, DELTA_HOURS);

                enumerators.add(enumerator);
            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return enumerators;
    }

    @Override
    public void refreshList(BasicCallback callback)
    {

        IO.execute(() -> {

            int Flag = 0;

            try
            {

                List<Enumerator> enumerators = listAll();

                if(enumerators.size()>0)
                    Flag = 1;

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Response<List<Enumerator>> call = endpoints.getEnumeratorList(Flag).execute();

                if(call.isSuccessful()){

                    List<Enumerator> result =  call.body();

                    saveEnumerators(result);

                    callback.onSuccess();
                }
                else
                {
                   // callback.onFailure(new Exception(String.format("Http error: code %d, message :%s",call.code(),call.errorBody())));
                    callback.onFailure(new Exception(_context.getString(R.string.brave_server_error)));
                }


            }
            catch (Exception e)
            {
                callback.onFailure(new Exception(_context.getString(R.string.brave_network_error)));
            }


        });


    }

    @Override
    public Enumerator getEnumerator(String code) {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_enumerators where code=?", new String[]{code});

            cursor.moveToNext();

            Enumerator enumerator = new Enumerator();
            enumerator.code = cursor.getString(cursor.getColumnIndexOrThrow("code"));
            enumerator.fullName = cursor.getString(cursor.getColumnIndexOrThrow("fullName"));
            enumerator.photoBase64 = cursor.getString(cursor.getColumnIndexOrThrow("photoBase64"));
            enumerator.note = cursor.getString(cursor.getColumnIndexOrThrow("note"));
            enumerator.isSupervisor = cursor.getInt(cursor.getColumnIndexOrThrow("isSupervisor"))==1;
            enumerator.isActive = cursor.getInt(cursor.getColumnIndexOrThrow("isActive"))==1;
            enumerator.isPinUpdated = cursor.getInt(cursor.getColumnIndexOrThrow("isPinUpdated"))==1;
            enumerator.enumeratorPin = cursor.getBlob(cursor.getColumnIndexOrThrow("enumeratorPin"));
            enumerator.lastUpdated = cursor.getLong(cursor.getColumnIndexOrThrow("UpdatedOnMs"));
            enumerator.expiredAt = DateUtils.addHoursToTimestamp(enumerator.lastUpdated, DELTA_HOURS);

            return enumerator;

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
    public void saveEnumerators(List<Enumerator> enumerators) {
        try
        {
            db.open();
            db.beginTransaction();

            for(Enumerator e: enumerators)
            {
                ContentValues contentValues = e.getContentValues();
                db.replace("tbl_enumerators", contentValues);
            }

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
    public PinVerificationResult verifyPin(String code, char[] pin) {
        return null;
    }

    @Override
    public PinVerificationResult verifyInputPin(String code, String inputPin) {

        try {

            // Basic validation
            if (code == null || code.trim().isEmpty()) {
                return PinVerificationResult.notFound("Enumerator code is required.");
            }
            if (StringUtils.isBlank(inputPin)) {
                return PinVerificationResult.invalidPin("PIN is required.");
            }

            // 1) Find enumerator by code
            Enumerator match = getEnumerator(code);

            if (match == null) {
                return PinVerificationResult.notFound("Enumerator not found.");
            }

            // 2) Check active
            if (!match.isActive) {
                return PinVerificationResult.inactive("Enumerator is inactive.");
            }

            if(match.pinChangeRequired() && "0000".equals(inputPin))
            {
                return PinVerificationResult.success(match);
            }


            if(!SecretHasher.verifyPin(inputPin, match.enumeratorPin))
            {
                return PinVerificationResult.invalidPin("Invalid PIN.");
            }

            // 3) Check expiry (server-anchored time)
            if (DateUtils.isExpired(match.expiredAt)) {
                return PinVerificationResult.expired("Session expired.");
            }

            // 5) Success
            return PinVerificationResult.success(match);

        }
        catch (NoSuchAlgorithmException e)
        {
            throw new RuntimeException(e);
        }
    }

    @Override
    public void extendExpiry(ExtendExpiryRequest dto, BasicCallback callback) {

        IO.execute(() -> {

            try
            {
                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Response<List<Enumerator>> call = endpoints.extendExpiry(dto).execute();

                if(call.isSuccessful()){

                    List<Enumerator> result =  call.body();

                    saveEnumerators(result);

                    callback.onSuccess();
                }
                else
                {
                    //callback.onFailure(new Exception(String.format("Http error: code %d, message :%s", call.code(),call.errorBody())));
                    callback.onFailure(new Exception(_context.getString(R.string.brave_server_error)));
                }


            }
            catch (Exception e)
            {
                callback.onFailure(new Exception(_context.getString(R.string.brave_network_error)));
            }

        });

    }

    @Override
    public void setNewInputPin(SetPinRequest dto, BasicCallback callback) {

        boolean ok = !StringUtils.isBlank(dto.newPin) && dto.newPin.equals(dto.confirmedPin);

        IO.execute(() -> {

        try
        {
            if(ok)
            {

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Response<List<Enumerator>> call = endpoints.setPin(dto).execute();

                if(call.isSuccessful()){

                    List<Enumerator> result =  call.body();

                    saveEnumerators(result);

                    callback.onSuccess();
                }
                else
                {
                    callback.onFailure(new Exception(String.format("Http error: code %d, message :%s",call.code(),call.errorBody())));
                }

                /*PinVerificationResult result = verifyInputPin(code, newPin);

                if(result.isSuccess())
                    return result;*/

                //less likely happening
                //return PinVerificationResult.invalidPin("Invalid PIN.");
            }
        }
        catch (Exception e)
        {
            callback.onFailure(e);
        }

        });



        //return PinVerificationResult.invalidPin("PIN mismatch.");

    }

    @Override
    public PinVerificationResult setNewPin(String code, char[] oldPin, char[] newPin, char[] confirmedPin) {
        return null;
    }
}
