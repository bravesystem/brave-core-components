package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.text.InputFilter;
import android.view.View;
import android.widget.Toast;

import com.google.android.material.button.MaterialButton;
import com.google.android.material.textfield.TextInputEditText;
import com.google.android.play.core.integrity.IntegrityTokenResponse;

import java.io.IOException;
import java.security.GeneralSecurityException;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.IntegrityHelper;
import intl.iom.bravemobile.interfaces.ActivationService;
import intl.iom.bravemobile.interfaces.DeviceConfigService;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.models.DeviceConfig;
import intl.iom.bravemobile.services.RetrofitService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.Env;
import retrofit2.Retrofit;

public class ActivationPage extends AppCompatActivity {

    SecureStore secureStore;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_activation_page);

        secureStore = ServiceLocator.secureStore(this);

        RetrofitService retrofitService = null;
        Retrofit client;
        try
        {

            retrofitService = new RetrofitService(this);

            client = retrofitService.getClient();

        } catch (GeneralSecurityException e) {
            throw new RuntimeException(e);
        } catch (IOException e) {
            throw new RuntimeException(e);
        }

        setTitle(String.format("Device Activation - %s", secureStore.getEnvironment().toUpperCase()));


        //DeviceConfigService deviceConfigService = ServiceLocator.deviceConfigService();
        ActivationService activationService = ServiceLocator.activationService(this);

        // Initialize views
        com.google.android.material.card.MaterialCardView cardContainer = findViewById(R.id.cardContainer);
        TextInputEditText etClaimCode = findViewById(R.id.etClaimCode);
        MaterialButton btnActivate = findViewById(R.id.btnActivate);
        MaterialButton btnChange = findViewById(R.id.btnChangeEnvironment);


        // Optional fade-in animation
        cardContainer.setAlpha(0f);
        cardContainer.animate().alpha(1f).setDuration(600).start();

        // If device already active → move to Login

        if(activationService.isActive(secureStore.getEnvironment()))
        {
            Intent i =new Intent(this, LoginPage.class);
            startActivity(i);
            finish();
        }


// Apply InputFilter to force uppercase
        etClaimCode.setFilters(new InputFilter[]{
                new InputFilter.AllCaps()
        });

        etClaimCode.setOnFocusChangeListener(new View.OnFocusChangeListener() {
            @Override
            public void onFocusChange(View view, boolean b) {
                etClaimCode.setError(null);
            }
        });

        btnChange.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                //set devide integrity to false and kill
                ServiceLocator.secureStore(ActivationPage.this).setDeviceVerified(false);
                startActivity( new Intent(ActivationPage.this, SplashScreen.class) );
                finish();
            }
        });

        btnActivate.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                String claimcode = etClaimCode.getText().toString();

                if(claimcode.trim().isEmpty())
                {
                    etClaimCode.setError("*");
                    return;
                }


                DeviceActivationResult result = activationService.activate(client,secureStore.getEnvironment(), claimcode);

                if(result.isSuccess())
                {
                    Intent i =new Intent(ActivationPage.this, LoginPage.class);
                    startActivity(i);
                    finish();
                }

                Toast.makeText(ActivationPage.this,result.message,Toast.LENGTH_SHORT).show();

            }
        });
    }
}