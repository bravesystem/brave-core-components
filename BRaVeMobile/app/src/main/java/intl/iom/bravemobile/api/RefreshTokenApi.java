package intl.iom.bravemobile.api;

import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.POST;

public interface RefreshTokenApi {
    @POST("api/v1/token/refresh")
    Call<TokenRefreshResponse> refresh(@Body TokenRefreshRequest req);
}
