package intl.iom.bravemobile.rules;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class AdmLevelParser {

    public static int parseAdmLevelId(String json) {
        JsonElement rootElement = JsonParser.parseString(json);
        if (!rootElement.isJsonObject()) {
            throw new IllegalArgumentException("Invalid JSON: root must be an object");
        }

        JsonObject root = rootElement.getAsJsonObject();

        JsonElement typeEl = root.get("type");
        String type = typeEl != null ? typeEl.getAsString() : null;
        if (!"administrative_level".equals(type)) {
            throw new IllegalArgumentException(
                    "Invalid type, expected 'administrative_level' but got: " + type
            );
        }

        JsonElement valueEl = root.get("value");
        if (valueEl == null || !valueEl.isJsonObject()) {
            throw new IllegalArgumentException("Invalid JSON: 'value' must be an object");
        }

        JsonObject valueObj = valueEl.getAsJsonObject();
        JsonElement idEl = valueObj.get("id");
        if (idEl == null || !idEl.isJsonPrimitive()) {
            throw new IllegalArgumentException("Invalid JSON: 'value.id' must be present");
        }

        try {
            return idEl.getAsInt();
        } catch (NumberFormatException ex) {
            throw new IllegalArgumentException("Invalid JSON: 'value.id' must be an int", ex);
        }
    }

}
