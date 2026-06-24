package intl.iom.bravemobile.helpers;

import android.content.Context;
import android.content.Intent;

public final class IntentHelper {

    /*public static void restartApp(Context context) {
        // Get the app's launch intent (the one with CATEGORY_LAUNCHER)
        Intent launchIntent = context.getPackageManager()
                .getLaunchIntentForPackage(context.getPackageName());

        if (launchIntent != null) {
            // Clear the current task and start a new one
            launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK |
                    Intent.FLAG_ACTIVITY_CLEAR_TASK);
            context.startActivity(launchIntent);

            // Optionally remove animation for a snappier feel
            context.overridePendingTransition(0, 0);
        }

        // Finish this activity (and all parents)
        context.finishAffinity();
    }*/
}
