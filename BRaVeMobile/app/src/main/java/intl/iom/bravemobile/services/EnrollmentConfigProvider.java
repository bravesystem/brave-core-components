package intl.iom.bravemobile.services;

import android.content.Context;

import intl.iom.bravemobile.interfaces.EnrollmentConfig;
import intl.iom.bravemobile.services.mocks.PrefsEnrollmentConfig;

public final class EnrollmentConfigProvider {
    private static volatile EnrollmentConfig INSTANCE;

    private EnrollmentConfigProvider() {}

    public static EnrollmentConfig get(Context ctx) {
        if (INSTANCE == null) {
            synchronized (EnrollmentConfigProvider.class) {
                if (INSTANCE == null) {
                    // Default to prefs during development.
                    // Flip to ManagedEnrollmentConfig when you integrate with Intune,
                    // or choose via build flavor/flag.
                    INSTANCE = new PrefsEnrollmentConfig(ctx.getApplicationContext());
                    // INSTANCE = new ManagedEnrollmentConfig(ctx.getApplicationContext());
                }
            }
        }
        return INSTANCE;
    }

}
