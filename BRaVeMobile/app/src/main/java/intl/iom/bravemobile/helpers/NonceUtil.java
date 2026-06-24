package intl.iom.bravemobile.helpers;

import android.util.Base64;
import android.util.Xml;

import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;

public final class NonceUtil {
    private static final SecureRandom RNG = new SecureRandom();

    public static String generate() {
        return generate(32);
    }
    /** Generate a N-byte random nonce, base64url (no padding). */
    public static String generate(int numBytes) {
        byte[] buf = new byte[numBytes];
        RNG.nextBytes(buf);
        return b64u(buf);
    }

    public static String b64u(byte[] b) {
        return Base64.encodeToString(b, Base64.NO_WRAP);
        //return Base64.encodeToString(b, Base64.URL_SAFE | Base64.NO_WRAP | Base64.NO_PADDING);
        //return new String(b, StandardCharsets.UTF_8);
    }
    public static byte[] decode(String s) {
        //return Base64.decode(s, Base64.URL_SAFE | Base64.NO_WRAP);
        return Base64.decode(s, Base64.NO_WRAP);
        //return s.getBytes(StandardCharsets.UTF_8);
    }
}
