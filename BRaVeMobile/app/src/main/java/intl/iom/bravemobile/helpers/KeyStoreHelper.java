package intl.iom.bravemobile.helpers;

import android.content.Context;
import android.os.Build;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.security.keystore.KeyProtection;

import java.security.KeyFactory;
import java.security.KeyPair;
import java.security.KeyPairGenerator;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.PublicKey;
import java.security.spec.X509EncodedKeySpec;
import java.util.Base64;

import javax.crypto.Cipher;

public final class KeyStoreHelper {
    public static final String ANDROID_KEYSTORE = "AndroidKeyStore";
    public static final String KEY_ALIAS = "BRAVE_DEVICE_KEY";
    public static final String SERVER_KEY = "SERVER_PUBLIC_KEY";

    private KeyStoreHelper() {}


    /** Ensure a keypair exists; create if missing, and return the current public key. */
    public static PublicKey ensureKeyPairAndGetPublicKey(Context ctx) throws Exception {
        KeyStore ks = KeyStore.getInstance(ANDROID_KEYSTORE);
        ks.load(null);

        if (!ks.containsAlias(KEY_ALIAS)) {
            generateRsaKeyPair();
        }
        KeyStore.PrivateKeyEntry entry =
                (KeyStore.PrivateKeyEntry) ks.getEntry(KEY_ALIAS, null);
        return entry.getCertificate().getPublicKey();
    }

    /** Returns the private key (stored inside the Keystore). */
    public static PrivateKey getPrivateKey() throws Exception {
        KeyStore ks = KeyStore.getInstance(ANDROID_KEYSTORE);
        ks.load(null);
        KeyStore.Entry entry = ks.getEntry(KEY_ALIAS, null);
        if (!(entry instanceof KeyStore.PrivateKeyEntry)) {
            throw new IllegalStateException("No private key entry for alias: " + KEY_ALIAS);
        }
        return ((KeyStore.PrivateKeyEntry) entry).getPrivateKey();
    }

    /** Returns the public key currently associated with the alias. */
    public static PublicKey getPublicKey() throws Exception {
        KeyStore ks = KeyStore.getInstance(ANDROID_KEYSTORE);
        ks.load(null);
        KeyStore.Entry entry = ks.getEntry(KEY_ALIAS, null);
        if (!(entry instanceof KeyStore.PrivateKeyEntry)) {
            throw new IllegalStateException("No key entry for alias: " + KEY_ALIAS);
        }
        return ((KeyStore.PrivateKeyEntry) entry).getCertificate().getPublicKey();
    }

    /** Export the public key in PEM format (BEGIN/END PUBLIC KEY). */
    public static String getPublicKeyPem() throws Exception {
        PublicKey pk = getPublicKey();
        byte[] der = pk.getEncoded();
        String b64 = (Build.VERSION.SDK_INT >= 26)
                ? Base64.getMimeEncoder(64, "\n".getBytes()).encodeToString(der)
                : android.util.Base64.encodeToString(der, android.util.Base64.NO_WRAP);
        return "-----BEGIN PUBLIC KEY-----\n" + b64 + "\n-----END PUBLIC KEY-----";
    }

    public static String getPublicKeyBase64() throws Exception {
        PublicKey pk = getPublicKey();
        byte[] der = pk.getEncoded();

        // Use Android's Base64 for compatibility
        return android.util.Base64.encodeToString(der, android.util.Base64.NO_WRAP);
    }


    /** Sign data with SHA256withRSA using the private key. */
    public static byte[] sign(byte[] data) throws Exception {
        PrivateKey priv = getPrivateKey();
        java.security.Signature sig = java.security.Signature.getInstance("SHA256withRSA");
        sig.initSign(priv);
        sig.update(data);
        return sig.sign();
    }

    /** Verify data+signature with the current public key. */
    public static boolean verify(byte[] data, byte[] signature) throws Exception {
        PublicKey pub = getPublicKey();
        java.security.Signature sig = java.security.Signature.getInstance("SHA256withRSA");
        sig.initVerify(pub);
        sig.update(data);
        return sig.verify(signature);
    }

    /** Encrypt short payloads with RSA/ECB/PKCS1Padding (e.g., to send a secret). */
    public static byte[] encryptWithPublic(byte[] plain) throws Exception {
        PublicKey pub = getPublicKey();
        Cipher c = Cipher.getInstance("RSA/ECB/PKCS1Padding");
        c.init(Cipher.ENCRYPT_MODE, pub);
        return c.doFinal(plain);
    }

    /** Decrypt with the private key in the Keystore. */
    public static byte[] decryptWithPrivate(byte[] cipher) throws Exception {
        PrivateKey priv = getPrivateKey();
        Cipher c = Cipher.getInstance("RSA/ECB/PKCS1Padding");
        c.init(Cipher.DECRYPT_MODE, priv);
        return c.doFinal(cipher);
    }

    // --- Internal ---

    private static void generateRsaKeyPair() throws Exception {

        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.M) {
            throw new UnsupportedOperationException("API 23+ required for this example.");
        }

        KeyPairGenerator kpg = KeyPairGenerator.getInstance(
                KeyProperties.KEY_ALGORITHM_RSA, ANDROID_KEYSTORE);

        KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(
                KEY_ALIAS,
                KeyProperties.PURPOSE_SIGN
                        | KeyProperties.PURPOSE_VERIFY
                        | KeyProperties.PURPOSE_ENCRYPT
                        | KeyProperties.PURPOSE_DECRYPT)
                .setKeySize(2048)
                .setDigests(KeyProperties.DIGEST_SHA256, KeyProperties.DIGEST_SHA512)
                .setSignaturePaddings(KeyProperties.SIGNATURE_PADDING_RSA_PKCS1)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_RSA_PKCS1)
                .setUserAuthenticationRequired(false) // set true if you want biometrics/lockscreen
                .build();

        kpg.initialize(spec);
        KeyPair ignored = kpg.generateKeyPair(); // stored inside Keystore
    }
}
