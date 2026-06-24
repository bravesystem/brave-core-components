package intl.iom.bravemobile.helpers;

import com.google.gson.JsonDeserializationContext;
import com.google.gson.JsonDeserializer;
import com.google.gson.JsonElement;
import com.google.gson.JsonParseException;

import java.lang.reflect.Type;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class DateDeserializer implements JsonDeserializer<Date> {

    private final SimpleDateFormat format =
            new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US);

    @Override
    public Date deserialize(JsonElement json, Type typeOfT,
                            JsonDeserializationContext context)
            throws JsonParseException {
        try {
            return format.parse(json.getAsString());
        } catch (ParseException e) {
            throw new JsonParseException(e);
        }
    }
}