package intl.iom.bravemobile.helpers;

import com.google.gson.Gson;
import com.google.gson.JsonSyntaxException;

import java.lang.reflect.Type;

public final class ObjectSerializer {
    private static final Gson gson = new Gson();

    private ObjectSerializer() {}

    /** Serialize any object to JSON string */
    public static String serialize(Object obj) {
        return gson.toJson(obj);
    }

    /** Deserialize JSON string into the given class */
    public static <T> T deserialize(String json, Class<T> clazz) {
        try {
            return gson.fromJson(json, clazz);
        } catch (JsonSyntaxException e) {
            return null; // or throw custom exception
        }
    }
    public static <T> T deserialize(String json, Type typeOfT) {
        try {
            return gson.fromJson(json, typeOfT);
        } catch (JsonSyntaxException e) {
            return null; // or throw custom exception
        }
    }



}

