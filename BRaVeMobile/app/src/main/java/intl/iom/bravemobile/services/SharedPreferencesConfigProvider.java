package intl.iom.bravemobile.services;

import android.content.Context;
import android.content.SharedPreferences;
import android.provider.Settings;

import intl.iom.bravemobile.interfaces.AppConfigProvider;


public class SharedPreferencesConfigProvider implements AppConfigProvider {

    private SharedPreferences prefs;

    public SharedPreferencesConfigProvider(Context context) {
        prefs = context.getSharedPreferences("app_config", Context.MODE_PRIVATE);

        prefs.edit().putString("brave_esid", getDeviceId(context)).apply();

    }

    //mock unique device Id;
    private String getDeviceId(Context ctx) {
        String androidID = Settings.System.getString(ctx.getContentResolver(), Settings.Secure.ANDROID_ID);
        return androidID.toUpperCase();
    }

    @Override
    public String getString(String key, String defaultValue) {
        return prefs.getString(key, defaultValue);
    }

    @Override
    public int getInt(String key, int defaultValue) {
        return prefs.getInt(key, defaultValue);
    }

    @Override
    public boolean getBoolean(String key, boolean defaultValue) {
        return prefs.getBoolean(key, defaultValue);
    }
}

