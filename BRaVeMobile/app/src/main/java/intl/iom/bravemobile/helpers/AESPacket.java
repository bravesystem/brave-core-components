package intl.iom.bravemobile.helpers;

import javax.crypto.SecretKey;

public class AESPacket {
    public byte[] iv;
    public byte[] encKey;

    //public String ivBase64;
    //public String encKeyBase64;
    public SecretKey aesKey;

}
