package intl.iom.bravemobile.interfaces;

import java.util.List;

import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Location;

public interface AdmLocationService {
    List<AdminLevel> getAdmLevels();
    AdminLevel getAdmLevelById(int id);
    List<SelectItem> getLocations(Integer parent);
    Location getLocationById(int id);
}
