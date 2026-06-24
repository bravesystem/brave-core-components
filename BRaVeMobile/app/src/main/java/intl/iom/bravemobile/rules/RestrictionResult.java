package intl.iom.bravemobile.rules;

public class RestrictionResult {

    public boolean isValid;
    public String errorMessage;

    public RestrictionResult(boolean isValid, String errorMessage) {
        this.isValid = isValid;
        this.errorMessage = errorMessage;
    }
}
