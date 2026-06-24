package intl.iom.bravemobile.helpers;

import java.util.Calendar;
import java.util.Date;
import java.util.Locale;

public class MathUtils {

    public static String buildAgeLabel(Date dobOpt,
                                        Integer yearsOpt,
                                        Integer monthsOpt,
                                        Integer daysOpt) {
        // If dob present → compute years
        if (dobOpt != null ) {
            int years = yearsBetween(dobOpt, new Date());
            return String.valueOf(years);
        }
        // Else fall back to triplet (any that are present)
        boolean hasYears = yearsOpt != null;
        boolean hasMonths = monthsOpt != null;
        boolean hasDays = daysOpt != null ;

        if (!hasYears && !hasMonths && !hasDays) return "-";

        StringBuilder sb = new StringBuilder();
        if (hasYears) sb.append(yearsOpt).append("y");
        if (hasMonths) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(monthsOpt).append("m");
        }
        if (hasDays) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(daysOpt).append("d");
        }
        return sb.toString();
    }

    private static int yearsBetween(Date from, Date to) {
        Calendar c1 = Calendar.getInstance(Locale.getDefault());
        Calendar c2 = Calendar.getInstance(Locale.getDefault());
        c1.setTime(from);
        c2.setTime(to);
        int years = c2.get(Calendar.YEAR) - c1.get(Calendar.YEAR);
        // if birthday hasn’t occurred yet this year, subtract one
        if (c2.get(Calendar.DAY_OF_YEAR) < c1.get(Calendar.DAY_OF_YEAR)) {
            years--;
        }
        return Math.max(0, years);
    }
}
