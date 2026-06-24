package intl.iom.bravemobile.services.mocks;

import java.util.Arrays;
import java.util.List;

import intl.iom.bravemobile.interfaces.ActivationService;
import intl.iom.bravemobile.models.DeviceActivationResult;
import retrofit2.Retrofit;

public class MockedActivationService implements ActivationService {

    @Override
    public void cleardata() {

    }

    @Override
    public boolean hasData() {
        return false;
    }

    @Override
    public DeviceActivationResult activate(Retrofit client, String env, String claim) {

        if (claim == null || claim.trim().isEmpty())
            return null;

        if(claim.trim().equalsIgnoreCase("000"))
            return SAMPLE_RESULTS.get(0);


        if(claim.trim().equalsIgnoreCase("D4010L"))
            return SAMPLE_RESULTS.get(2);


        return SAMPLE_RESULTS.get(1);
    }

    @Override
    public boolean isActive(String env) {
        return false;
    }

    // ---- Static test data ----
    public static final List<DeviceActivationResult> SAMPLE_RESULTS = Arrays.asList(
            new DeviceActivationResult(
                    DeviceActivationResult.Status.SUCCESS,
                    "S65",
                    17,
                    "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A...", // sample public key
                    "Device successfully activated."
            ),
            new DeviceActivationResult(
                    DeviceActivationResult.Status.INVALID_CLAIM_CODE,
                    "The claim code you entered is invalid."
            ),
            new DeviceActivationResult(
                    DeviceActivationResult.Status.DEVICE_ALREADY_ACTIVATED,
                    "This device is already activated."
            )
    );
}
