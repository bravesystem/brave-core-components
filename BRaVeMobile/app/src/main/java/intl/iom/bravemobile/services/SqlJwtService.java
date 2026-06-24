package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Base64;
import android.util.Log;

import org.json.JSONException;
import org.json.JSONObject;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.interfaces.JwtService;
import intl.iom.bravemobile.models.jwt.JwtRefreshPair;

public class SqlJwtService implements JwtService {

    private static  String TAG = SqlJwtService.class.getSimpleName();

    private DatabaseManager db;

    public SqlJwtService(Context context)
    {
        db = new DatabaseManager(context);
    }
    @Override
    public JwtRefreshPair getJwt(String environment) {
        try
        {
            db.open();

            Cursor cursor = db.rawQuery("tbl_jwt_tokens", new String[]{environment});

            cursor.moveToNext();

            JwtRefreshPair jwtRefreshPair = new JwtRefreshPair();

            jwtRefreshPair.jwt =  cursor.getString(cursor.getColumnIndexOrThrow("jwt"));
            jwtRefreshPair.refresh =  cursor.getString(cursor.getColumnIndexOrThrow("refresh"));

            return jwtRefreshPair;

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
    public void saveJwt(String environment, String jwtToken, String refreshToken) {
        try
        {
            db.open();

            ContentValues cv = new ContentValues();
            cv.put("environment", environment);
            cv.put("jwt", jwtToken);
            cv.put("refresh", refreshToken);
            cv.put("updated_on",System.currentTimeMillis());

            db.replace("tbl_jwt_tokens",cv);

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }
    }

    public static boolean isJwtExpired(String jwtToken, int skewSeconds) {

        if (jwtToken == null) return true;

        try {
            long exp = getExpSeconds(jwtToken);
            long now = System.currentTimeMillis() / 1000L;
            return exp == 0 || now >= (exp - skewSeconds);
        } catch (JSONException e) {
            Log.e(TAG, "Failed to parse JWT", e);
            return true;
        }

    }

    private static long getExpSeconds(String jwt) throws JSONException {
        if (jwt == null) return 0;
        String[] parts = jwt.split("\\.");
        byte[] decodedBytes = Base64.decode(parts[1], Base64.URL_SAFE);
        String payloadJson = new String(decodedBytes, java.nio.charset.StandardCharsets.UTF_8);
        JSONObject obj = new JSONObject(payloadJson);
        return obj.optLong("exp", 0L);
    }

}
