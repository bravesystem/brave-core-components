package intl.iom.bravemobile.models;

public class LoginState {
    private boolean isSupervisor;
    private boolean isAuthenticated;
    private String enumeratorCode;

    public String getEnumeratorCode(){
        return enumeratorCode;
    }

    public boolean isAuthenticated() {
        return isAuthenticated;
    }

    public boolean isSupervisor(){
        return isSupervisor;
    }

    public void signOut()
    {
        isSupervisor = isAuthenticated = false;
    }


    public void setAuthenticated(PinVerificationResult result) {

        if(result.isSuccess())
        {
            isAuthenticated = true;
            isSupervisor = result.enumerator.isSupervisor;
            enumeratorCode = result.enumerator.code;
        }
    }

}
