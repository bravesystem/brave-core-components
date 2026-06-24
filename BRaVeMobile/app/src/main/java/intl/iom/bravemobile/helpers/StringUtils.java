package intl.iom.bravemobile.helpers;

public class StringUtils {
    public static boolean isBlank(String s) {
        return s == null || s.trim().isEmpty();
    }

    public static String nz(String s, String fallback) {
        return isBlank(s) ? (fallback == null ? "" : fallback) : s;
    }

    public static String joinNonEmpty(String sep, String... parts) {
        StringBuilder sb = new StringBuilder();
        for (String p : parts) {
            if (!isBlank(p)) {
                if (sb.length() > 0) sb.append(sep);
                sb.append(p);
            }
        }
        return sb.toString();
    }

    public static String truncateWithDots(String input) {
        return truncateWithDots(input, 50);
    }

    public static String truncateWithDots(String input, int len) {
        if (input == null || input.isEmpty()) {
            return null;
        }

        if (input.length() <= len) {
            return input;
        }

        return input.substring(0, len) + "...";
    }

}
