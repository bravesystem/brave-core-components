package intl.iom.bravemobile.helpers;

import android.content.Context;
import android.content.res.ColorStateList;
import android.view.View;
import android.widget.TextView;

import androidx.core.content.ContextCompat;
import androidx.core.view.ViewCompat;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.interfaces.DeviceConfigService;
import intl.iom.bravemobile.services.ServiceLocator;

public final class EnvUi {
    private EnvUi() {}
    public static void applyEnvironment(
            String env,
            TextView tvEnv,
            View envIndicator,
            Context context
    ) {
        if (env == null) env = "";
        String upper = env.toLowerCase();

        int colorRes;
        String label;

        switch (upper) {
            case "dev":
                label = "Environment:  DEV";
                colorRes = R.color.yellow_500;
                break;
            case "uat":
                label = "Environment:  UAT";
                colorRes = R.color.md_blue_600;
                break;
            case "ptr":
                label = "Environment:  PTR";
                colorRes = R.color.md_green_600;
                break;
            default:
                label = "Environment:  PROD";
                colorRes = R.color.md_green_600;   // define in colors.xml
                break;
        }

        tvEnv.setText(label);
        int color = ContextCompat.getColor(context, colorRes);
        ViewCompat.setBackgroundTintList(envIndicator, ColorStateList.valueOf(color));
    }
}
