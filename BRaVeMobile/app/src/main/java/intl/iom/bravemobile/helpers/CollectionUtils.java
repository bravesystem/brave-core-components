package intl.iom.bravemobile.helpers;

import java.time.LocalDate;
import java.time.Period;
import java.time.ZoneId;
import java.util.Date;
import java.util.List;
import java.util.Locale;

public final class CollectionUtils {

    public static void toLowerCaseInPlace(List<String> list) {
        if (list == null) return;

        for (int i = 0; i < list.size(); i++) {
            String s = list.get(i);
            if (s != null) {
                list.set(i, s.toLowerCase(Locale.ROOT));
            }
        }
    }

    public static void toUpperCaseInPlace(List<String> list) {
        if (list == null) return;

        for (int i = 0; i < list.size(); i++) {
            String s = list.get(i);
            if (s != null) {
                list.set(i, s.toUpperCase(Locale.ROOT));
            }
        }
    }

}
