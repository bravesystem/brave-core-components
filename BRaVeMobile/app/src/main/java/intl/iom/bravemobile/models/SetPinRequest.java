package intl.iom.bravemobile.models;

public class SetPinRequest {

    public String code;
    public String oldPin ;
    public String newPin ;
    public String confirmedPin ;

    public SetPinRequest(String code, String oldPin, String newPin) {
        this.code = code;
        this.oldPin = oldPin;
        this.newPin = newPin;
    }

    public SetPinRequest(String code, String oldPin, String newPin, String confirmedPin) {
        this.code = code;
        this.oldPin = oldPin;
        this.newPin = newPin;
        this.confirmedPin = confirmedPin;
    }
}
