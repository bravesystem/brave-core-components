package intl.iom.bravemobile.models;

public class CustomValidationResponse {

    public boolean isValid;
    public String message;

    public CustomValidationResponse(boolean isValid, String message) {
        this.isValid = isValid;
        this.message = message;
    }
}
