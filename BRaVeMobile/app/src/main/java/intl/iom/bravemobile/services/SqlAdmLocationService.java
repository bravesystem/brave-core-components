package intl.iom.bravemobile.services;

import android.content.Context;
import android.database.Cursor;
import android.util.Log;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Location;

public class SqlAdmLocationService implements AdmLocationService {

    private static String TAG = SqlAdmLocationService.class.getSimpleName();
    private DatabaseManager db;
    public SqlAdmLocationService(Context context)
    {
        db = new DatabaseManager(context);
    }

    @Override
    public List<AdminLevel> getAdmLevels() {

        List<AdminLevel> adminLevels = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_admin_levels");

            while(cursor!=null && cursor.moveToNext())
            {

                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String name = cursor.getString(cursor.getColumnIndexOrThrow("name"));
                boolean is_active = cursor.getInt(cursor.getColumnIndexOrThrow("is_active"))==1;

                adminLevels.add(new AdminLevel(id, name, is_active, is_active));

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return adminLevels;
    }

    @Override
    public AdminLevel getAdmLevelById(int id) {
        for(AdminLevel a: getAdmLevels())
            if(a.id==id)
                return a;

        return null;
    }

    public List<Location> getAllLocations() {

        List<Location> locations = new ArrayList<>();

        try
        {
            db.open();

            Cursor cursor = db.rawQuery("Select * from tbl_admin_locations");

            while(cursor!=null && cursor.moveToNext())
            {

                int id = cursor.getInt(cursor.getColumnIndexOrThrow("id"));
                String name = cursor.getString(cursor.getColumnIndexOrThrow("name"));
                int level = cursor.getInt(cursor.getColumnIndexOrThrow("level"));

                int parentCol = cursor.getColumnIndexOrThrow("parent");

                Integer parent = null;

                if (!cursor.isNull(parentCol)) {
                    parent = cursor.getInt(cursor.getColumnIndexOrThrow("parent"));
                }

                boolean is_active = cursor.getInt(cursor.getColumnIndexOrThrow("is_active"))==1;

                locations.add(new Location(id, name,level, parent, is_active));

            }

        }
        catch (Exception e)
        {
            Log.e(TAG, e.getMessage());
        }
        finally {
            db.close();
        }

        return locations;
    }

    @Override
    public List<SelectItem> getLocations(Integer parent) {
        List<SelectItem> result = new ArrayList<>();

        for(Location l: getAllLocations())
            if(l.parent!=null && l.parent.equals(parent)  || parent==null && l.parent==null)
                result.add(new SelectItem(l.id, l.name));


        return result;
    }

    @Override
    public Location getLocationById(int id) {
        for(Location l: getAllLocations())
            if(l.id==id)
                return l;

        return null;
    }
}
