package intl.iom.bravemobile.models.registrations;

import java.util.HashMap;
import java.util.Map;

public class LocationAddress {
    public Map<Integer, Integer> admLocations = new HashMap<>();
    public String siteAddress;

    public void setAddress(LocationAddress _address)
    {
        admLocations.putAll(_address.admLocations);
        siteAddress = _address.siteAddress;
    }

}
