package intl.iom.bravemobile.ui.verifications;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.RecyclerView;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.VerificationAdapter;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.CustomCallback;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.models.registrations.VerificationResponse;
import intl.iom.bravemobile.services.LaunchInitializerService;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.EnvironmentSelectorPage;
import intl.iom.bravemobile.ui.LoginPage;
import intl.iom.bravemobile.ui.SplashScreen;
import intl.iom.bravemobile.ui.registrations.RegistrationListPage;

public class VerificationListPage extends AppCompatActivity {

    @Override
    public void onBackPressed() {
        return;
    }

    VerificationService verificationService;
    RegistrationActivityService registrationActivityService;

    VerificationAdapter adapter;
    TextView emptyView;
    RecyclerView rv;

    WithLoading withLoading;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_verification_list_page);

        setTitle("Biometric Record Check");

        verificationService = ServiceLocator.verificationService(this);

        registrationActivityService = ServiceLocator.registrationActivityService(this);

        String activityCode = registrationActivityService.getCurrent();

        rv = findViewById(R.id.rvVerifications);
        emptyView = findViewById(R.id.emptyView);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);

        List<Verification> verifications = verificationService.getAll(
                registrationActivityService.getCurrent()
        );

        adapter = new VerificationAdapter(this, verifications, new VerificationAdapter.OnItemActionListener() {
            @Override
            public void getMatch(String uuid) {

                Intent intent = new Intent(VerificationListPage.this, VerificationDetailsPage.class);
                intent.putExtra(IntentKeys.HOUSEHOLD_ID,uuid);
                startActivity(intent);

            }

            @Override
            public void findMatch(String uuid) {

                //get verification record
                BiometricCheckModel verification = verificationService.getVerification(uuid);

                List<BiometricCheckModel> payload = new ArrayList<>();
                payload.add(verification);

                withLoading.run("Submitting request to the server…",cb -> {

                    verificationService.verifyTemplates( activityCode, "", payload, new BasicCallback() {
                        @Override
                        public void onSuccess() {

                            runOnUiThread(() ->{

                                Toast.makeText(VerificationListPage.this, "Request sent to the server. Searching for a biometric match", Toast.LENGTH_SHORT).show();

                                List<Verification> verifications = verificationService.getAll(
                                        registrationActivityService.getCurrent()
                                );

                                adapter.setData(verifications);

                            });

                            Log.e("Biometric Verification", "Request sent to server.");
                            cb.onSuccess(null);
                        }

                        @Override
                        public void onFailure(Throwable t) {

                            Log.e("Biometric Verification", "Request failed: " + t.getMessage());

                            runOnUiThread(() ->
                                    Toast.makeText(VerificationListPage.this, t.getMessage(), Toast.LENGTH_SHORT).show()
                            );

                            cb.onFailure(t);
                        }
                    });

                });

            }

            @Override
            public void deleteVerification(String uuid) {

            }

            @Override
            public void getEnrollment(Verification verification) {

            }
        });
        rv.setAdapter(adapter);

        // Call once after initial data load too:
        emptyView.setVisibility(verifications.size() == 0 ? View.VISIBLE : View.GONE);
        rv.setVisibility(verifications.size() == 0 ? View.GONE : View.VISIBLE);

        findViewById(R.id.fabAddVerification).setOnClickListener(v -> {

            try {

                if(!registrationActivityService.isActive(activityCode)){
                    AlertDialogUtils.showDialog(VerificationListPage.this, getString(R.string.activity_closed_lbl), getString(R.string.activity_closed_text), R.string.msg_ok);
                    return;
                }

            } catch (RegistrationActivityNotFound e) {
                throw new RuntimeException(e); //not happening
            }


            Intent intent = new Intent(this, VerificationCreatePage.class);
            activityResultLauncher.launch(intent);

        });


    }

    private final ActivityResultLauncher<Intent> activityResultLauncher=  registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

        if (result.getResultCode() == Activity.RESULT_OK && result.getData() != null)
        {
            boolean refresh = result.getData().getBooleanExtra(IntentKeys.REFRESH, false);

            if(refresh)
            {
                List<Verification> verifications = verificationService.getAll(
                        registrationActivityService.getCurrent()
                );

                adapter.setData(verifications);

                // Call once after initial data load too:
                emptyView.setVisibility(verifications.size() == 0 ? View.VISIBLE : View.GONE);
                rv.setVisibility(verifications.size() == 0 ? View.GONE : View.VISIBLE);
            }



        }
    });


    /*private List<Verification> buildSampleData() {
        List<Verification> list = new ArrayList<>();

        // Sample items
        list.add(new Verification("uuid-001", 1, 1704067200L, false, false, null, "Newly saved verification"));
        list.add(new Verification("uuid-002", 2, 1704153600000L, true, true, "family-789", "Matched with a known family"));
        list.add(new Verification("uuid-003", 1, 1704240000L, true, false, null, ""));
        list.add(new Verification("uuid-004", 2, 1704326400L, false, false, null, "Will be processed soon"));

        return list;
    }*/

}