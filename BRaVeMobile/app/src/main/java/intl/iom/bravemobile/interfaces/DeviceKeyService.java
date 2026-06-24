package intl.iom.bravemobile.interfaces;

import intl.iom.bravemobile.models.DeviceKeyEntity;

public interface DeviceKeyService {

    DeviceKeyEntity get(String alias);
    void save(String alias, String pub);
}
