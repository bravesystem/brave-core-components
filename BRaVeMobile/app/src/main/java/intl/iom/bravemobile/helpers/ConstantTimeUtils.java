package intl.iom.bravemobile.helpers;

public final class ConstantTimeUtils {

    /** Constant-time char[] compare to reduce timing side-channels. */
    public static boolean constantTimeEquals(char[] a, char[] b) {
        if (a == null || b == null) return false;
        int len = Math.max(a.length, b.length);
        int result = 0;
        for (int i = 0; i < len; i++) {
            char ca = i < a.length ? a[i] : 0;
            char cb = i < b.length ? b[i] : 0;
            result |= (ca ^ cb);
        }
        return result == 0 && a.length == b.length;
    }
}
