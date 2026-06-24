package intl.iom.bravemobile.helpers;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Arrays;

public class SecretHasher {
    /*public static byte[] hashPin(String pin) throws NoSuchAlgorithmException {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        return digest.digest(pin.getBytes(java.nio.charset.StandardCharsets.UTF_8));
    }*/

    public static byte[] sha256Bytes(String pin) {
        if (pin == null) throw new IllegalArgumentException("pin is null");

        try {
            byte[] data = pin.getBytes(StandardCharsets.UTF_8);

            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            return digest.digest(data);   // returns byte[]
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException("SHA-256 not supported", e);
        }
    }

    public static boolean verifyPin(String inputPin, byte[] storedHash) throws NoSuchAlgorithmException {
        byte[] computedHash = sha256Bytes(inputPin);
        return Arrays.equals(storedHash, computedHash);
    }

}
