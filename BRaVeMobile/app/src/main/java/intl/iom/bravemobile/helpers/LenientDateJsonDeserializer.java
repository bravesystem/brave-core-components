package intl.iom.bravemobile.helpers;

import androidx.annotation.Nullable;

import com.google.gson.JsonDeserializationContext;
import com.google.gson.JsonDeserializer;
import com.google.gson.JsonElement;
import com.google.gson.JsonParseException;

import java.lang.reflect.Type;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.TimeZone;

public class LenientDateJsonDeserializer implements JsonDeserializer<Date> {

    private static SimpleDateFormat fmt(String pattern, @Nullable TimeZone tz) {
        SimpleDateFormat f = new SimpleDateFormat(pattern, Locale.US);
        if (tz != null) f.setTimeZone(tz);
        return f;
    }

    // Try several patterns: with/without millis, with/without timezone, with 'T' or space
    private static final List<SimpleDateFormat> WITH_TZ = Arrays.asList(
            fmt("yyyy-MM-dd'T'HH:mm:ss.SSSX", null),
            fmt("yyyy-MM-dd'T'HH:mm:ssX", null),
            fmt("yyyy-MM-dd HH:mm:ss.SSSX", null),
            fmt("yyyy-MM-dd HH:mm:ssX", null)
    );

    // No timezone → assume UTC
    private static final List<SimpleDateFormat> UTC_NO_TZ = Arrays.asList(
            fmt("yyyy-MM-dd'T'HH:mm:ss.SSS", TimeZone.getTimeZone("UTC")),
            fmt("yyyy-MM-dd'T'HH:mm:ss",     TimeZone.getTimeZone("UTC")),
            fmt("yyyy-MM-dd HH:mm:ss.SSS",   TimeZone.getTimeZone("UTC")),
            fmt("yyyy-MM-dd HH:mm:ss",       TimeZone.getTimeZone("UTC"))
    );

    @Override
    public Date deserialize(JsonElement json, Type typeOfT, JsonDeserializationContext context)
            throws JsonParseException {
        String s = json.getAsString();
        // 1) try formats that include timezone
        for (SimpleDateFormat f : WITH_TZ) {
            try { return f.parse(s); } catch (ParseException ignored) {}
        }
        // 2) try formats without timezone (assume UTC)
        for (SimpleDateFormat f : UTC_NO_TZ) {
            try { return f.parse(s); } catch (ParseException ignored) {}
        }
        throw new JsonParseException("Unsupported date: " + s);
    }

}