package intl.iom.bravemobile.helpers;

import android.util.Base64;

public final class Bytes {
    // Hex
    public static String toHex(byte[] b) {
        StringBuilder sb = new StringBuilder(b.length * 2);
        for (byte x : b) sb.append(String.format("%02x", x));
        return sb.toString();
    }
    public static byte[] fromHex(String hex) {
        if (hex.length() % 2 != 0) throw new IllegalArgumentException("Hex length must be even");
        byte[] out = new byte[hex.length() / 2];
        for (int i = 0; i < out.length; i++)
            out[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
        return out;
    }


    /**
     * Encode a byte[] to Base64 string (standard).
     * Uses NO_WRAP to avoid inserting newlines.
     */
    public static String toBase64(byte[] bytes) {
        if (bytes == null || bytes.length == 0) return "";
        return Base64.encodeToString(bytes, Base64.NO_WRAP);
    }

    /**
     * Decode a Base64 string to byte[].
     * Returns empty array for null/blank input.
     * Throws IllegalArgumentException on invalid Base64.
     */
    public static byte[] fromBase64(String base64) {
        if (base64 == null || base64.trim().isEmpty()) return new byte[0];
        return Base64.decode(base64, Base64.DEFAULT); // DEFAULT tolerates line breaks/padding
    }




}
