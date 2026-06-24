package intl.iom.bravemobile.models;

public class DataPacketHeader {
    public String EncryptedKey ;
    public String Nonce ;
    public String ActivityCode ;
    public String DPoP;
    public String Timestamp ;
    public String Extra = "" ;
    public String BatchId;
}
