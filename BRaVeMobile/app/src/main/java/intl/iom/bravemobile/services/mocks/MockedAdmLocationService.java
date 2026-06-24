package intl.iom.bravemobile.services.mocks;

import android.content.Context;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Location;

public class MockedAdmLocationService implements AdmLocationService {


    private final List<AdminLevel> adminLevels = new ArrayList<>();
    private final List<Location> locations = new ArrayList<>();


    public MockedAdmLocationService(Context context)
    {

        // Admin Levels
        adminLevels.add( new AdminLevel(1, "Country", true, true, System.currentTimeMillis()) );
        adminLevels.add( new AdminLevel(2, "State", true, true,System.currentTimeMillis()) );
        adminLevels.add( new AdminLevel(3, "City", true, false, System.currentTimeMillis()) );

        // Locations with parent relationships
        locations.add(new Location(101, "Nigeria", 1, null, true, System.currentTimeMillis()));
        locations.add(new Location(102, "Borno", 2, 101, true, System.currentTimeMillis()));
        locations.add(new Location(103, "Lagos State", 2, 101, true, System.currentTimeMillis()));
        locations.add(new Location(104, "Maiduguri", 3, 102, true, System.currentTimeMillis()));
        locations.add(new Location(105, "Lagos City", 3, 103, true, System.currentTimeMillis()));
        locations.add(new Location(106, "Abuja", 3, 101, true, System.currentTimeMillis()));

    }


    @Override
    public List<AdminLevel> getAdmLevels() {
        return adminLevels;
    }



    @Override
    public AdminLevel getAdmLevelById(int id) {
        for(AdminLevel a: adminLevels)
            if(a.id==id)
                return a;

        return null;
    }

    @Override
    public List<SelectItem> getLocations(Integer parent) {

        List<SelectItem> result = new ArrayList<>();

        for(Location l: locations)
            if(l.parent==parent)
                result.add(new SelectItem(l.id, l.name));


        return result;
    }

    @Override
    public Location getLocationById(int id) {

        for(Location l: locations)
            if(l.id==id)
                return l;

        return null;
    }
}
