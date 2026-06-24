package intl.iom.bravemobile.services;

import android.content.Context;
import android.util.Log;

import java.time.Instant;

import intl.iom.bravemobile.interfaces.ActivationService;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.interfaces.AppConfigProvider;
import intl.iom.bravemobile.interfaces.ClockService;
import intl.iom.bravemobile.interfaces.DashboardService;
import intl.iom.bravemobile.interfaces.DeviceKeyService;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.services.mocks.MockedClock;

public final class ServiceLocator {
    //private static volatile DeviceConfigService deviceConfigService;
    private static String TAG = ServiceLocator.class.getSimpleName();
    private static volatile ClockService clockService;
    private static volatile EnumeratorService enumeratorService;
    private static volatile ActivationService activationService;
    private static volatile LookupService lookupService;
    private static volatile RegistrationActivityService registrationActivityService;
    private static volatile HouseholdRegistrationService householdRegistrationService;
    private static volatile VerificationService verificationService;
    private static volatile DeviceKeyService deviceKeyService;
    private static volatile AppConfigProvider appConfigProvider;
    private static volatile AdmLocationService admLocationService;
    private static volatile GpsService gpsService;
    private static volatile SecureStore secureStore;
    private static volatile DistributionService distributionService;
    private static volatile DashboardService dashboardService;



    private static void safeClose(Object service) {
        if (service instanceof AutoCloseable) {
            try {
                ((AutoCloseable) service).close();
            } catch (Exception ignored) {
                // TODO: log if you have a logger, e.g.,
                Log.w(TAG, "close failed", ignored);
            }
        }
    }


    public static void reset(boolean skipActivationService) {
        synchronized (ServiceLocator.class) {
            // ActivationService
            if(!skipActivationService)
                if (activationService != null) {
                    safeClose(activationService);
                    activationService = null;
                }

            // EnumeratorService
            if (enumeratorService != null) {
                safeClose(enumeratorService);
                enumeratorService = null;
            }

            // LookupService
            if (lookupService != null) {
                safeClose(lookupService);
                lookupService = null;
            }

            // RegistrationActivityService
            if(!skipActivationService)
                if (registrationActivityService != null) {
                    safeClose(registrationActivityService);
                    registrationActivityService = null;
                }

            // HouseholdRegistrationService
            if (householdRegistrationService != null) {
                safeClose(householdRegistrationService);
                householdRegistrationService = null;
            }

            // verificationService
            if (verificationService != null) {
                safeClose(verificationService);
                verificationService = null;
            }

            // distributionService
            if (distributionService != null) {
                safeClose(distributionService);
                distributionService = null;
            }

            // AdmLocationService
            if (admLocationService != null) {
                safeClose(admLocationService);
                admLocationService = null;
            }
        }
    }


    private ServiceLocator() {}

    public static SecureStore secureStore(Context context) {
        if (secureStore == null) {
            synchronized (ServiceLocator.class) {
                if (secureStore == null) {
                    secureStore = new SecureStore(context);
                }
            }
        }
        return secureStore;
    }

    public static GpsService gpsService(Context context) {
        if (gpsService == null) {
            synchronized (ServiceLocator.class) {
                if (gpsService == null) {
                    gpsService = new GpsService(context);
                }
            }
        }
        return gpsService;
    }

    public static AdmLocationService admLocationService(Context context) {
        if (admLocationService == null) {
            synchronized (ServiceLocator.class) {
                if (admLocationService == null) {
                    admLocationService = new SqlAdmLocationService(context);
                }
            }
        }
        return admLocationService;
    }

    public static AppConfigProvider appConfigProvider(Context context) {
        if (appConfigProvider == null) {
            synchronized (ServiceLocator.class) {
                if (appConfigProvider == null) {
                    appConfigProvider = new SharedPreferencesConfigProvider(context);
                }
            }
        }
        return appConfigProvider;
    }

    public static DeviceKeyService deviceKeyService(Context context) {
        if (deviceKeyService == null) {
            synchronized (ServiceLocator.class) {
                if (deviceKeyService == null) {
                    deviceKeyService = new SqlDeviceKeyService(context);
                }
            }
        }
        return deviceKeyService;
    }

    public static HouseholdRegistrationService householdRegistrationService(Context context) {
        if (householdRegistrationService == null) {
            synchronized (ServiceLocator.class) {
                if (householdRegistrationService == null) {
                    householdRegistrationService = new SqlHouseholdRegistrationService(context);
                }
            }
        }
        return householdRegistrationService;
    }

    public static VerificationService verificationService(Context context) {
        if (verificationService == null) {
            synchronized (ServiceLocator.class) {
                if (verificationService == null) {
                    verificationService = new SqlVerificationService(context);
                }
            }
        }
        return verificationService;
    }

    public static DistributionService distributionService(Context context) {
        if (distributionService == null) {
            synchronized (ServiceLocator.class) {
                if (distributionService == null) {
                    distributionService = new SqlDistributionService(context);
                }
            }
        }
        return distributionService;
    }


    public static RegistrationActivityService registrationActivityService(Context context) {
        if (registrationActivityService == null) {
            synchronized (ServiceLocator.class) {
                if (registrationActivityService == null) {
                    registrationActivityService = new SqlRegistrationActivityService(context);
                }
            }
        }
        return registrationActivityService;
    }

    public static LookupService lookupService(Context context) {
        if (lookupService == null) {
            synchronized (ServiceLocator.class) {
                if (lookupService == null) {
                    lookupService = new SqlLookupService(context);
                }
            }
        }
        return lookupService;
    }

    public static ClockService clockService() {
        if (clockService == null) {
            synchronized (ServiceLocator.class) {
                if (clockService == null) {
                    MockedClock clock = null;
                    if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
                        clock = new MockedClock(Instant.parse("2025-10-14T00:00:00Z").toEpochMilli());
                    }
                    clockService = clock;
                }
            }
        }
        return clockService;
    }

    public static ActivationService activationService(Context context) {
        if (activationService == null) {
            synchronized (ServiceLocator.class) {
                if (activationService == null) {
                    activationService = new SqlActivationService(context);
                    //activationService = new MockedActivationService();
                }
            }
        }
        return activationService;
    }

    public static DashboardService dashboardService(Context context) {
        if (dashboardService == null) {
            synchronized (ServiceLocator.class) {
                if (dashboardService == null) {
                    dashboardService = new SqlDashboardService(context);
                }
            }
        }
        return dashboardService;
    }

    public static EnumeratorService enumeratorService(Context context) {
        if (enumeratorService == null) {
            synchronized (ServiceLocator.class) {
                if (enumeratorService == null) {
                    enumeratorService = new SqlEnumeratorService( context);
                }
            }
        }
        return enumeratorService;
    }


}
