package intl.iom.bravemobile.services.mocks;

import android.os.Build;

import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.helpers.ConstantTimeUtils;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.ClockService;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.DeviceConfig;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.ExtendExpiryRequest;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.models.SetPinRequest;
import intl.iom.bravemobile.statics.Env;

public class MockedEnumeratorService implements EnumeratorService {

    String deviceSignature = "";
    String env;

    List<Enumerator> list = new ArrayList<>(5);

    public MockedEnumeratorService(String env, String deviceSignature)
    {
        this.deviceSignature = deviceSignature;
        this.env = env;

    }


    @Override
    public List<Enumerator> listAll() {
        list.clear();

        long now =  clock.nowMs();//System.currentTimeMillis();
        long HOUR = 60L * 60L * 1000L;
        long DAY  = 24L * HOUR;



        if(!Env.PROD.equals( env))
        {
            Enumerator e0 = new Enumerator();
            e0.code = String.format("%s-001",deviceSignature);
            e0.fullName = "Dev User";
            e0.note = "Test Enumerator";
            e0.type = "E";
            e0.pin = "0000";
            e0.lastUpdated = now - 2 * HOUR;
            e0.expiredAt = now + DAY;     // expires in 24h
            e0.isActive = true;
            list.add(e0);
            return list;
        }

        // EN-0001 (active, not expired)
        Enumerator e1 = new Enumerator();
        e1.code = "EN-0001";
        e1.fullName = "Amina Yusuf";
        e1.note = "Senior enumerator";
        e1.type = "E";
        e1.pin = "1234";
        e1.isSupervisor=true;
        e1.lastUpdated = now - 2 * HOUR;
        e1.expiredAt = now + DAY;     // expires in 24h
        e1.isActive = true;
        list.add(e1);

        // EN-0002 (active, expires soon)
        Enumerator e2 = new Enumerator();
        e2.code = "EN-0002";
        e2.fullName = "Jean Pierre";
        e2.note = "French speaker";
        e2.type = "E";
        e2.pin = "1234";
        e2.lastUpdated = now - 6 * HOUR;
        e2.expiredAt = now + 6 * HOUR;
        e2.isActive = true;
        list.add(e2);

        // EN-0003 (inactive)
        Enumerator e3 = new Enumerator();
        e3.code = "EN-0003";
        e3.fullName = "Lindiwe Mokoena";
        e3.note = "On leave";
        e3.type = "E";
        e3.pin = "2468";
        e3.lastUpdated = now - DAY;
        e3.expiredAt = now + DAY;
        e3.isActive = false;
        list.add(e3);

        // EN-0004 (expired 1h ago)
        Enumerator e4 = new Enumerator();
        e4.code = "EN-0004";
        e4.fullName = "Samuel Okoro";
        e4.note = "Night shift";
        e4.type = "E";
        e4.pin = "1357";
        e4.lastUpdated = now - 3 * HOUR;
        e4.expiredAt = now - 1 * HOUR;  // already expired
        e4.isActive = true;
        list.add(e4);

        // EN-0005 (supervisor, long expiry)
        Enumerator e5 = new Enumerator();
        e5.code = "EN-0005";
        e5.fullName = "Maria Santos";
        e5.note = "Supervisor";
        e5.type = "S";
        e5.pin = "0000";
        e5.lastUpdated = now - 30 * HOUR;
        e5.expiredAt = now + 7 * DAY;   // a week
        e5.isActive = true;
        list.add(e5);

        return list;
    }

    @Override
    public void refreshList(BasicCallback callback) {

    }

    @Override
    public Enumerator getEnumerator(String code) {
        return null;
    }

    @Override
    public void saveEnumerators(List<Enumerator> enumerators) {

    }

    @Override
    public PinVerificationResult verifyPin(String code, char[] pin) {
        try {
            long approxServerNowMs = clock.nowMs();

            // Basic validation
            if (code == null || code.trim().isEmpty()) {
                return PinVerificationResult.notFound("Enumerator code is required.");
            }
            if (pin == null || pin.length == 0) {
                return PinVerificationResult.invalidPin("PIN is required.");
            }

            // 1) Find enumerator by code
            Enumerator match = null;
            for (Enumerator e : listAll()) {
                if (code.equals(e.code)) { // case-sensitive; change to equalsIgnoreCase if needed
                    match = e;
                    break;
                }
            }
            if (match == null) {
                return PinVerificationResult.notFound("Enumerator not found.");
            }

            // 2) Check active
            if (!match.isActive) {
                return PinVerificationResult.inactive("Enumerator is inactive.");
            }



            // 4) Compare PIN (dummy data uses cleartext; in production compare salted hash)
            char[] stored = match.pin != null ? match.pin.toCharArray() : new char[0];
            boolean ok = ConstantTimeUtils.constantTimeEquals(pin, stored);

            // Wipe the local copy of the stored pin
            Arrays.fill(stored, '\0');

            if (!ok) {
                return PinVerificationResult.invalidPin("Invalid PIN.");
            }

            // 3) Check expiry (server-anchored time)
            long exp = match.expiredAt; // UTC epoch millis
            if (exp > 0 && approxServerNowMs >= exp) {
                return PinVerificationResult.expired("PIN expired.");
            }

            // 5) Success
            return PinVerificationResult.success(match);
        } finally {
            // Always wipe caller-provided PIN buffer
            if (pin != null) Arrays.fill(pin, '\0');
        }
    }

    @Override
    public PinVerificationResult verifyInputPin(String code, String inputPin) {
        return null;
    }

    @Override
    public void extendExpiry(ExtendExpiryRequest dto, BasicCallback callback) {

    }


    @Override
    public void setNewInputPin(SetPinRequest dto, BasicCallback callback) {

    }


    @Override
    public PinVerificationResult setNewPin(String code, char[] oldPin, char[] newPin, char[] confirmPin) {

        boolean ok = ConstantTimeUtils.constantTimeEquals(newPin, confirmPin);

        if(ok)
        {
            PinVerificationResult result = verifyPin(code, oldPin);

            if(result.isSuccess()) {
                result.enumerator.pin = new String(newPin);
                return result;
            }

            //less likely happening
            return PinVerificationResult.invalidPin("Invalid PIN.");
        }

        return PinVerificationResult.invalidPin("PIN mismatch.");

    }


}
