package intl.iom.bravemobile.interfaces;


import intl.iom.bravemobile.statics.Env;

public interface EnrollmentConfig
{
    /** Returns the base URL for the given environment, or null if missing. */
    String getApiBaseUrl(String env);

    /** Convenience getters (optional). */
    default String getProdApiBaseUrl() { return getApiBaseUrl(Env.PROD); }
    default String getUatApiBaseUrl()  { return getApiBaseUrl(Env.UAT); }
    default String getDevApiBaseUrl()  { return getApiBaseUrl(Env.DEV); }

    /** Policy version (0 if missing/unset). */
    int getPolicyVersion();

    /** Enrollment code (may be null/empty if not provisioned yet). */
    String getEnrollCode();

    /** True if at least one base URL + enroll code are present. */
    boolean isReady();
}
