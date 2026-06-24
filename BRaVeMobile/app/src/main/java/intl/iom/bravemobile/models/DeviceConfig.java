package intl.iom.bravemobile.models;

import intl.iom.bravemobile.statics.Env;

public class DeviceConfig {

    public Env env;
    public String deviceSignature;
    public int nextId;

    public DeviceConfig(Env env, String deviceSignature, int nextId) {
        this.env = env;
        this.deviceSignature = deviceSignature;
        this.nextId = nextId;
    }
}
