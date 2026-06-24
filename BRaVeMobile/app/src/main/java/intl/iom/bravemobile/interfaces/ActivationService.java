package intl.iom.bravemobile.interfaces;

import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.services.RetrofitService;
import retrofit2.Retrofit;

public interface ActivationService {
    void cleardata();
    boolean hasData();
    DeviceActivationResult activate(Retrofit client, String env, String claim);
    boolean isActive(String env);

}
