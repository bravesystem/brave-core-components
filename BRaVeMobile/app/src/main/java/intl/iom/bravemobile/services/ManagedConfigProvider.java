package intl.iom.bravemobile.services;

import intl.iom.bravemobile.interfaces.AppConfigProvider;

import android.content.Context;
import android.content.RestrictionsManager;
import android.os.Bundle;

public class ManagedConfigProvider implements AppConfigProvider {

    private Bundle config;

    public ManagedConfigProvider(Context context) {
        RestrictionsManager rm = (RestrictionsManager) context.getSystemService(Context.RESTRICTIONS_SERVICE);
        config = rm.getApplicationRestrictions();
    }

    @Override
    public String getString(String key, String defaultValue) {
        return config != null ? config.getString(key, defaultValue) : defaultValue;
    }

    @Override
    public int getInt(String key, int defaultValue) {
        return config != null ? config.getInt(key, defaultValue) : defaultValue;
    }

    @Override
    public boolean getBoolean(String key, boolean defaultValue) {
        return config != null ? config.getBoolean(key, defaultValue) : defaultValue;
    }
}