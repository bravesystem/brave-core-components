package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.widget.Toast;

import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;

public class CreatePinPage extends AppCompatActivity {



    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_create_pin_page);


        setTitle("New Enumerator - Set PIN");

        EnumeratorService enumeratorService = ServiceLocator.enumeratorService(this);

        Bundle b = getIntent() != null ? getIntent().getExtras() : null;
        String userId = b != null ? b.getString(IntentKeys.SELECTED_ENUMERATOR) : null;
        String old_pin = b != null ? b.getString(IntentKeys.OLD_PIN) : null;

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
        pinPad.setTitle("Create New PIN");
        pinPad.setUserHint(sb.toString());
        pinPad.setMaxLen(4);
        String enumerator_code = userId.trim();
        pinPad.setListener(new PinPadView.Listener() {
            @Override public void onLengthChanged(int len) {
                if (len < 4) pinPad.clearError();
            }
            @Override public void onCompleted(String pin) {
                //Toast.makeText(CreatePinPage.this, pin,Toast.LENGTH_SHORT).show();

                Intent i = new Intent(CreatePinPage.this, ConfirmPinPage.class);
                i.putExtra(IntentKeys.SELECTED_ENUMERATOR, userId);
                i.putExtra(IntentKeys.CREATED_PIN, pin);
                i.putExtra(IntentKeys.OLD_PIN, old_pin);

                startActivity(i);

                finish();
            }
        });

    }
}