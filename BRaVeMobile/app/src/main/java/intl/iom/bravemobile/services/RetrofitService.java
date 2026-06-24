package intl.iom.bravemobile.services;

import android.content.Context;

import com.google.gson.Gson;
import com.google.gson.GsonBuilder;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.Date;
import java.util.concurrent.TimeUnit;

import intl.iom.bravemobile.api.RefreshTokenApi;
import intl.iom.bravemobile.api.TokenAuthenticator;
import intl.iom.bravemobile.helpers.DateDeserializer;
import intl.iom.bravemobile.helpers.LenientDateJsonDeserializer;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public final class RetrofitService {
    private  final SecureStore secureStore;

    private static Gson gson = new GsonBuilder()
            .registerTypeAdapter(Date.class, new DateDeserializer())
            .create();

    public RetrofitService(Context context) throws GeneralSecurityException, IOException {
        secureStore = ServiceLocator.secureStore(context);
    }

    public static Retrofit getClient(String url) throws GeneralSecurityException, IOException {

        HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
        logging.setLevel(HttpLoggingInterceptor.Level.BODY); // Options: NONE, BASIC, HEADERS, BODY


        OkHttpClient.Builder httpClient = new OkHttpClient
                .Builder()
                .writeTimeout(90, TimeUnit.SECONDS)   // update write timeout
                .readTimeout(90, TimeUnit.SECONDS)    // optional
                .connectTimeout(30, TimeUnit.SECONDS) // optional
                .addInterceptor(logging);



        return new Retrofit.Builder()
                .baseUrl(url)
                .addConverterFactory(GsonConverterFactory.create(gson))
                .client(httpClient.build())
                .build();

    }


    public Retrofit getClient() throws GeneralSecurityException, IOException {

        RefreshTokenApi refreshTokenApi = getRefreshRetrofit();


        HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
        logging.setLevel(HttpLoggingInterceptor.Level.BODY); // Options: NONE, BASIC, HEADERS, BODY


        OkHttpClient.Builder httpClient = new OkHttpClient
                .Builder()
                .writeTimeout(90, TimeUnit.SECONDS)   // update write timeout
                .readTimeout(90, TimeUnit.SECONDS)    // optional
                .connectTimeout(30, TimeUnit.SECONDS) // optional
                .addInterceptor(logging);

        String bt = secureStore.getAccess();

        String lang = "es";

        httpClient.addInterceptor(chain -> {
                    Request original = chain.request();
                    Request request = original.newBuilder()
                            .header("Authorization", "Bearer " + bt)
                            .header("Accept-Language", lang)
                            .method(original.method(), original.body())
                            .build();
                    return chain.proceed(request);
                })            // adds Authorization: Bearer ...
                .authenticator(new TokenAuthenticator(secureStore, refreshTokenApi)) // uses the separate refresh Retrofit
                .build();


        String currentUrl = secureStore.getCurrentUrl();


        return new Retrofit.Builder()
                .baseUrl(currentUrl)
                .addConverterFactory(GsonConverterFactory.create(gson))
                .client(httpClient.build())
                .build();

    }

    public RefreshTokenApi getRefreshRetrofit()
    {
        OkHttpClient refreshClient = new OkHttpClient.Builder()
                .build();

        String currentUrl = secureStore.getCurrentUrl();


        Retrofit refreshRetrofit = new Retrofit.Builder()
                .baseUrl(currentUrl)
                .client(refreshClient)
                .addConverterFactory(GsonConverterFactory.create(gson))
                .build();
        return  refreshRetrofit.create(RefreshTokenApi.class);
    }
}
