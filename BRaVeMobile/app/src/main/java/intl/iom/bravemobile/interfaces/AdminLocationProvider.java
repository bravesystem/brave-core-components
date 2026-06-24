package intl.iom.bravemobile.interfaces;

import java.util.List;

import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.registrations.AdminLevel;

public interface AdminLocationProvider {
    List<AdminLevel> getAdminLevelsUpTo(int Level);
    List<SelectItem> getOptionsFor(Integer parent);
}
