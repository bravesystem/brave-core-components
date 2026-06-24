package intl.iom.bravemobile.api;

import intl.iom.bravemobile.services.SecureStore;

public class TokenRefreshRequest {
    private String deviceId;
    private String enumerator;
    private String tokenRefresh;
    private String nonce;
    private String dPoP;


    // Getters and Setters
    public String getDeviceId() {
        return deviceId;
    }

    public void setDeviceId(String deviceId) {
        this.deviceId = deviceId;
    }

    public String getEnumerator() {
        return enumerator;
    }

    public void setEnumerator(String enumerator) {
        this.enumerator = enumerator;
    }

    public String getTokenRefresh() {
        return tokenRefresh;
    }

    public void setTokenRefresh(String tokenRefresh) {
        this.tokenRefresh = tokenRefresh;
    }

    public String getNonce() {
        return nonce;
    }

    public void setNonce(String nonce) {
        this.nonce = nonce;
    }

    public String getDPoP() {
        return dPoP;
    }

    public void setDPoP(String dPoP) {
        this.dPoP = dPoP;
    }
}