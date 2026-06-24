package intl.iom.bravemobile.helpers;

import java.util.List;

import intl.iom.bravemobile.models.datapoints.TranslatedText;

public class TranslationUtils {

    private static final String EN = "en";
    private static final String NOT_FOUND = "Not found.";

    /** Exact match > default(true) > English > "Not found." */
    public static String get(List<TranslatedText> textTranslations, String lang) {
        if (textTranslations == null || textTranslations.isEmpty()) return NOT_FOUND;

        String requested = isBlank(lang) ? EN : lang;

        String english = null;       // first English text we see
        String defaultText = null;   // first isDefault==true text we see

        for (TranslatedText t : textTranslations) {
            if (t == null) continue;

            // 1) Exact language match wins immediately
            if (t.lang != null && requested.equalsIgnoreCase(t.lang)) {
                return orFallback(t.text, defaultText, english);
            }

            // 2) Track first default(true)
            /*if (defaultText == null && t.isDefault) {
                defaultText = t.text;
            }*/

            // 3) Track English
            if (english == null && t.lang != null && EN.equalsIgnoreCase(t.lang)) {
                english = t.text;
            }
        }

        // 4) Fallback chain
        if (!isBlank(defaultText)) return defaultText;
        if (!isBlank(english)) return english;
        return NOT_FOUND;
    }

    private static boolean isBlank(String s) {
        return s == null || s.trim().isEmpty();
    }

    private static String orFallback(String primary, String def, String en) {
        if (!isBlank(primary)) return primary;
        if (!isBlank(def)) return def;
        if (!isBlank(en)) return en;
        return NOT_FOUND;
    }

}
