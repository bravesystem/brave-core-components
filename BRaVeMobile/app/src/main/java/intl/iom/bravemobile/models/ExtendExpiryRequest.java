package intl.iom.bravemobile.models;

public class ExtendExpiryRequest {
    public String code;
    public String pin ;

    public ExtendExpiryRequest(String code, String pin) {
        this.code = code;
        this.pin = pin;
    }
}
