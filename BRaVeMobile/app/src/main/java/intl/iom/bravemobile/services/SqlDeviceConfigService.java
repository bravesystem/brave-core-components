package intl.iom.bravemobile.services;

import android.content.Context;

import java.util.Optional;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.interfaces.DeviceConfigService;
import intl.iom.bravemobile.models.DeviceConfig;
import intl.iom.bravemobile.statics.Env;

public class SqlDeviceConfigService implements DeviceConfigService {

    DatabaseManager db;
    public SqlDeviceConfigService(Context context)
    {
        db = new DatabaseManager(context);
    }
    @Override
    public Optional<DeviceConfig> current() {
        return Optional.empty();
    }

    @Override
    public Env env() {
        return null;
    }

    @Override
    public Optional<DeviceConfig> get(Env env) {
        return Optional.empty();
    }

    @Override
    public void set(DeviceConfig config) {

    }

    @Override
    public void set(Env env) {

    }

    @Override
    public boolean isActive() {
        return false;
    }
}
