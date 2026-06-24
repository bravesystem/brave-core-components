package intl.iom.bravemobile.models;

import java.util.List;

public class DeviceActivationResult {

    public final Status status;
    public final String serverPubKey;
    public final String deviceSignature;
    public final int nextId;
    public final String message;

    public boolean isSuccess()
    {
        return status == Status.SUCCESS;
    }


    public DeviceActivationResult(DeviceActivationResult.Status status,String deviceSignature, int nextId, String serverPubKey, String message) {
        this.status = status;
        this.deviceSignature = deviceSignature;
        this.nextId = nextId;
        this.serverPubKey = serverPubKey;
        this.message = message;
    }

    public DeviceActivationResult(DeviceActivationResult.Status status,String message) {
        this.status = status;
        this.deviceSignature = null;
        this.nextId = 0;
        this.serverPubKey = null;
        this.message = message;
    }

    public enum Status {
        SUCCESS,
        INVALID_CLAIM_CODE,
        EXPIRED_CLAIM_CODE,
        DEVICE_ALREADY_ACTIVATED,
        DEVICE_BLOCKED,
        UNAUTHORIZED,
        NETWORK_ERROR,
        SERVER_UNAVAILABLE,
        SECURITY_VIOLATION,
        INVALID_DEVICE_SIGNATURE,
        TENANT_NOT_FOUND,
        ENVIRONMENT_MISMATCH,
        RATE_LIMIT_EXCEEDED,
        CAPACITY_EXCEEDED,
        HOUSEHOLD_BINDING_POOL_EXHAUSTED,
        UNKNOWN_ERROR }
}
