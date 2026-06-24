package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.app.Activity;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;

import com.google.android.material.radiobutton.MaterialRadioButton;

import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.services.SecureStore;

public class EnvironmentSelectorPage extends AppCompatActivity
{
    public static final String EXTRA_RESULT_ENV  = "result_env";  // returned to caller
    private MaterialRadioButton rbProd, rbUat, rbDev, rbPtr;
    @Override
    protected void onCreate(Bundle savedInstanceState)
    {

        super.onCreate(savedInstanceState);

        setContentView(R.layout.activity_environment_selector_page);

        rbProd = findViewById(R.id.rbProd);
        rbUat  = findViewById(R.id.rbUat);
        rbDev  = findViewById(R.id.rbDev);
        rbPtr  = findViewById(R.id.rbPartner);

        SecureStore secureStore = new SecureStore(this);

        setTitle("Environment..");

        String current = secureStore.getEnvironment();

        selectRadio(current);

        findViewById(R.id.btnCancel).setOnClickListener(v -> {
            setResult(Activity.RESULT_CANCELED);
            finish();
        });

        findViewById(R.id.btnSave).setOnClickListener(v -> {
            String selected = getSelectedEnv();
            // Return to caller
            Intent data = new Intent();
            data.putExtra(EXTRA_RESULT_ENV, selected);
            setResult(Activity.RESULT_OK, data);

            finish();
        });
    }

    private void selectRadio(String env) {
        if (env == null) env = "prod";
        switch (env.toLowerCase(Locale.ROOT)) {
            case "uat":  rbUat.setChecked(true);  break;
            case "dev":  rbDev.setChecked(true);  break;
            case "ptr":  rbPtr.setChecked(true);  break;
            default:     rbProd.setChecked(true); break;
        }
    }

    private String getSelectedEnv() {
        if (rbUat.isChecked()) return "uat";
        if (rbDev.isChecked()) return "dev";
        if (rbPtr.isChecked()) return "ptr";
        return "prod";
    }
}