package intl.iom.bravemobile.helpers;

import android.util.Base64;

import java.security.KeyFactory;
import java.security.PublicKey;
import java.security.spec.X509EncodedKeySpec;

public final class RsaKeyLoader {

    public static PublicKey loadRsaPublicKeyFromBase64(String base64Key) throws Exception {
        try
        {
            // 1. Decode base64 to bytes
            byte[] derBytes = Base64.decode(base64Key, Base64.DEFAULT);

            // 2. Wrap in X.509 (SubjectPublicKeyInfo) spec
            X509EncodedKeySpec keySpec = new X509EncodedKeySpec(derBytes);

            // 3. Build PublicKey for RSA
            KeyFactory kf = KeyFactory.getInstance("RSA");
            return kf.generatePublic(keySpec);
        }
        catch (Exception e)
        {
            throw e;
        }
    }
}
