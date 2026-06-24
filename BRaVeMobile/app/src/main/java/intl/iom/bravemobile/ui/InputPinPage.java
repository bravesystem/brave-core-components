package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.Toast;

import com.google.android.material.button.MaterialButton;
import com.google.android.material.floatingactionbutton.FloatingActionButton;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.DeviceConfigService;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.ExtendExpiryRequest;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;

public class InputPinPage extends AppCompatActivity {

    private static String TAG = InputPinPage.class.getSimpleName();
    private EnumeratorService enumeratorService;
    private SecureStore secureStore;

    private FloatingActionButton fabChangePin;

    WithLoading withLoading;

    boolean pinExpired = false;

    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_input_pin_page);

        fabChangePin = findViewById(R.id.fabChangePin);

        secureStore = ServiceLocator.secureStore(this);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);

        enumeratorService = ServiceLocator.enumeratorService(this);

        String deviceSignature = secureStore.getHouseholdPrefix();

        setTitle(String.format("Login - (Device %s)",deviceSignature));


        Bundle b = getIntent() != null ? getIntent().getExtras() : null;

        String userId = b != null ? b.getString(IntentKeys.SELECTED_ENUMERATOR) : null;

        String fullName = enumeratorService.getEnumerator(userId).fullName;

        // normalize
        String normalized = (userId == null ? "" : fullName);
        if (!normalized.isEmpty()) normalized = normalized.toUpperCase(Locale.ROOT);

        // build: (User: EN-0007)
        StringBuilder sb = new StringBuilder(24);
        sb.append('(')
                .append("User: ")
                .append(normalized)
                .append(')');


        PinPadView pinPad = findViewById(R.id.pinPad);
        pinPad.setTitle("Enter PIN");
        pinPad.setUserHint(sb.toString());
        pinPad.setMaxLen(4);
        String enumerator_code = userId.trim();

        final String[] inputPin = new String[1];
        pinPad.setListener(new PinPadView.Listener() {
            @Override public void onLengthChanged(int len) {
                if (len < 4) pinPad.clearError();
            }
            @Override public void onCompleted(String pin) {

                PinVerificationResult result =  enumeratorService.verifyInputPin(enumerator_code, pin );
                //boolean ok = validatePinRemote(enumerator_code, pin); // replace with your logic
                if (result.isSuccess()) {
                    try
                    {
                        if (result.enumerator.pinChangeRequired())
                        {
                            Intent i = new Intent(InputPinPage.this, CreatePinPage.class);
                            i.putExtra(IntentKeys.SELECTED_ENUMERATOR, userId);
                            i.putExtra(IntentKeys.OLD_PIN, pin);

                            startActivity(i);
                            finish();
                            return;
                        }
                        enumeratorService.signIn(result);

                        secureStore.login(enumeratorService.getEnumeratorCode());

                        startActivity(new Intent(InputPinPage.this, Dashboard.class));
                        finish();
                    }
                    catch (AuthException e)
                    {
                       Log.e(TAG, e.getMessage());
                    }

                } else {
                    pinPad.reset();
                    pinPad.setError(result.message);

                    //boolean pinExpired = result.status== PinVerificationResult.Status.EXPIRED;
                    pinExpired = result.status== PinVerificationResult.Status.EXPIRED;

                    inputPin[0] = pin;

                    fabChangePin.setVisibility(pinExpired?View.VISIBLE:View.GONE);
                }
            }
        });


        fabChangePin.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                if(pinExpired)
                {
                    withLoading.run("Extending session expiry..", cb -> {

                        enumeratorService.extendExpiry(new ExtendExpiryRequest(userId,  inputPin[0]), new BasicCallback() {
                            @Override
                            public void onSuccess() {

                                PinVerificationResult result = enumeratorService.verifyInputPin(userId, inputPin[0]);

                                try {

                                    if (!result.isSuccess()) {
                                        runOnUiThread(() -> {
                                            pinPad.reset();
                                            pinPad.setError(result.message);
                                        });
                                        return;
                                    }

                                    enumeratorService.signIn(result);
                                    secureStore.login(enumeratorService.getEnumeratorCode());

                                    startActivity(new Intent(InputPinPage.this, Dashboard.class));

                                    runOnUiThread(() ->
                                            Toast.makeText(InputPinPage.this, "Enumerator session extended successfully...", Toast.LENGTH_SHORT).show()
                                    );

                                    cb.onSuccess(null);

                                    finish();

                                } catch (AuthException e) {
                                    throw new RuntimeException(e);
                                }


                            }

                            @Override
                            public void onFailure(Throwable t) {


                                //PinVerificationResult result = PinVerificationResult.invalidPin("Invalid PIN.");
                                runOnUiThread(() -> {
                                    //Toast.makeText(ConfirmPinPage.this,"Unable to connect. Check your internet connection and try again later", Toast.LENGTH_SHORT);

                                    pinPad.reset();
                                    pinPad.setError(t.getMessage());
                                    //pinPad.setError(result.message);
                                });

                                cb.onFailure(t);

                            }
                        });


                    }, new WithLoading.ResultHandler<Void>() {
                        @Override
                        public void onSuccess(Void result) {
                            finish();
                        }

                        @Override
                        public void onFailure(Throwable t) {

                        }
                    });
                }
                else
                {
                    Intent i = new Intent(InputPinPage.this, CreatePinPage.class);
                    i.putExtra(IntentKeys.SELECTED_ENUMERATOR, userId);
                    i.putExtra(IntentKeys.OLD_PIN, inputPin[0]);

                    startActivity(i);
                    finish();
                }



            }
        });




    }



}