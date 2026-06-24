package intl.iom.bravemobile.helpers;

public final class EnumUtils {

    public static <E extends Enum<E>> E parseOrDefault(Class<E> type, String text, E defaultValue) {
        if (text == null) return defaultValue;
        for (E constant : type.getEnumConstants()) {
            if (constant.name().equalsIgnoreCase(text.trim())) {
                return constant;
            }
        }
        return defaultValue;
    }

}
