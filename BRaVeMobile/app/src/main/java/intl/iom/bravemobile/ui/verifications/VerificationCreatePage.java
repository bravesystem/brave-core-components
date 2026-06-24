package intl.iom.bravemobile.ui.verifications;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;

import android.app.Activity;
import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.statics.ReservedLookups;
import intl.iom.bravemobile.ui.registrations.MemberDetailsPage;

public class VerificationCreatePage extends AppCompatActivity {


    private Spinner spinnerGender;
    private Button btnCapture;

    private Button btnSubmit;
    private Button btnClear;
    private TextView tvBiometricStatus;

    private boolean biometricCaptured = false;

    private final BiometricFlow flow = new ClientBiometricFlow();

    private ActivityResultLauncher<Intent> biometricLauncher;

    private String biometricBase64;
    private String uuid;

    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_verification_create_page);

        setTitle("New Biometric Record Check");

        VerificationService verificationService = ServiceLocator.verificationService(this);

        String code = ServiceLocator.registrationActivityService(this).getCurrent();

        spinnerGender = findViewById(R.id.spinnerGender);
        btnCapture = findViewById(R.id.btnCapture);
        btnSubmit = findViewById(R.id.btnSubmit);
        btnClear = findViewById(R.id.btnClear);

        tvBiometricStatus = findViewById(R.id.tvBiometricStatus);

        flow.init(VerificationCreatePage.this);

        /*
        DO NOT UNCOMMENT
        String[] subjects = flow.listIds();

        for(String s: subjects)
            flow.delete(s);*/

        //householdRegistrationService.deleteTmpSubjects(household.activityCode);


        biometricLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {
            BiometricFlow.CaptureResult cr = flow.parseResult(result.getResultCode(), result.getData());

            if (!cr.success) {
                Toast.makeText(this,cr.error, Toast.LENGTH_SHORT);
                return;
            }
            switch (cr.operation) {

                case CAPTURE_BIOMETRIC:

                    Bundle payload = cr.payload;

                    if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_RAW_DATA))
                    {
                        byte[] bytes = payload.getByteArray(BiometricFlow.EXTRA_RAW_DATA);

                        biometricBase64 = Bytes.toBase64(bytes);

                        Toast.makeText(VerificationCreatePage.this, getString(R.string.biometric_collection_success), Toast.LENGTH_SHORT).show();

                        biometricCaptured = true;

                        setBiometricStatus();

                    }

                    break;
            }
        });



        // 1) Build your genders list (you can pass it from previous activity or fetch)
        List<SelectItem> genders = new ArrayList<>();

        // Add a placeholder as first item (value=0 means "invalid selection")
        genders.add(new SelectItem(-1, "-- gender --"));

        Optional<List<SelectItem>> _genders = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_GENDERS);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            if(_genders.isPresent())
            {
                genders.addAll(_genders.get());
            }
        }

        // 2) Create an ArrayAdapter using toString() for label
        ArrayAdapter<SelectItem> genderAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, genders);
        genderAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spinnerGender.setAdapter(genderAdapter);


// 3) Enable button only when selection is valid (value != 0)
        spinnerGender.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long l) {

                SelectItem selected = (SelectItem) parent.getItemAtPosition(position);
                boolean valid = selected.getValue() > 0;
                btnCapture.setEnabled(valid);

                if(!valid)
                    btnSubmit.setEnabled(false);


            }

            @Override
            public void onNothingSelected(AdapterView<?> adapterView) {

            }
        });

        // 4) Handle click: read selected item & proceed (toast for now)
        btnCapture.setOnClickListener(v -> {
            /*SelectItem selected = (SelectItem) spinnerGender.getSelectedItem();
            int genderCode = selected.getValue(); // 1 or 2
            String genderLabel = selected.getLabel();*/

            //Toast.makeText(this, "Proceed with " + genderLabel + " (code " + genderCode + ")", Toast.LENGTH_SHORT).show();

            Bundle extras = new Bundle();

            uuid = UUID.randomUUID().toString();

            //BiometricFlow.EXTRA_IS_VERIFY_ONLY
            extras.putString(BiometricFlow.EXTRA_INDIVIDUAL_ID, uuid);
            extras.putBoolean(BiometricFlow.EXTRA_IS_VERIFY_ONLY, false);
            extras.putBoolean(BiometricFlow.EXTRA_IS_REGISTRATION_ONLY, true);
            extras.putBoolean(BiometricFlow.EXTRA_ALLOW_DATA_UPDATE, false);
            try {
                biometricLauncher.launch(flow.createIntent(VerificationCreatePage.this, BiometricFlow.Operation.CAPTURE_BIOMETRIC, extras));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }


            //biometricCaptured = true;

            //setBiometricStatus();

            // TODO: Launch fingerprint capture flow, then save verification:
            // e.g. new Verification(uuid, genderCode, System.currentTimeMillis(), false, false, null, "")
            // and return to list
        });

        btnClear.setOnClickListener(v -> {
            if(biometricCaptured)
            {
                AlertDialogUtils.confirmDialog(VerificationCreatePage.this, "Confirm", "Clearing the biometric capture will require you to retake it. Do you want to proceed?", new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {
                        biometricCaptured = false;
                        setBiometricStatus();
                    }
                });
            }
        });

        btnSubmit.setOnClickListener(v  ->{

            verificationService.saveVerification(code, false,
                    new Verification(
                            uuid,
                            SpinnerUtils.getSelected(spinnerGender),
                            biometricBase64
                    ));

            Intent data = new Intent();
            data.putExtra(IntentKeys.REFRESH, true);
            setResult(Activity.RESULT_OK, data);
            finish();

        });


        setBiometricStatus();



    }

    private void setBiometricStatus() {
        String yesNo = getString(R.string.msg_no);

        if(biometricCaptured){
            yesNo = getString(R.string.msg_yes);
        }

        String text = String.format(getString(R.string.label_biometric_captured), yesNo);

        tvBiometricStatus.setText(text);

        btnSubmit.setEnabled(biometricCaptured);
    }
}