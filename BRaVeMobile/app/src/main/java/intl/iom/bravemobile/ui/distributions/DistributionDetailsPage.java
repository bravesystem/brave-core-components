package intl.iom.bravemobile.ui.distributions;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.button.MaterialButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Locale;
import java.util.UUID;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.KitsAdapter;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.EnrollmentCallback;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.distributions.EnrollmentResponse;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.registrations.MemberListPage;
import intl.iom.bravemobile.ui.registrations.RegistrationDetailsPage;
import intl.iom.bravemobile.ui.verifications.VerificationCreatePage;

public class DistributionDetailsPage extends AppCompatActivity {

    private static String TAG = DistributionDetailsPage.class.getSimpleName();

    private TextView tvDistributionTitle, tvDistributionId;
    private RecyclerView rvKits;

    MaterialButton btnFpScan, btnSearch;

    private ActivityResultLauncher<Intent>pendingVerificationLauncher;


    @Override
    public void onBackPressed() {
        return;
    }

    private final BiometricFlow flow = new ClientBiometricFlow();

    private ActivityResultLauncher<Intent> biometricLauncher;

    private ActivityResultLauncher<Intent> showMatchLauncher;
    private RegistrationActivityService registrationActivityService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_distribution);

        setTitle("Distribution details");

        DistributionService distributionService = ServiceLocator.distributionService(this);
        HouseholdRegistrationService householdRegistrationService = ServiceLocator.householdRegistrationService(this);

        registrationActivityService = ServiceLocator.registrationActivityService(this);

        VerificationService verificationService = ServiceLocator.verificationService(this);


        tvDistributionTitle = findViewById(R.id.tvDistributionTitle);
        tvDistributionId = findViewById(R.id.tvDistributionId);
        rvKits = findViewById(R.id.rvKits);

        btnFpScan = findViewById(R.id.btnItemFpScan);
        btnSearch = findViewById(R.id.btnItemSearch);

        String code = registrationActivityService.getCurrent();

        Distribution distribution = registrationActivityService.getDistributionById( code, distributionService.getCurrent() );

        DistDto dto = new DistDto(distribution);
        dto.activityCode =  code;

        distributionService.setCurItems(dto);

        tvDistributionTitle.setText(distribution.title);
        tvDistributionId.setText("ID: " + distribution.distributionId);

        rvKits.setLayoutManager(new LinearLayoutManager(this));
        rvKits.setAdapter(new KitsAdapter(distribution.kits));

        showMatchLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {
                Toast.makeText(DistributionDetailsPage.this, "Beneficiary marked as received.", Toast.LENGTH_SHORT).show();
            }

        });

        biometricLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            BiometricFlow.CaptureResult cr = flow.parseResult(result.getResultCode(), result.getData());

            if (!cr.success)
            {
                Toast.makeText(this,cr.error, Toast.LENGTH_SHORT).show();
                return;
            }

            if(cr.operation==null)
                return;

            switch (cr.operation) {

                case CAPTURE_BIOMETRIC:

                    Bundle payload = cr.payload;

                    //no match found, but ask to save collected biometric => create biometric verification request
                    if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_RESULT_TYPE) &&  BiometricFlow.ResultType.INDENTIFY_KEEP_BIOMETRIC.name().equals(payload.getString(BiometricFlow.EXTRA_RESULT_TYPE)) )
                    {

                        AlertDialogUtils.confirmDialog(DistributionDetailsPage.this, "Biometric Check", "Save the biometric data and send it to the backend for record verification.", new AlertDialogUtils.Callback() {
                            @Override
                            public void onPositive() {


                                if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_RAW_DATA))
                                {
                                    byte[] bytes = payload.getByteArray(BiometricFlow.EXTRA_RAW_DATA);

                                    String biometricBase64 = Bytes.toBase64(bytes);

                                    String dbid = UUID.randomUUID().toString();

                                    verificationService.saveVerification(code, true,
                                            new Verification(
                                                    dbid,
                                                    0, //unspecified
                                                    biometricBase64
                                            ));

                                    List<BiometricFlow.BiometricSubject> subjects = new ArrayList<>(
                                            Arrays.asList(new BiometricFlow.BiometricSubject(dbid, bytes))
                                    );

                                    if(subjects.size()>0)
                                        flow.enrollBatch(subjects);


                                    handlePendingVerification();


                                }

                            }
                        });

                    }

                    //match found. it's either in the registration list or enrolled beneficiaries
                    //if enrolled beneficiaries=>  all good (see onSuccess)
                    //if freshly registered beneficiaries, ideally we can decide to enroll based on the distribution type (see onFailureCheckOnline)

                    else if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_MATCHING_ID))
                    {
                        String matching_id = payload.getString(BiometricFlow.EXTRA_MATCHING_ID);

                        try
                        {
                            UUID.fromString(matching_id);
                            matching_id = verificationService.getMatchingId(code, matching_id);
                        }
                        catch (IllegalArgumentException ex)
                        {

                        }

                        if(!StringUtils.isBlank(matching_id))
                        {
                            String hhId = DataCollectionUtils.getHouseholdId(matching_id);
                            distributionService.checkEnrollment(dto.activityCode, dto.id, hhId, new EnrollmentCallback() {
                                @Override
                                public void onSuccess() {
                                    Intent i = new Intent(DistributionDetailsPage.this, MatchFoundPage.class);
                                    i.putExtra(IntentKeys.HOUSEHOLD_ID, hhId);
                                    showMatchLauncher.launch(i);
                                }

                                @Override
                                public void onFailure(Throwable t) {

                                    Toast.makeText(DistributionDetailsPage.this, t.getMessage(), Toast.LENGTH_SHORT).show();

                                }

                                @Override
                                public void onFailureCheckOnline(Throwable t){


                                    if(!dto.allowDownload()){
                                        Toast.makeText(DistributionDetailsPage.this, getText(R.string.beneficiary_no_enrolled), Toast.LENGTH_SHORT).show();
                                        return;
                                    }

                                    AlertDialogUtils.confirmDialog(DistributionDetailsPage.this, getString(R.string.beneficiary_enrollment_required_lbl), getString(R.string.beneficiary_enrolled_to_distr), new AlertDialogUtils.Callback() {
                                        @Override
                                        public void onPositive() {

                                            Family family = null;

                                            try
                                            {

                                                family = householdRegistrationService.getDistributionTemplate(code, hhId);

                                                if(family==null)
                                                {
                                                    Toast.makeText(DistributionDetailsPage.this, getString(R.string.beneficiary_under_different_activity), Toast.LENGTH_SHORT).show();
                                                    return;

                                                }

                                                if(distributionService.saveEnrollment(dto.activityCode, dto.id, hhId, family))
                                                {
                                                    showMatch(true, hhId);
                                                }

                                            }
                                            catch (HouseholdError e)
                                            {
                                                //throw new RuntimeException(e);
                                                Toast.makeText(DistributionDetailsPage.this, getString(R.string.beneficiary_possible_entry_error), Toast.LENGTH_SHORT).show();
                                            }

                                        }

                                    });


                                }
                            });

                            ///
                        }



                    }


                    break;
                case ACTIVATION:
                    // maybe no payload, just confirm success
                    Bundle test1 = cr.payload;
                    break;
            }
        });

        flow.init(DistributionDetailsPage.this);

        btnFpScan.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {


                try
                {
                    if(!registrationActivityService.isActive(code))
                    {
                        AlertDialogUtils.showDialog(DistributionDetailsPage.this, getString(R.string.activity_closed_lbl), getString(R.string.activity_closed_text), R.string.msg_ok);
                        return;
                    }
                }
                catch (ActivityNotFoundException e)
                {
                    Log.e(TAG, e.getMessage());
                    Toast.makeText(DistributionDetailsPage.this,"Unable to start biometric capture.", Toast.LENGTH_LONG).show();
                    return;
                } catch (RegistrationActivityNotFound e) {
                    throw new RuntimeException(e);
                }

                boolean hasPendingVerification = verificationService.hasPendingVerification(code);

                if (hasPendingVerification) {
                    handlePendingVerification();
                } else {
                    startBiometricCaptureFlow();
                }

            }
        });


        btnSearch.setOnClickListener(v ->{

                   try
                    {

                        if(!registrationActivityService.isActive(code)){
                            AlertDialogUtils.showDialog(DistributionDetailsPage.this, getString(R.string.activity_closed_lbl), getString(R.string.activity_closed_text), R.string.msg_ok);
                            return;
                        }

                    }
                    catch (ActivityNotFoundException e)
                    {
                        Log.e(TAG, e.getMessage());
                        Toast.makeText(DistributionDetailsPage.this,"Unable to initiate manual search.", Toast.LENGTH_LONG).show();
                        return;
                    } catch (RegistrationActivityNotFound e) {
                        throw new RuntimeException(e);
                    }


                    Intent i = new Intent(this, ManualSearchPage.class);
                    startActivity(i);

                }

        );

        pendingVerificationLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK && result.getData() != null)
            {
                String householdId = result.getData().getStringExtra(IntentKeys.HOUSEHOLD_ID);
                // Use the returned data

                try
                {
                    UUID.fromString(householdId);
                    householdId = verificationService.getMatchingId(code, householdId);
                }
                catch (IllegalArgumentException ex)
                {

                }

                showMatch(true, householdId);

                /*Intent i = new Intent(DistributionDetailsPage.this, MatchFoundPage.class);
                i.putExtra(IntentKeys.HOUSEHOLD_ID, householdId);
                showMatchLauncher.launch(i);*/

            }


        });


    }

    private void showMatch(boolean is_biometric_captured, String householdId){
        Intent i = new Intent(DistributionDetailsPage.this, MatchFoundPage.class);
        i.putExtra(IntentKeys.Is_BIOMETRIC_CAPTURED, is_biometric_captured);
        i.putExtra(IntentKeys.HOUSEHOLD_ID, householdId);
        showMatchLauncher.launch(i);
    }

    private void startBiometricCaptureFlow() {

        try
        {
            Bundle extras = new Bundle();
            extras.putString(BiometricFlow.EXTRA_INDIVIDUAL_ID, UUID.randomUUID().toString());
            extras.putBoolean(BiometricFlow.EXTRA_IS_VERIFY_ONLY, true);
            extras.putBoolean(BiometricFlow.EXTRA_ALLOW_DATA_UPDATE, false);
            extras.putBoolean(BiometricFlow.EXTRA_IS_AUTH, true);
            biometricLauncher.launch(flow.createIntent(DistributionDetailsPage.this, BiometricFlow.Operation.CAPTURE_BIOMETRIC, extras));

        }
        catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e); //not happening
        }
        catch (Exception e){
            Toast.makeText(DistributionDetailsPage.this,e.getMessage(), Toast.LENGTH_LONG).show();
        }


    }

    private void handlePendingVerification()
    {
        Intent i = new Intent(DistributionDetailsPage.this, PendingVerificationActivity.class);
        i.putExtra(IntentKeys.AUTO_EXECUTE, true);
        pendingVerificationLauncher.launch(i);

    }


}