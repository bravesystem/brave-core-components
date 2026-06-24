package intl.iom.bravemobile.ui;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import android.Manifest;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Bundle;
import android.provider.Settings;
import android.text.TextUtils;
import android.util.Log;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.AutoCompleteTextView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.button.MaterialButton;
import com.google.android.material.dialog.MaterialAlertDialogBuilder;
import com.google.android.material.textfield.MaterialAutoCompleteTextView;
import com.google.android.material.textfield.TextInputLayout;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.EnvUi;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.ActivationService;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.interfaces.PingService;
import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.services.ConnectivityTest;
import intl.iom.bravemobile.services.PubKeyService;
import intl.iom.bravemobile.services.RetrofitService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import retrofit2.Retrofit;

public class LoginPage extends AppCompatActivity {

    private MaterialAutoCompleteTextView actSearchable;
    private TextInputLayout tilSearchable; // parent layout (to show error nicely)
    private MaterialButton btnContinue;
    private boolean hasSelection = false;

    private EnumeratorService enumeratorService ;
    //private DeviceConfigService cfg = ServiceLocator.deviceConfigService();
    List<String> enumeratorCodes;
    private ArrayAdapter<String> adapter;

    private SecureStore secureStore;

    private ActivationService activationService;

    private ActivityResultLauncher<String[]> requestPermissionsLauncher;

    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.login_settings, menu); // shows icons in Action Bar
        //menu.findItem(R.id.action_select_environment).setVisible(false);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {

        if (item.getItemId() == R.id.action_refresh_enumerators) {
            //Toast.makeText(this, IntentKeys.SELECTED_ENUMERATOR, Toast.LENGTH_SHORT).show();
            //PubKeyService pubKeyService = new PubKeyService(this);
            //ConnectivityTest test = new ConnectivityTest(this);

            //boolean ping = test.basicPing();

            withLoading.run("Loading..",cb -> {

              enumeratorService.refreshList(new BasicCallback() {
                  @Override
                  public void onSuccess() {

                      runOnUiThread(() ->
                              Toast.makeText(LoginPage.this, "Enumerator List updated successfully!", Toast.LENGTH_SHORT).show()
                      );

                      Log.e("Enumerator", "Refresh successful.");
                      cb.onSuccess(null);
                  }

                  @Override
                  public void onFailure(Throwable t) {
                      String error ="Enumerator list Refresh failed: " + t.getMessage();
                      Log.e("Enumerator", "Refresh List failure: " + t.getMessage());

                      runOnUiThread(() ->
                              Toast.makeText(LoginPage.this, error, Toast.LENGTH_SHORT).show()
                      );

                      cb.onFailure(t);
                  }
              });

            });

            return true;
        } else if (item.getItemId() == R.id.action_list_enumerators) {
            Intent i = new Intent(this, EnumeratorListPage.class);
            startActivity(i);
        } else if (item.getItemId() == R.id.action_select_environment) {
            Intent i = new Intent(this, EnvironmentSelectorPage.class);
            activityResultLauncher.launch(i);
            return true;
        }
        return false;

    }

    TextView tvEnv;
    View envIndicator;

    WithLoading withLoading;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login_page);

        tvEnv = findViewById(R.id.tvEnv);

        envIndicator = findViewById(R.id.envIndicator);

        enumeratorService = ServiceLocator.enumeratorService(this);

        activationService = ServiceLocator.activationService(this);

        secureStore = new SecureStore(this);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);


        //PubKeyService pubKeyService = new PubKeyService(this);
        //ConnectivityTest test = new ConnectivityTest(this);

        setTitle(String.format("Login - (Device %s)",secureStore.getHouseholdPrefix()));

        EnvUi.applyEnvironment(secureStore.getEnvironment(), tvEnv, envIndicator, this);

        actSearchable = findViewById(R.id.actSearchable);
        tilSearchable = findViewById(R.id.tilSearchable); // if you used it
        btnContinue = findViewById(R.id.btnContinue);

        enumeratorCodes = toCodes(enumeratorService.listAll());

        adapter = new ArrayAdapter<>(this, android.R.layout.simple_list_item_1, enumeratorCodes);

        actSearchable.setAdapter(adapter);
        actSearchable.setThreshold(0);   // show suggestions immediately

        actSearchable.setValidator(new AutoCompleteTextView.Validator() {
            @Override public boolean isValid(CharSequence text) {
                String t = text == null ? "" : text.toString().trim();
                // case-insensitive contains; tighten to equals if needed
                for (int i = 0; i < adapter.getCount(); i++) {
                    if (adapter.getItem(i).equalsIgnoreCase(t)) return true;
                }
                return false;
            }
            @Override public CharSequence fixText(CharSequence invalidText) {
                // don’t auto-fix; just return as-is
                return invalidText;
            }
        });

        //String key = pubKeyService.getPubKey();
        //boolean ping1 = test.BasicPing();
        //boolean ping2 = test.SecurePing();

        actSearchable.setOnFocusChangeListener((v, hasFocus) -> { if (hasFocus) actSearchable.showDropDown(); });

        // Track when user picks an item from dropdown
        actSearchable.setOnItemClickListener(new AdapterView.OnItemClickListener() {
            @Override
            public void onItemClick(AdapterView<?> parent, View view, int position, long id) {
                hasSelection = true;
                clearFieldError();
            }
        });

        // If user edits text manually, consider it a selection too (optional)
        // actSearchable.addTextChangedListener(...)

        btnContinue.setOnClickListener(v -> {
            String value = actSearchable.getText() != null
                    ? actSearchable.getText().toString().trim()
                    : "";

            if (hasSelection || !TextUtils.isEmpty(value)) {

                boolean ok = actSearchable.getValidator().isValid(actSearchable.getText());

                if(!ok)
                {
                    showFieldError("Pick a value from the list.");
                    return;
                }

                // Go to InputPinPage
                Intent i = new Intent(LoginPage.this, InputPinPage.class);
                // Optionally pass the selection:
                i.putExtra("selected_value", value);
                startActivity(i);
            } else {
                showMissingSelectionAlert();
                showFieldError("Please select an option.");
            }
        });


        requestPermissionsLauncher =
                registerForActivityResult(new ActivityResultContracts.RequestMultiplePermissions(), results -> {
                    boolean cameraGranted = Boolean.TRUE.equals(results.get(Manifest.permission.CAMERA));
                    boolean fineGranted = Boolean.TRUE.equals(results.get(Manifest.permission.ACCESS_FINE_LOCATION));
                    boolean coarseGranted = Boolean.TRUE.equals(results.get(Manifest.permission.ACCESS_COARSE_LOCATION));
                    boolean locationGranted = fineGranted || coarseGranted;

                    if (cameraGranted && locationGranted) {
                        startCameraAndLocationFeatures();
                    } else {
                        handlePermissionDenied();
                    }
                });

        if (isFirstRun()) {
            requestCameraAndLocationIfNeeded();
        }

    }

    // Register once (e.g., field in your Activity)
    private final ActivityResultLauncher<Intent> activityResultLauncher =
            registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {
                if (result.getResultCode() == Activity.RESULT_OK && result.getData() != null) {

                    String env = result.getData().getStringExtra(EnvironmentSelectorPage.EXTRA_RESULT_ENV);

                    if(!secureStore.getEnvironment().equals(env))
                    {

                        if(secureStore.isProvisioned(env)){

                            if(!activationService.hasData())
                            {
                                switchToEnv(env);
                                return;
                            }
                            //Toast.makeText(LoginPage.this, "Sync data first...", Toast.LENGTH_SHORT).show();

                            AlertDialogUtils.showDialog(LoginPage.this,"Alert", "Data from the current environment must be synchronized before switching to a different environment.", R.string.msg_ok);

                        }
                        else
                        {

                            if(!ServiceLocator.activationService(LoginPage.this).hasData()){

                                AlertDialogUtils.inputDialog(LoginPage.this, String.format("Enter the claim code received from the administrator to provision the device in %s.", env.toUpperCase()), "", new AlertDialogUtils.InputCallback() {
                                    @Override
                                    public void onInput(String claim) {

                                        activateAsync(env, claim);
                                        return;

                                    }
                                });


                            }else
                            {

                                AlertDialogUtils.showDialog(LoginPage.this,"Alert", "Data from the current environment must be synchronized before switching to a different environment.", R.string.msg_ok);
                                //Toast.makeText(LoginPage.this, "Sync data first...", Toast.LENGTH_SHORT).show();

                            }



                        }




                    }

                    // Use the selected env (e.g., update UI, re-point base URL, etc.)
                    //secureStore.setEnvironment(env.toLowerCase());

                    //EnvUi.applyEnvironment(env, tvEnv, envIndicator, this);
                    // 1) update the data
                    /*List<String> items = toCodes(enumeratorService.listAll());
                    enumeratorCodes.clear();;
                    enumeratorCodes.addAll(items);*/
                    // 2) tell the adapter to refresh
                    //adapter.notifyDataSetChanged();
                    /*adapter.clear();
                    adapter.addAll(items);
                    adapter.notifyDataSetChanged();*/


                }
            });

    private void switchToEnv(String env) {

        //Toast.makeText(LoginPage.this, "switchToEnv -success ", Toast.LENGTH_SHORT).show();

        activationService.cleardata();

        secureStore.setEnvironment(env);

        restartActivity();


    }

    private void activateAsync(String env, String claim) {

        //Toast.makeText(LoginPage.this, "activateAsync -success ", Toast.LENGTH_SHORT).show();

        String envUrl = secureStore.getEndpoint(env);

        try {


            Retrofit client =  RetrofitService.getClient(envUrl);

            DeviceActivationResult result = activationService.activate(client, env, claim);

            if(result.isSuccess()) {

                activationService.cleardata();
                secureStore.setEnvironment(env);
                AlertDialogUtils.showDialog(LoginPage.this, getString(R.string.device_provisioning_response), result.message, R.string.msg_ok, new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {

                        restartActivity();
                    }
                });



            }
            else
            {
                Toast.makeText(LoginPage.this,result.message,Toast.LENGTH_SHORT).show();

            }



        }
        catch (Exception e)
        {
            Toast.makeText(LoginPage.this, getString(R.string.error_connection_failed) ,Toast.LENGTH_SHORT).show();
        }

    }

    private void restartActivity() {
        /*Intent intent = getIntent();
        finish(); // Close the current activity
        startActivity(intent); // Start the same activity again
         */

        ServiceLocator.reset(false);

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


    private void showMissingSelectionAlert() {
        new MaterialAlertDialogBuilder(this)
                .setTitle("Selection required")
                .setMessage("Please choose an option from the dropdown before continuing.")
                .setPositiveButton("OK", null)
                .show();
    }

    private void showFieldError(String msg) {
        if (tilSearchable != null) {
            tilSearchable.setError(msg);
        }
    }

    private void clearFieldError() {
        if (tilSearchable != null) {
            tilSearchable.setError(null);
        }
    }

    private static List<String> toCodes(List<Enumerator> list) {
        if (list == null || list.isEmpty()) return new ArrayList<>();
        ArrayList<String> codes = new ArrayList<>(list.size());
        for (Enumerator e : list) {
            if (e != null && e.code != null) {
                codes.add(e.code);
            }
        }
        return codes;
    }


    private boolean isFirstRun() {
        SharedPreferences prefs = getSharedPreferences("app_prefs", MODE_PRIVATE);
        return prefs.getBoolean("first_run", true);
    }

    private void markFirstRunComplete() {
        SharedPreferences prefs = getSharedPreferences("app_prefs", MODE_PRIVATE);
        prefs.edit().putBoolean("first_run", false).apply();
    }

    private void requestCameraAndLocationIfNeeded() {
        List<String> toRequest = new ArrayList<>();

        // Use MainActivity.this if you're inside an inner class
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA)
                != PackageManager.PERMISSION_GRANTED) {
            toRequest.add(Manifest.permission.CAMERA);
        }

        boolean fineGranted = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
                == PackageManager.PERMISSION_GRANTED;
        boolean coarseGranted = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
                == PackageManager.PERMISSION_GRANTED;

        if (!fineGranted && !coarseGranted) {
            toRequest.add(Manifest.permission.ACCESS_FINE_LOCATION);
            toRequest.add(Manifest.permission.ACCESS_COARSE_LOCATION);
        }

        if (toRequest.isEmpty()) {
            startCameraAndLocationFeatures();
        } else {
            requestPermissionsLauncher.launch(toRequest.toArray(new String[0]));
        }
    }
    private void handlePermissionDenied() {
        boolean anyPermanentlyDenied =
                (ContextCompat.checkSelfPermission(this, Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED &&
                        !ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.CAMERA))
                        ||
                        (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION) != PackageManager.PERMISSION_GRANTED &&
                                !ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.ACCESS_FINE_LOCATION))
                        ||
                        (ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED &&
                                !ActivityCompat.shouldShowRequestPermissionRationale(this, Manifest.permission.ACCESS_COARSE_LOCATION));

        if (anyPermanentlyDenied) {
            new AlertDialog.Builder(this)
                    .setTitle("Enable permissions")
                    .setMessage("Please enable Camera and Location in App Settings to use these features.")
                    .setPositiveButton("Open Settings", (d, w) -> {
                        Intent intent = new Intent(Settings.ACTION_APPLICATION_DETAILS_SETTINGS,
                                Uri.parse("package:" + getPackageName()));
                        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                        startActivity(intent);
                    })
                    .setNegativeButton("Cancel", null)
                    .show();
        } else {
            Toast.makeText(this, "Permissions denied. Some features may not work.", Toast.LENGTH_LONG).show();
        }
    }

    private void startCameraAndLocationFeatures() {
        // Start camera / get location
    }

}