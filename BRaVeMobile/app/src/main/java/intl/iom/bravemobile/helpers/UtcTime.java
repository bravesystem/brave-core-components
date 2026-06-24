package intl.iom.bravemobile.helpers;

import android.os.Build;

import androidx.annotation.RequiresApi;

import java.time.*;
import java.time.format.DateTimeFormatter;
import java.time.format.DateTimeParseException;

@RequiresApi(api = Build.VERSION_CODES.O)
public final class UtcTime {
    private static final DateTimeFormatter ISO_Z = DateTimeFormatter.ISO_OFFSET_DATE_TIME; // e.g., ...Z or +00:00
    private static final DateTimeFormatter SQL_NO_ZONE = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss.SSS");

    /** @param s nullable string from backend */
    public static Instant parseBackendUtc(String s) {
        if (s == null || s.isEmpty()) return null;
        try {
            // Try ISO 8601 first, e.g., 2025-10-14T12:34:56.789Z
            return OffsetDateTime.parse(s, ISO_Z).toInstant();
        } catch (DateTimeParseException e) {
            // Fallback: SQL Server-style without zone, treat as UTC
            LocalDateTime ldt = LocalDateTime.parse(s, SQL_NO_ZONE);
            return ldt.toInstant(ZoneOffset.UTC);
        }
    }

    public static long toEpochMillis(Instant instant) {
        return instant == null ? 0L : instant.toEpochMilli();
    }

    public static String toIso8601Z(Instant instant) {
        return instant == null ? null : instant.toString(); // ISO-8601 with Z
    }
}
