package intl.iom.bravemobile.ui;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;

import android.util.Log;
import android.view.View;
import android.view.animation.AlphaAnimation;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;


import com.google.android.play.core.integrity.IntegrityTokenResponse;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.EnvUi;
import intl.iom.bravemobile.helpers.IntegrityHelper;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.AppConfigProvider;
import intl.iom.bravemobile.interfaces.CustomCallback;
import intl.iom.bravemobile.interfaces.PingService;
import intl.iom.bravemobile.models.Environments;
import intl.iom.bravemobile.models.jwt.IntegrityTokenRequestDto;
import intl.iom.bravemobile.models.jwt.NonceResponse;
import intl.iom.bravemobile.models.jwt.VerifyResponse;
import intl.iom.bravemobile.services.ConnectivityTest;
import intl.iom.bravemobile.services.LaunchInitializerService;
import intl.iom.bravemobile.services.RetrofitService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.PreferenceKeys;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;

import android.util.Base64;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;

public class SplashScreen extends AppCompatActivity {

    private static final String TAG = LaunchInitializerService.class.getSimpleName();
    private static final ExecutorService IO = Executors.newSingleThreadExecutor();
    private static final int SPLASH_DURATION = 2000; // 2 seconds
    private IntegrityHelper integrityHelper;

    private SecureStore secureStore;

    Button btnPing;
    Button btnReboot;

    TextView tvWaiting;

    WithLoading withLoading;

    private final ActivityResultLauncher<Intent> activityResultLauncher =  registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {
        if (result.getResultCode() == Activity.RESULT_OK && result.getData() != null) {
            String env = result.getData().getStringExtra(EnvironmentSelectorPage.EXTRA_RESULT_ENV);

            // Use the selected env (e.g., update UI, re-point base URL, etc.)
            secureStore.setEnvironment(env.toLowerCase());


            GetConfigurationParameters();

            ServiceLocator.activationService(SplashScreen.this)
                    .cleardata();


            LaunchInitializerService.run(this);

            withLoading.run("Backend handshaking...",cb -> {

                initializeBackend(new CustomCallback() {
                    @Override
                    public void onSuccess() {

                    }

                    @Override
                    public void onFailure(Throwable t) {
                        Toast(t.getMessage());
                        cb.onFailure(t);
                    }
                });

            });

        }
    });

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_splash_screen);

        secureStore = ServiceLocator.secureStore(this);

        integrityHelper = new IntegrityHelper(this);

        ImageView logo = findViewById(R.id.splash_logo);
        TextView tagline = findViewById(R.id.splash_tagline);

        tvWaiting = findViewById(R.id.tvWaiting);
        btnPing = findViewById(R.id.btnPing);
        btnReboot = findViewById(R.id.btnReboot);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);

        // Fade-in animation
        AlphaAnimation fadeIn = new AlphaAnimation(0f, 1f);
        fadeIn.setDuration(1000); // 1 second fade
        fadeIn.setFillAfter(true);

        logo.startAnimation(fadeIn);
        tagline.startAnimation(fadeIn);

        //secureStore.setDeviceVerified(false);
        //String curUrl= secureStore.getCurrentUrl();

        //secureStore.setDeviceVerified(false);

        /*secureStore.setDevUrlTesting(
                Environments.dev
                //"https://cd5k74hq-7092.euw.devtunnels.ms/"
        );

        String devurl = secureStore.getEndpoint("dev");*/
        //String uaturl = secureStore.getEndpoint("uat");

        if(!secureStore.isDeviceVerified())
        {
            Intent i = new Intent(this, EnvironmentSelectorPage.class);
            activityResultLauncher.launch(i);

        }
        else
        {
            startActivity(new Intent(SplashScreen.this, ActivationPage.class));
            finish();
        }

        btnPing.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                withLoading.run("Connecting..",cb -> {

                    new ConnectivityTest(SplashScreen.this).basicPing(new PingService.PingCallback() {
                        @Override
                        public void onSuccess() {
                            Toast.makeText(SplashScreen.this, "Connectivity test succeeded.", Toast.LENGTH_SHORT).show();
                            cb.onSuccess(null);
                        }

                        @Override
                        public void onFailure(Throwable t) {
                            String error ="Connectivity test Failed: " + t.getMessage();
                            Log.e("Ping", "Failed: " + t.getMessage());
                            Toast.makeText(SplashScreen.this, error, Toast.LENGTH_SHORT).show();
                            cb.onFailure(t);
                        }
                    });

                });



            }
        });

        btnReboot.setOnClickListener(v -> restartApp());

    }

    //private String url = "https://iom-d-we-webapp-brave-mob-002.azurewebsites.net/";
    //private String url = "https://sm3pjfm3-7092.uks1.devtunnels.ms/";

    private void GetConfigurationParameters() {

        AppConfigProvider appConfigProvider = ServiceLocator.appConfigProvider(SplashScreen.this);

        String prodUrl = appConfigProvider.getString(PreferenceKeys.PROD, Environments.prod);

        secureStore.setDeviceId(
            appConfigProvider.getString("brave_esid", null)
        );

        secureStore.setProdUrl(prodUrl);

        secureStore.setUatUrl(
                appConfigProvider.getString(PreferenceKeys.UAT, Environments.uat)
        );

        secureStore.setDevUrl(
                appConfigProvider.getString(PreferenceKeys.DEV, Environments.dev)
        );

        secureStore.setPartnerUrl(
                appConfigProvider.getString(PreferenceKeys.PTR, Environments.ptr)
        );

    }

    private void Toast(String message)
    {
        new Handler(Looper.getMainLooper()).post(()->{
            Toast.makeText(SplashScreen.this, message, Toast.LENGTH_SHORT ).show();
        });
    }


    private void initializeBackend(CustomCallback cb)
    {

        IO.execute(() -> {
            try {

                RetrofitService retrofitService = new RetrofitService(SplashScreen.this);

                Retrofit client = retrofitService.getClient();

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                Call<NonceResponse> step1 = endpoints.getNonce(
                        secureStore.getDeviceId()
                );

                Response<NonceResponse> response1 = step1.execute();

                if(response1.isSuccessful())
                {
                    NonceResponse data = response1.body();

                    integrityHelper.requestIntegrityToken(data.nonce, integrityHelper.Project_Number ,new IntegrityHelper.RequestCallback() {
                        @Override
                        public void sendTokenToBackend(IntegrityTokenResponse integrityTokenResponse) {

                            String token = //integrityTokenResponse.token();
                                    data.nonce;// mock

                            Call<VerifyResponse> step2 = endpoints.verifyToken(new IntegrityTokenRequestDto(data.id,token));

                            try
                            {
                                step2.enqueue(new Callback<VerifyResponse>() {
                                    @Override
                                    public void onResponse(Call<VerifyResponse> call, Response<VerifyResponse> response) {


                                        if (response.isSuccessful()) {
                                            // Handle success

                                            VerifyResponse result = response.body();

                                            boolean is_verified = result.success ;

                                            secureStore.setDeviceVerified(is_verified);

                                            if(is_verified)
                                            {
                                                Log.d("Integrity", "Verification successful: " + response.code());

                                                startActivity(new Intent(SplashScreen.this, ActivationPage.class));

                                                finish();
                                            }
                                            else
                                            {
                                                Log.e("Integrity", "Verification failed: " + response.code());

                                                new Handler(Looper.getMainLooper()).post(()->{
                                                    tvWaiting.setVisibility(View.GONE);
                                                    btnPing.setVisibility(View.VISIBLE);
                                                    btnReboot.setVisibility(View.VISIBLE);
                                                });

                                                cb.onFailure(new Exception("Verification failed: " + response.code()));
                                            }

                                        }
                                        else
                                        {
                                            Log.e("Retrofit", "Request failed: " + response.message());
                                            //Toast.makeText(SplashScreen.this, "Something went wrong: " + response.message(), Toast.LENGTH_LONG).show();
                                            //btnReboot.setVisibility(View.VISIBLE);

                                            new Handler(Looper.getMainLooper()).post(()->{
                                                tvWaiting.setVisibility(View.GONE);
                                                btnReboot.setVisibility(View.VISIBLE);
                                            });

                                            cb.onFailure(new Exception("Something went wrong: " + response.message()));
                                        }

                                    }

                                    @Override
                                    public void onFailure(Call<VerifyResponse> call, Throwable t) {
                                        // Handle failure
                                        Log.e("Retrofit", "Error during Device integrity verification, network error: " + t.getMessage());
                                        //Toast("Error during Device integrity verification, network error");

                                        new Handler(Looper.getMainLooper()).post(()->{
                                            tvWaiting.setVisibility(View.GONE);
                                            btnPing.setVisibility(View.VISIBLE);
                                            btnReboot.setVisibility(View.VISIBLE);
                                        });

                                        cb.onFailure(new Exception("Error during Device integrity verification, network error: " + t.getMessage()));


                                    }
                                });


                            } catch (Exception e) {
                                throw new RuntimeException(e);
                            }

                        }
                    });

                }
                else
                {
                    Log.e("Nonce", "Nonce not received: " + response1.code());

                    //Toast("Nonce not received. Possible network issue");

                    new Handler(Looper.getMainLooper()).post(()->{
                        tvWaiting.setVisibility(View.GONE);
                        btnPing.setVisibility(View.VISIBLE);
                        btnReboot.setVisibility(View.VISIBLE);
                    });

                    cb.onFailure(new Exception("Nonce not received. Possible network issue"));
                }

            }
            catch (Exception e)
            {
                Log.e(TAG, "Key init failed", e);

                //Toast(e.getMessage());

                new Handler(Looper.getMainLooper()).post(()->{
                    tvWaiting.setVisibility(View.GONE);
                    btnPing.setVisibility(View.VISIBLE);
                    btnReboot.setVisibility(View.VISIBLE);
                });

                cb.onFailure(e);

            }
        });



    }


    private void restartApp() {
        // Get the app's launch intent (the one with CATEGORY_LAUNCHER)
        Intent launchIntent = getPackageManager()
                .getLaunchIntentForPackage(getPackageName());

        if (launchIntent != null) {
            // Clear the current task and start a new one
            launchIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK |
                    Intent.FLAG_ACTIVITY_CLEAR_TASK);
            startActivity(launchIntent);

            // Optionally remove animation for a snappier feel
            overridePendingTransition(0, 0);
        }

        // Finish this activity (and all parents)
        finishAffinity();
    }

}