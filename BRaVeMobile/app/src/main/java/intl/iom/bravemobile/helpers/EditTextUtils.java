package intl.iom.bravemobile.helpers;

import android.text.TextUtils;
import android.widget.EditText;

public final class EditTextUtils {

    /** Returns trimmed text; never null. If empty or ET is null, returns "" */
    public static String text(EditText et) {
        if (et == null || et.getText() == null) return "";
        return et.getText().toString().trim();
    }

    /** Returns trimmed text or null when empty / ET is null */
    public static String textOrNull(EditText et) {
        String s = text(et);
        return s.isEmpty() ? null : s;
    }

    /** Returns trimmed text; if empty/null, returns the provided default */
    public static String textOrDefault(EditText et, String def) {
        String s = text(et);
        return s.isEmpty() ? def : s;
    }


    public static boolean Empty(EditText et) {
        if (et == null) return true;
        CharSequence cs = et.getText();
        String s = cs == null ? "" : cs.toString().trim();
        return TextUtils.isEmpty(s);
    }


}
