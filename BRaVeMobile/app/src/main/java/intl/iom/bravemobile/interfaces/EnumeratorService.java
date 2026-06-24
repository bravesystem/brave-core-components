package intl.iom.bravemobile.interfaces;

import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.ExtendExpiryRequest;
import intl.iom.bravemobile.models.LoginState;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.models.SetPinRequest;
import intl.iom.bravemobile.services.ServiceLocator;

public interface EnumeratorService {

    //DeviceConfigService cfg = ServiceLocator.deviceConfigService();
    ClockService clock = ServiceLocator.clockService();
    LoginState loginState = new LoginState();

    default String getEnumeratorCode()
    {
        return loginState.getEnumeratorCode();
    }

    default void signIn(PinVerificationResult result) throws AuthException
    {
        if(!result.isSuccess()) throw new AuthException(result.message);

        if(!loginState.isAuthenticated())
            loginState.setAuthenticated(result);

    }

    /** Sign out and clear local session. */
    default void signOut(){
        if(loginState.isAuthenticated())
            loginState.signOut();
    }

    /** Quick status check. */
    default boolean isAuthenticated()
    {
        return loginState.isAuthenticated();
    }

    /** Check if supervisor */
    default boolean isSupervisor(){
        return loginState.isSupervisor();
    }

    /** Return all enumerators (you can add paging later). */
    List<Enumerator> listAll();

    void refreshList(BasicCallback callback);
    Enumerator getEnumerator(String code);

    void saveEnumerators(List<Enumerator> enumerators);

    /**
     * Verify the PIN for the given enumerator code.
     *
     * @param code                 enumerator unique code (e.g., "EN-0007")
     * @param pin                  user-entered PIN (use char[] so you can zero it after use)
     */
    PinVerificationResult verifyPin(String code, char[] pin);
    PinVerificationResult verifyInputPin(String code, String inputPin);
    void extendExpiry(ExtendExpiryRequest dto, BasicCallback callback);
    void setNewInputPin(SetPinRequest dto, BasicCallback callback);
    PinVerificationResult setNewPin(String code, char[] oldPin, char[] newPin, char[] confirmedPin);


    /**
     * Check if an enumerator’s PIN is expired at the given (server-anchored) time.
     */
    default boolean isPinExpired(Enumerator e, long approxServerNowMs) {
        // If expiredAt == 0 means "no expiry" (adjust to your rules)
        if (e == null) return true;
        long exp = e.expiredAt;
        return exp > 0 && approxServerNowMs >= exp;
    }

}
