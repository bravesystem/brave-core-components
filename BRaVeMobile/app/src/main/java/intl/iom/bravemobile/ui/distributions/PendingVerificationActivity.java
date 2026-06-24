package intl.iom.bravemobile.ui.distributions;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.graphics.drawable.DrawableCompat;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.graphics.drawable.Drawable;
import android.os.Bundle;
import android.text.format.DateFormat;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.UUID;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.VerificationAdapter;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.VerificationExtra;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.activities.ActivityListPage;
import intl.iom.bravemobile.ui.verifications.VerificationDetailsPage;
import intl.iom.bravemobile.ui.verifications.VerificationListPage;

public class PendingVerificationActivity extends AppCompatActivity {

    private static final String TAG = PendingVerificationActivity.class.getSimpleName();
    @Override
    public void onBackPressed() {
        return;
    }

    DistributionService distributionService ;

    VerificationAdapter adapter;
    TextView emptyView;
    RecyclerView rv;

    WithLoading withLoading;

    private ActivityResultLauncher<Intent> pendingVerificationLauncher;

    boolean shouldExecuteOnLoad = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_verification_list_page);

        setTitle("Pending Verification");


        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);

        rv = findViewById(R.id.rvVerifications);
        emptyView = findViewById(R.id.emptyView);

        VerificationService verificationService = ServiceLocator.verificationService(this);
        distributionService = ServiceLocator.distributionService(this);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);

        String code = registrationActivityService.getCurrent();

        Distribution distribution = registrationActivityService.getDistributionById( code, distributionService.getCurrent() );

        DistDto dto = new DistDto(distribution);
        dto.activityCode =  code;

        List<Verification> verifications = verificationService.getPendingVerification(code);


        shouldExecuteOnLoad = getIntent() != null &&
                getIntent().hasExtra(IntentKeys.AUTO_EXECUTE) &&
                getIntent().getBooleanExtra(IntentKeys.AUTO_EXECUTE, false);

        //shouldExecuteOnLoad = false;


        adapter = new VerificationAdapter(this, verifications, true, new VerificationAdapter.OnItemActionListener() {
            @Override
            public void getMatch(String uuid) {

                Intent intent = new Intent(PendingVerificationActivity.this, VerificationDetailsPage.class);
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

                    VerificationExtra v =  new VerificationExtra();
                    v.DistributionId =  distribution.distributionId;

                    String extra = ObjectSerializer.serialize(v);

                    verificationService.verifyTemplates( code, extra, payload, new BasicCallback() {
                        @Override
                        public void onSuccess() {

                            runOnUiThread(() ->{

                                Toast.makeText(PendingVerificationActivity.this, "Request sent to the server. Searching for a biometric match", Toast.LENGTH_SHORT).show();

                                List<Verification> verifications = verificationService.getPendingVerification(
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
                                    Toast.makeText(PendingVerificationActivity.this, t.getMessage(), Toast.LENGTH_SHORT).show()
                            );

                            cb.onFailure(t);
                        }
                    });

                });

            }

            @Override
            public void deleteVerification(String uuid) {

                verificationService.deleteVerification(code, uuid, new BasicCallback() {
                    @Override
                    public void onSuccess() {

                        AlertDialogUtils.confirmDialog(PendingVerificationActivity.this, "Delete Biometric Check Record", "Confirm deletion of biometric check record", new AlertDialogUtils.Callback() {
                            @Override
                            public void onPositive() {

                                BiometricFlow flow = new ClientBiometricFlow();
                                flow.init(PendingVerificationActivity.this);

                                flow.delete(uuid);

                                setResult(RESULT_OK);
                                finish();

                            }
                        });

                    }

                    @Override
                    public void onFailure(Throwable t) {

                    }
                });

            }

            @Override
            public void getEnrollment(Verification verification) {

                Family restored = ObjectSerializer.deserialize(verification.data, Family.class);

                if(! distributionService.isEnrolled(code, dto.id, restored.getHohid())){

                    AlertDialogUtils.confirmDialog(PendingVerificationActivity.this, getString(R.string.beneficiary_enrollment_required_lbl), getString(R.string.beneficiary_enrolled_to_distr), new AlertDialogUtils.Callback() {
                        @Override
                        public void onPositive() {

                            if(distributionService.mapHousehold(code, dto.id, verification.uuid,restored))
                            {
                                Intent result = new Intent();
                                result.putExtra(IntentKeys.HOUSEHOLD_ID, verification.uuid);
                                setResult(RESULT_OK, result);
                                finish();
                            }

                        }
                    });

                }



            }
        });
        rv.setAdapter(adapter);

        // Call once after initial data load too:
        emptyView.setVisibility(verifications.size() == 0 ? View.VISIBLE : View.GONE);
        rv.setVisibility(verifications.size() == 0 ? View.GONE : View.VISIBLE);

        if(shouldExecuteOnLoad){
            adapter.executeOnLoad();
        }


    }
}