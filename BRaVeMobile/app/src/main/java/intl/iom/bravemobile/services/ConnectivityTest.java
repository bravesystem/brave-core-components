package intl.iom.bravemobile.services;

import android.content.Context;
import android.util.Log;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.concurrent.CompletableFuture;
import java.util.function.Consumer;

import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.interfaces.PingService;
import intl.iom.bravemobile.models.PublicKeyResponse;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;

public class ConnectivityTest implements PingService {

    private static String TAG = PubKeyService.class.getSimpleName();
    private SecureStore secureStore;
    private Retrofit client;
    public ConnectivityTest(Context context)
    {
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

    /*@Override
    public boolean basicPing() {


        EnrolledApis endpoints = client.create(EnrolledApis.class);

        Call<Boolean> call = endpoints.basicPing();

        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {

            CompletableFuture<Boolean> future = CompletableFuture.supplyAsync(() -> {
                try
                {
                    Response<Boolean> response = call.execute();

                    if(response.isSuccessful())
                    {
                        Boolean result = response.body();

                        return result;
                    }



                }
                catch (IOException e) {
                    Log.e(TAG, e.getMessage());
                }
                return false;
            });

            return future.join(); // blocks until result is ready
        }


        return false;
    }
*/

    @Override
    public void basicPing(PingCallback  callback) {
        EnrolledApis endpoints = client.create(EnrolledApis.class);
        Call<Boolean> call = endpoints.basicPing();
        call.enqueue(new Callback<Boolean>() {
            @Override
            public void onResponse(Call<Boolean> call, Response<Boolean> response) {
                if (response.isSuccessful() && Boolean.TRUE.equals(response.body())) {
                    callback.onSuccess();
                } else {
                    callback.onFailure(new Exception("HTTP " + response.code()));
                }
            }

            @Override
            public void onFailure(Call<Boolean> call, Throwable t) {
                Log.e(TAG, t.getMessage());
                callback.onFailure(t);
            }
        });
    }


    @Override
    public void securePing(PingCallback  callback) {
        EnrolledApis endpoints = client.create(EnrolledApis.class);
        Call<Boolean> call = endpoints.securePing();
        call.enqueue(new Callback<Boolean>() {
            @Override
            public void onResponse(Call<Boolean> call, Response<Boolean> response) {
                if (response.isSuccessful() && Boolean.TRUE.equals(response.body())) {
                    callback.onSuccess();
                } else {
                    callback.onFailure(new Exception("HTTP " + response.code()));
                }
            }

            @Override
            public void onFailure(Call<Boolean> call, Throwable t) {
                Log.e(TAG, t.getMessage());
                callback.onFailure(t);
            }
        });
    }

}
