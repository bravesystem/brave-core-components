package intl.iom.bravemobile.interfaces;

public interface RefreshCallback {
    void onSuccess(String newToken);
    void onFailure(String error);
}