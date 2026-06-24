package intl.iom.bravemobile.interfaces;

public interface EnrollmentCallback {
    void onSuccess();
    void onFailure(Throwable t);
    void onFailureCheckOnline(Throwable t);
}
