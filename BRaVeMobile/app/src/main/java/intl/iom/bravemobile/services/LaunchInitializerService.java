package intl.iom.bravemobile.services;

import android.content.Context;
import android.util.Log;

import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.database.DatabaseManager;
import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.interfaces.DeviceKeyService;
import intl.iom.bravemobile.models.DeviceKeyEntity;

public final class LaunchInitializerService {

    private static final String TAG = LaunchInitializerService.class.getSimpleName();
    private static final ExecutorService IO = Executors.newSingleThreadExecutor();

    private LaunchInitializerService() {}

    /** Call once at app start; safe to call every time. */
    public static void run(Context ctx) {
        IO.execute(() -> {
            try {
                // Ensure keys exist in Keystore
                KeyStoreHelper.ensureKeyPairAndGetPublicKey(ctx);

                // Persist public key PEM in DB if missing/stale
                DeviceKeyService deviceKeyService = ServiceLocator.deviceKeyService(ctx);
                DeviceKeyEntity existing = deviceKeyService.get(KeyStoreHelper.KEY_ALIAS);

                String currentPem = KeyStoreHelper.getPublicKeyPem();
                if (existing == null || (existing.publicKeyPem != null && !existing.publicKeyPem.equals(currentPem))) {
                    deviceKeyService.save(KeyStoreHelper.KEY_ALIAS, currentPem);
                    Log.i(TAG, "Saved/updated public key in DB.");
                } else {
                    Log.i(TAG, "Public key already present in DB.");
                }
            } catch (Exception e) {
                Log.e(TAG, "Key init failed", e);
            }
        });
    }
}
