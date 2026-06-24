package intl.iom.bravemobile;

import android.content.Context;
import android.util.Log;

import com.neurotec.licensing.NLicenseManager;

public final class CaptureSdk {

    private static volatile boolean inited;

    public static synchronized void init(Context ctx, boolean useTrial) {
        if (inited) return;
        Context app = ctx.getApplicationContext();
        try {
            NLicenseManager.setTrialMode(useTrial);
            System.setProperty("jna.nounpack", "true");
            System.setProperty("java.io.tmpdir", app.getCacheDir().getAbsolutePath());
            inited = true;
        } catch (Throwable t) {
            Log.e("CaptureSdk", "Init failed", t);
        }
    }
}
