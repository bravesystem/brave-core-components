package intl.iom.bravemobile.helpers;

import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;
import java.util.Optional;
import java.util.TimeZone;

public final class DateUtils {


    private static final SimpleDateFormat SDF = new SimpleDateFormat("yyyy-MM-dd", Locale.US);

    static {
        SDF.setLenient(false); // strict parsing
    }

    public static boolean isValidIsoDate(String input) {
        if (input == null) return false;
        try
        {
            // parse will accept matching format only due to non-lenient,
            // but we also verify the formatted output matches exactly to prevent partials.
            SDF.parse(input);
            return input.length() == 10; // "yyyy-MM-dd" is 10 chars; avoids "2024-02-01 12:00"
        }
        catch (ParseException e)
        {
            return false;
        }
}


    public static String toString(Date date){

        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd", Locale.US);
        String formatted = sdf.format(date);
        return  formatted;

    }


    public static Date parse(String s) throws ParseException {
        if (s == null) return null;
        String in = s.trim();
        if (in.isEmpty()) return null;

        synchronized (SDF) {
            SDF.setLenient(false);
            return SDF.parse(in);
        }
    }



    public static Date parseIso8601(String isoDate) throws ParseException {

        if(StringUtils.isBlank(isoDate))return null;

        // ISO 8601 format example: "2025-12-03T21:41:44Z"
        //SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ssX");
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss");
        sdf.setTimeZone(TimeZone.getTimeZone("UTC"));
        return sdf.parse(isoDate);
    }


    // Helper to create a UTC Date (midnight)
    public static Date dateUtc(int year, int monthZeroBased, int dayOfMonth) {
        Calendar cal = Calendar.getInstance(TimeZone.getTimeZone("UTC"));
        cal.clear();
        cal.set(year, monthZeroBased, dayOfMonth, 0, 0, 0);
        return cal.getTime();
    }

    public static long addHoursToTimestamp(long updatedOnMs, int deltaHours) {
        // Convert hours to milliseconds
        long deltaMs = deltaHours * 60L * 60L * 1000L;
        return updatedOnMs + deltaMs;
    }



    public static boolean isExpired(long expiredAtMs) {
        long nowMs = System.currentTimeMillis(); // Current time in ms (UTC)
        return nowMs > expiredAtMs;
    }


    public static String formatDateRange(DateFormat dateFmt, Date start, Optional<Date> endOpt) {
        String startStr = start != null ? dateFmt.format(start) : "—";
        String endStr = null;
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {
            endStr = (endOpt != null && endOpt.isPresent())
                    ? dateFmt.format(endOpt.get())
                    : "Open-ended";
        }
        return startStr + " — " + endStr;
    }


}
