package intl.iom.bravemobile.services;

import android.content.Context;
import android.util.Log;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.concurrent.CompletableFuture;

import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.models.PublicKeyResponse;
import intl.iom.bravemobile.models.jwt.ClaimRequest;
import intl.iom.bravemobile.models.jwt.ClaimResponse;
import retrofit2.Call;
import retrofit2.Response;
import retrofit2.Retrofit;

public class PubKeyService {

    private static String TAG = PubKeyService.class.getSimpleName();
    private SecureStore secureStore;
    private Retrofit client;
    public PubKeyService(Context context){

        secureStore = ServiceLocator.secureStore(context);

        RetrofitService retrofitService = null;
        try
        {

            retrofitService = new RetrofitService(context);

            client = retrofitService.getClient();

        } catch (GeneralSecurityException e) {
            throw new RuntimeException(e);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }
    }

    /*public String getPubKey()
    {

        EnrolledApis endpoints = client.create(EnrolledApis.class);

        Call<PublicKeyResponse> call = endpoints.getPubKeyB64();

        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {

            CompletableFuture<String> future = CompletableFuture.supplyAsync(() -> {
                try
                {
                    Response<PublicKeyResponse> response = call.execute();

                    if(response.isSuccessful())
                    {
                        PublicKeyResponse result = response.body();

                        secureStore.setServerPublicKey("", result.publicKeyBase64);

                        return result.publicKeyBase64;

                    }

                }
                catch (IOException e) {
                    Log.e(TAG, e.getMessage());
                }
                return null;
            });

            return future.join(); // blocks until result is ready
        }

        return null;
    }*/

}
