package intl.iom.bravemobile.services;

import android.content.Context;
import android.content.RestrictionsManager;
import android.os.Bundle;

import intl.iom.bravemobile.interfaces.EnrollmentConfig;
import intl.iom.bravemobile.models.EnrollmentKeys;
import intl.iom.bravemobile.statics.Env;

public class ManagedEnrollmentConfig implements EnrollmentConfig {
    private final RestrictionsManager rm;

    public ManagedEnrollmentConfig(Context ctx) {
        this.rm = (RestrictionsManager) ctx.getSystemService(Context.RESTRICTIONS_SERVICE);
    }

    private Bundle cfg() {
        Bundle b = rm != null ? rm.getApplicationRestrictions() : null;
        return (b != null) ? b : Bundle.EMPTY;
    }

    @Override public String getApiBaseUrl(String env) {
        Bundle b = cfg();
        switch (env) {
            case "prod": return b.getString(EnrollmentKeys.PROD_API_BASE_URL, null);
            case "uat":  return b.getString(EnrollmentKeys.UAT_API_BASE_URL,  null);
            case "dev":  return b.getString(EnrollmentKeys.DEV_API_BASE_URL,  null);
            default:   return null;
        }
    }

    @Override public int getPolicyVersion() {
        return cfg().getInt(EnrollmentKeys.POLICY_VERSION, 0);
    }

    @Override public String getEnrollCode() {
        return cfg().getString(EnrollmentKeys.ENROLL_CODE, null);
    }

    @Override public boolean isReady() {
        return getEnrollCode() != null
                && (getProdApiBaseUrl() != null || getUatApiBaseUrl() != null || getDevApiBaseUrl() != null);
    }
}
