package intl.iom.bravemobile.models.jwt;

public class ClaimRequest {
    public String sessionCode;
    public String deviceId;
    public String deviceKeyThumbprint;

    public ClaimRequest(String sessionCode, String deviceId, String deviceKeyThumbprint) {
        this.sessionCode = sessionCode;
        this.deviceId = deviceId;
        this.deviceKeyThumbprint = deviceKeyThumbprint;
    }
}