package intl.iom.bravemobile.helpers;

import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.statics.AnswerType;

public class PreferenceHelper {

    public static Object getParseValue(String value, AnswerType type) throws UnsupportedPreferenceType {

        switch (type)
        {
            case INT: return Integer.parseInt(value);
            case NUMERIC: return Double.parseDouble(value);
            case BOOLEAN: return Boolean.parseBoolean(value);
            case TEXT: return value;
            default: throw new UnsupportedPreferenceType("Unsupported preference type. Only INT, NUMERIC, BOOLEAN, and STRING are allowed.");
        }
    }
}
