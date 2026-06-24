package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Toast;

import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.models.SetPinRequest;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;

public class ConfirmPinPage extends AppCompatActivity {

    private SecureStore secureStore;

    WithLoading withLoading;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_confirm_pin_page);

        setTitle("New Enumerator - Set PIN");

        Bundle b = getIntent() != null ? getIntent().getExtras() : null;
        String userId = b != null ? b.getString(IntentKeys.SELECTED_ENUMERATOR) : null;
        String newPin = b != null ? b.getString(IntentKeys.CREATED_PIN) : null;
        String oldPin = b != null ? b.getString(IntentKeys.OLD_PIN) : null;

        EnumeratorService enumeratorService = ServiceLocator.enumeratorService(this);

        secureStore = new SecureStore(this);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);


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
        pinPad.setTitle("Confirm New PIN");
        pinPad.setUserHint(sb.toString());
        pinPad.setMaxLen(4);
        String enumerator_code = userId.trim();
        pinPad.setListener(new PinPadView.Listener() {
            @Override public void onLengthChanged(int len) {
                if (len < 4) pinPad.clearError();
            }
            @Override public void onCompleted(String pin) {

                boolean ok = !StringUtils.isBlank(pin) && pin.equals(newPin);

                if(!ok)
                {
                    PinVerificationResult result = PinVerificationResult.invalidPin("Invalid PIN.");
                    pinPad.reset();
                    pinPad.setError(result.message);
                    return;
                }

                SetPinRequest dto = new SetPinRequest(userId, oldPin, newPin,  pin);

                withLoading.run("Loading..", cb -> {

                    enumeratorService.setNewInputPin(dto, new BasicCallback() {
                        @Override
                        public void onSuccess() {

                            PinVerificationResult result = enumeratorService.verifyInputPin(userId, newPin);

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
                                startActivity(new Intent(ConfirmPinPage.this, Dashboard.class));

                                runOnUiThread(() ->
                                        Toast.makeText(ConfirmPinPage.this, "Pin changed successfully...", Toast.LENGTH_SHORT).show()
                                );

                                cb.onSuccess(null);

                                //finish();

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


                //Toast.makeText(ConfirmPinPage.this, "Pin is not matching...", Toast.LENGTH_SHORT).show();


            }
        });



    }
}