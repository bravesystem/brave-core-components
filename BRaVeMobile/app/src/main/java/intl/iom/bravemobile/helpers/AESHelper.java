package intl.iom.bravemobile.helpers;

import android.util.Base64;

import java.nio.charset.StandardCharsets;
import java.security.InvalidAlgorithmParameterException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.PublicKey;
import java.security.SecureRandom;

import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.KeyGenerator;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

public class AESHelper {

    private static final String AES_TRANSFORMATION = "AES/GCM/NoPadding";
    private static final int AES_KEY_SIZE = 256;          // 128 if 256 not available
    private static final int GCM_TAG_LENGTH = 128;        // bits
    private static final int IV_LENGTH_BYTES = 12;        // 96-bit GCM nonce

    public static AESPacket getPair() throws NoSuchAlgorithmException {

        // 1. Generate AES key
        KeyGenerator kg = KeyGenerator.getInstance("AES");
        kg.init(AES_KEY_SIZE);
        SecretKey aesKey = kg.generateKey();
        byte[] aesKeyBytes = aesKey.getEncoded(); // THIS is what gets RSA-encrypted

        // 2. Generate IV
        byte[] iv = new byte[IV_LENGTH_BYTES];
        new SecureRandom().nextBytes(iv);

        AESPacket packet = new AESPacket();
        packet.encKey = aesKeyBytes;
        packet.iv = iv;
        packet.aesKey= aesKey;
        //packet.ivBase64 = Base64.encodeToString(iv, Base64.NO_WRAP);
        //packet.encKeyBase64 = Base64.encodeToString(aesKeyBytes, Base64.NO_WRAP);

        return packet;

    }

    public static String encryptPayload(SecretKey aesKey, byte[] iv, String plainText) throws Exception {
        // 3. AES-GCM encrypt payload
        Cipher aesCipher = Cipher.getInstance(AES_TRANSFORMATION);
        GCMParameterSpec gcmSpec = new GCMParameterSpec(GCM_TAG_LENGTH, iv);
        aesCipher.init(Cipher.ENCRYPT_MODE, aesKey, gcmSpec);
        byte[] cipherBytes = aesCipher.doFinal(plainText.getBytes(StandardCharsets.UTF_8));

        return Base64.encodeToString(cipherBytes, Base64.NO_WRAP);
    }

    public static String encryptEAS( byte[] aesKeyBytes, String pubRsaKeyB64 ) throws Exception {

        PublicKey serverPublicKey = RsaKeyLoader.loadRsaPublicKeyFromBase64(pubRsaKeyB64);

        // 4. RSA-encrypt AES key bytes
        Cipher rsaCipher = Cipher.getInstance("RSA/ECB/OAEPWithSHA-256AndMGF1Padding");
        rsaCipher.init(Cipher.ENCRYPT_MODE, serverPublicKey);
        byte[] encKeyBytes = rsaCipher.doFinal(aesKeyBytes);

        return Base64.encodeToString(encKeyBytes, Base64.NO_WRAP);
    }
}
