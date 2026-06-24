package intl.iom.bravemobile.models;

public class PinVerificationResult {

    public final Status status;
    public final Enumerator enumerator;     // present when SUCCESS
    public final String message;            // optional, user/dev message


    public PinVerificationResult(Status status, Enumerator enumerator, String message) {
        this.status = status;
        this.enumerator = enumerator;
        this.message = message;
    }

    public boolean isSuccess()
    {
        return status == Status.SUCCESS;
    }


    public static PinVerificationResult success(Enumerator e) {
        return new PinVerificationResult(Status.SUCCESS, e, null);
    }
    public static PinVerificationResult notFound(String msg) {
        return new PinVerificationResult(Status.NOT_FOUND, null, msg);
    }
    public static PinVerificationResult expired(String msg) {
        return new PinVerificationResult(Status.EXPIRED, null, msg);
    }
    public static PinVerificationResult inactive(String msg) {
        return new PinVerificationResult(Status.INACTIVE, null, msg);
    }
    public static PinVerificationResult invalidPin(String msg) {
        return new PinVerificationResult(Status.INVALID_PIN, null, msg);
    }

    public enum Status { SUCCESS, NOT_FOUND, INACTIVE, EXPIRED, INVALID_PIN }
}
