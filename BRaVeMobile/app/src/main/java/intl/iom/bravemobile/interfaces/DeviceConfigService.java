package intl.iom.bravemobile.interfaces;

import android.os.Build;

import java.util.Optional;

import intl.iom.bravemobile.models.DeviceConfig;
import intl.iom.bravemobile.statics.Env;

public interface DeviceConfigService {
    Optional<DeviceConfig> current();
    Env env();
    Optional<DeviceConfig> get(Env env); //Get config for Env
    void set(DeviceConfig config); //Set config for Env

    void set(Env env);

    boolean isActive();

    default String getDeviceSignature()
    {
        Optional<DeviceConfig> device = current();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            if (device.isPresent())
            {
                return device.get().deviceSignature;
            }
        }

        return null;
    }


}
