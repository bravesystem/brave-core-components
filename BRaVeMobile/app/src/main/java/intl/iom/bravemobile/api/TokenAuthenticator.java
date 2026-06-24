package intl.iom.bravemobile.api;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.io.IOException;

import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.helpers.NonceUtil;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.SqlJwtService;
import okhttp3.HttpUrl;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.Route;

public final class TokenAuthenticator implements okhttp3.Authenticator{

    private final SecureStore secureStore;
    private final Object lock = new Object();
    private final RefreshTokenApi authApi; // a Retrofit service for /api/token/refresh


    public TokenAuthenticator(SecureStore tokens, RefreshTokenApi authApi) {
        this.secureStore = tokens; this.authApi = authApi;
    }

    private static int responseCount(Response response) {
        int count = 1;
        while ((response = response.priorResponse()) != null) {
            count++;
        }
        return count;
    }

    private static boolean isRefreshEndpoint(HttpUrl url) {
        // Adjust to your path; e.g., /api/v1/token/refresh
        return url.encodedPath().equalsIgnoreCase("/api/v1/token/refresh");
    }

    @Override
    public Request authenticate(@Nullable Route route, @NonNull Response response) throws IOException {


// 1) No auth header originally → don’t add one
        final String priorAuth = response.request().header("Authorization");
        if (priorAuth == null) return null;

        // 2) Limit follow-up attempts for this chain
        if (responseCount(response) >= 2) { // 0-original, 1-first retry
            // Avoid infinite loop / "too many follow-up requests"
            return null;
        }

        // 3) Don’t run for refresh endpoint itself
        if (isRefreshEndpoint(response.request().url())) {
            return null;
        }


        synchronized (lock) {
            // If another thread already refreshed, reuse it
            String newAccess = secureStore.getAccess();
            if (newAccess != null && !SqlJwtService.isJwtExpired(newAccess, 20)) {
                return response.request().newBuilder()
                        .header("Authorization", "Bearer " + newAccess)
                        .build();
            }

            String refresh = secureStore.getRefresh();
            if (refresh == null) return null;

            try {
                // Call refresh endpoint synchronously
                TokenRefreshRequest req = new TokenRefreshRequest();

                req.setDeviceId( secureStore.getDeviceId() ); //1

                req.setTokenRefresh( refresh ); //2

                req.setEnumerator(secureStore.getEnumerator()); //3

                String nonce = NonceUtil.generate();
                req.setNonce(nonce); //4
                byte[] b = NonceUtil.decode(nonce);
                byte[] bytes = KeyStoreHelper.sign( b );
                req.setDPoP( NonceUtil.b64u( bytes ) ); //5

                retrofit2.Response<TokenRefreshResponse> r = authApi.refresh(req).execute();

                if (!r.isSuccessful() || r.body() == null) {
                    secureStore.invalidateJws(); // refresh invalid → logout
                    return null;
                }

                String refresh1 = r.body().getRefresh();

                if(StringUtils.isBlank(refresh1))
                {
                    throw new IOException();
                }

                String env = secureStore.getEnvironment();

                secureStore.saveTokens(env, r.body().getJwt(), refresh1);

                // Retry original with new access
                return response.request().newBuilder()
                        .header("Authorization", "Bearer " + r.body().getJwt())
                        .build();
            } catch (Exception e) {
                secureStore.invalidateJws();
                return null;
            }
        }
    }
}
