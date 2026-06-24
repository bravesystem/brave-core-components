package intl.iom.bravemobile.services.mocks;

import android.content.Context;
import android.content.SharedPreferences;

import intl.iom.bravemobile.interfaces.EnrollmentConfig;
import intl.iom.bravemobile.models.EnrollmentKeys;
import intl.iom.bravemobile.statics.Env;

public class PrefsEnrollmentConfig implements EnrollmentConfig {

    private final SharedPreferences prefs;

    public PrefsEnrollmentConfig(Context ctx) {
        this.prefs = ctx.getSharedPreferences("enrollment_config", Context.MODE_PRIVATE);
    }

    @Override public String getApiBaseUrl(String env) {
        switch (env) {
            case "prod": return prefs.getString(EnrollmentKeys.PROD_API_BASE_URL, null);
            case "uat":  return prefs.getString(EnrollmentKeys.UAT_API_BASE_URL, null);
            case "dev":  return prefs.getString(EnrollmentKeys.DEV_API_BASE_URL, null);
            default:   return null;
        }
    }

    @Override public int getPolicyVersion() {
        return prefs.getInt(EnrollmentKeys.POLICY_VERSION, 0);
    }

    @Override public String getEnrollCode() {
        return prefs.getString(EnrollmentKeys.ENROLL_CODE, null);
    }

    @Override public boolean isReady() {
        return getEnrollCode() != null
                && (getProdApiBaseUrl() != null || getUatApiBaseUrl() != null || getDevApiBaseUrl() != null);
    }
}
