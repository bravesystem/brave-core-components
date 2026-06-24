package intl.iom.bravemobile.helpers;

import android.content.Context;
import android.os.Build;

import java.util.Locale;

public class LanguageHelper {


    public static String getCurrentLanguage(Context context) {
        Locale locale;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            locale = context.getResources().getConfiguration().getLocales().get(0);
        } else {
            locale = context.getResources().getConfiguration().locale;
        }

        return locale.getLanguage();   // e.g. "en"
    }


}
