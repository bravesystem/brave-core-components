package intl.iom.bravemobile.services;

import android.content.Context;
import android.database.Cursor;
import android.os.Build;
import android.util.Log;

import java.util.ArrayList;
import java.util.Date;
import java.util.Dictionary;
import java.util.HashMap;
import java.util.Hashtable;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.models.registrations.Household;

public class SqlLookupService implements LookupService {

    private static String TAG = SqlLookupService.class.getSimpleName();

    private final Map<Integer, List<SelectItem>> byId = new HashMap<>();
    // Store by lowercase name for case-insensitive matches
    private final Map<String, List<SelectItem>> byName = new HashMap<>();

    SecureStore secureStore;
    private DatabaseManager db;
    public  SqlLookupService(Context context)
    {
        db = new DatabaseManager(context);
        secureStore = ServiceLocator.secureStore(context);
    }

    @Override
    public void loadAll() {

        Dictionary<Integer, String > tmp = new Hashtable<>();

        try
        {

            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_lookup_names");

            while (cursor.moveToNext()) {

                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String name = cursor.getString(cursor.getColumnIndexOrThrow("name"));
                boolean is_active = cursor.getInt(cursor.getColumnIndexOrThrow("is_active"))==1;

                byName.put(name, new ArrayList<>());
                byId.put(id, new ArrayList<>());
                tmp.put(id, name);
            }


            cursor = db.rawQuery("Select * from tbl_lookup_values");

            while (cursor.moveToNext()) {

                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                int lkp_id = cursor.getInt(cursor.getColumnIndexOrThrow("lookup_id"));
                String name = cursor.getString(cursor.getColumnIndexOrThrow("name"));
                boolean is_active = cursor.getInt(cursor.getColumnIndexOrThrow("is_active"))==1;

                SelectItem item = new SelectItem(id, name, false, is_active);

                byId.get(lkp_id).add(item);
                byName.get(tmp.get(lkp_id)).add(item);


            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
            byName.clear();
            byId.clear();
        }
        finally
        {
            db.close();
        }


    }

    @Override
    public Optional<List<SelectItem>> getLookupItemList(int lookupId)
    {
        if(byId.isEmpty())
            loadAll();

        List<SelectItem> items = byId.get(lookupId);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            return Optional.ofNullable(items);
        }
        return null;
    }

    @Override
    public Optional<List<SelectItem>> getLookupItemList(String lookupName)
    {
        if(byId.isEmpty())
            loadAll();

        if (lookupName == null) return null;

        List<SelectItem> items = byName.get(lookupName);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            return Optional.ofNullable(items);
        }
        return null;
    }

}
