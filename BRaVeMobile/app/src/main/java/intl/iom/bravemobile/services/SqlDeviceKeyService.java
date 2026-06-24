package intl.iom.bravemobile.services;

import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.interfaces.DeviceKeyService;
import intl.iom.bravemobile.models.DeviceKeyEntity;
import intl.iom.bravemobile.models.registrations.Household;

public class SqlDeviceKeyService implements DeviceKeyService {

    private static String TAG = SqlDeviceKeyService.class.getSimpleName();

    private DatabaseManager db;
    public SqlDeviceKeyService(Context context)
    {
        db = new DatabaseManager(context);
    }

    @Override
    public DeviceKeyEntity get(String alias) {

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_device_key");

            cursor.moveToNext();

            DeviceKeyEntity key = new DeviceKeyEntity();

            key.alias = cursor.getString(cursor.getColumnIndexOrThrow("alias"));
            key.publicKeyPem = cursor.getString(cursor.getColumnIndexOrThrow("public_key_pm"));
            key.createdAtMillis = cursor.getLong(cursor.getColumnIndexOrThrow("inserted_on"));

            return key;
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
    public void save(String alias, String pub) {

        try
        {
            db.open();

            ContentValues cv = new ContentValues();
            cv.put("alias", alias);
            cv.put("public_key_pm", pub);
            cv.put("inserted_on",System.currentTimeMillis());

            db.replace("tbl_device_key",cv);

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

    }
}
