package intl.iom.bravemobile.ui.distributions;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.DividerItemDecoration;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.EditText;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.button.MaterialButton;
import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.Set;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.DistItemAdapter;
import intl.iom.bravemobile.adapters.MemberAdapter;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.services.CameraCaptureHelper;
import intl.iom.bravemobile.services.GpsService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DistributionType;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.registrations.MemberDetailsPage;

public class MatchFoundPage extends AppCompatActivity {

    private static final String TAG = MatchFoundPage.class.getSimpleName();
    /*public static final String EXTRA_FAMILY_JSON = "extra_family_json";
    public static final String EXTRA_DISTRIBUTION_ID = "extra_distribution_id";
    public static final String EXTRA_DISTRIBUTION_TYPE = "extra_distribution_type";
    public static final String EXTRA_RESULT_JSON = "extra_result_json";*/

    private static final String TYPE_INDIVIDUAL = "individual";
    /*private static final String TYPE_FAMILY = "family";
    private static final String TYPE_GROUP = "group";*/

    private TextView tvHohid;
    private TextView tvType;
    private RecyclerView rvMembers;
    private RecyclerView rvItems;
    private MaterialButton btnSubmit;

    private TextView tvSelectReason;
    private Spinner spinnerReason;
    private EditText etComment;

    private MemberAdapter adapter;
    private DistItemAdapter itemAdapter;
    private Family family;
    private int distributionId = 0;
    //private String distributionType = TYPE_INDIVIDUAL;
    private boolean singleSelect = false;

    private boolean biometricConfirmation = false;
    private boolean photoConfirmation = false;
    private String photoBase64;

    private boolean assistanceConfirmed = false;

    private SecureStore secureStore;

    @Override
    public void onBackPressed() {
        return;
    }

    private final BiometricFlow flow = new ClientBiometricFlow();

    private DistDto dto;

    private ActivityResultLauncher<Intent> biometricLauncher;
    private CameraCaptureHelper cameraHelper;

    private DistributionService distributionService;
    private GpsService gpsService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_family_selection);

        setTitle("Distribution Assistance");

        distributionService = ServiceLocator.distributionService(this);

        gpsService = ServiceLocator.gpsService(this);

        secureStore = ServiceLocator.secureStore(this);

        tvHohid = findViewById(R.id.tvHohid);
        tvSelectReason = findViewById(R.id.tvSelectReason);
        spinnerReason = findViewById(R.id.spinnerReason);
        etComment = findViewById(R.id.etComment);

        tvType = findViewById(R.id.tvType);
        rvMembers = findViewById(R.id.rvMembers);
        rvItems = findViewById(R.id.rvItems);
        btnSubmit = findViewById(R.id.btnSubmit);

        cameraHelper = new CameraCaptureHelper(this, this);

        flow.init(MatchFoundPage.this);

        biometricLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {
            BiometricFlow.CaptureResult cr = flow.parseResult(result.getResultCode(), result.getData());
            if (!cr.success) {
                Toast.makeText(this,cr.error, Toast.LENGTH_SHORT);
                return;
            }

            if(cr.operation==null)
                return;

            switch (cr.operation) {

                case CAPTURE_BIOMETRIC:

                    Bundle payload = cr.payload;

                    if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_RESULT_TYPE))
                    {
                        String result_type = payload.getString(BiometricFlow.EXTRA_RESULT_TYPE);

                        //Toast.makeText(MatchFoundPage.this, result_type,Toast.LENGTH_SHORT).show();

                        if(result_type.equalsIgnoreCase("verify_identify"))
                        {
                            assistanceConfirmed = true;
                            biometricConfirmation = true;
                        }


                    }

                    break;
            }
        });

        // Parse inputs
        String input = getIntent().getStringExtra(IntentKeys.HOUSEHOLD_ID);

        //if scan biometric, no need for confirmation
        biometricConfirmation = getIntent().getBooleanExtra(IntentKeys.Is_BIOMETRIC_CAPTURED, false);

        dto = distributionService.getCurItems();

        singleSelect = true;// !TYPE_INDIVIDUAL.equals(distributionType); // single for family/group

        assistanceConfirmed = (!dto.biometricReceiptRequired && !dto.photoReceiptRequired ); //auto confirm if both are false

        family = distributionService.getEnrolledBeneficiary(dto.activityCode,dto.id,input);

        // Bind header
        tvHohid.setText("Household: " + family.getHohid());
        tvType.setText("Type: " + family.getType());

        // RecyclerView setup
        adapter = new MemberAdapter(family.getMembers(), singleSelect, selectedUuids -> {
            // Optional: enable/disable submit based on rules
            // For example, to require at least one selected in single-select:
            // btnSubmit.setEnabled(!singleSelect || !selectedUuids.isEmpty());
        }, R.drawable.ic_no_photo_24);
        rvMembers.setLayoutManager(new LinearLayoutManager(this));
        rvMembers.setAdapter(adapter);
        rvMembers.addItemDecoration(new DividerItemDecoration(this, DividerItemDecoration.VERTICAL));

        //items setup
        itemAdapter = new DistItemAdapter(dto.items);

        rvItems.setLayoutManager(new LinearLayoutManager(this));
        rvItems.setAdapter(itemAdapter);


        ArrayAdapter<CharSequence> adapter =
                ArrayAdapter.createFromResource(
                        this,
                        R.array.reason_options,
                        android.R.layout.simple_spinner_item
                );

        spinnerReason.setAdapter(adapter);


        // Spinner selection listener
        spinnerReason.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {

                if (position > 0) {
                    etComment.setEnabled(true);
                    etComment.setAlpha(1.0f);   // visually enabled
                    itemAdapter.unlock();
                } else {
                    etComment.setText("");      // optional: clear text
                    etComment.setEnabled(false);
                    etComment.setAlpha(0.5f);   // visually disabled
                    itemAdapter.lock();
                }
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
                etComment.setEnabled(false);
            }
        });

        btnSubmit.setOnClickListener(v -> onSubmit());

        if(!dto.biometricReceiptRequired)
            findViewById(R.id.fabFP).setVisibility(View.GONE);

        if(!dto.photoReceiptRequired)
            findViewById(R.id.fabPicture).setVisibility(View.GONE);

        findViewById(R.id.fabFP).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                try
                {
                    Bundle extras = new Bundle();
                    //BiometricFlow.EXTRA_IS_VERIFY_ONLY

                    extras.putString(BiometricFlow.EXTRA_INDIVIDUAL_ID, family.getHohid());
                    extras.putBoolean(BiometricFlow.EXTRA_IS_VERIFY_ONLY, true);
                    extras.putBoolean(BiometricFlow.EXTRA_ALLOW_DATA_UPDATE, false);
                    biometricLauncher.launch(flow.createIntent(MatchFoundPage.this, BiometricFlow.Operation.CAPTURE_BIOMETRIC, extras));

                }
                catch (ActivityNotFoundException e)
                {
                    Toast.makeText(MatchFoundPage.this,"Unable to start biometric capture.", Toast.LENGTH_LONG).show();
                }
                catch (Exception e){
                    Toast.makeText(MatchFoundPage.this,e.getMessage(), Toast.LENGTH_LONG).show();
                }

            }
        });

        findViewById(R.id.fabPicture).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                cameraHelper.capture(new CameraCaptureHelper.Listener() {
                    @Override public void onCaptured(CameraCaptureHelper.Result r) {
                        // preview
                        if (r.bitmap != null)
                        {
                            assistanceConfirmed = true;
                            photoConfirmation=true;
                            photoBase64 = r.base64;
                        }
                        // keep base64 / file path on your model
                        //photoBase64 = r.base64;
                        // r.filePath has the saved full image
                    }
                    @Override public void onCanceled() {
                        // user canceled – optional toast
                    }
                    @Override public void onError(Exception e) {
                        // show error
                    }
                });


            }
        });

        findViewById(R.id.fabExitSearch).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                finish();
            }
        });

    }



    private void onSubmit() {
        Set<String> selected = adapter.getCurrentSelection();

        try {
            JSONObject root = new JSONObject();
            root.put("familyUuid", family.getHouseholdUuid());
            root.put("distribution", distributionId);

            //want to know the location where the goods were delivered
            root.put("collectedAt", gpsService.getGpsCoordinates());

            JSONArray arr = new JSONArray();

            boolean one = false;

            int household_id = 0;

            for (Family.Member m : family.getMembers()) {

                if(!one && selected.contains(m.getUuid()))
                {
                    household_id = m.getMemno();
                    one = true;
                }

                JSONObject item = new JSONObject();
                item.put("uuid", m.getUuid());
                item.put("selected", selected.contains(m.getUuid()));
                arr.put(item);
            }

            if(!one)
            {
                //Toast.makeText(MatchFoundPage.this, "", Toast.LENGTH_SHORT).show();

                AlertDialogUtils.showDialog(MatchFoundPage.this,"Select Recipient",
                        "Please select the individual or designated person who will receive the assistance."
                ,R.string.msg_ok);


                return;
            }

            root.put("selections", arr);

            JSONObject distItemsMap = itemAdapter.getSelectionJson(); // e.g. {"1":true,"2":false,...}
            root.put("itemSelections", distItemsMap);

            if(itemAdapter.emptySelection())
            {
                AlertDialogUtils.showDialog(MatchFoundPage.this,"Selection Required",
                        "Please select at least one item."
                        ,R.string.msg_ok);

                return;
            }


            String decision = spinnerReason.getSelectedItem().toString();

            if( decision.equalsIgnoreCase(getString(R.string.some_items_provided))
                && EditTextUtils.Empty(etComment)
            ){

                AlertDialogUtils.showDialog(MatchFoundPage.this,"Comment Needed",
                        "If certain items were not delivered, a comment is required."
                        ,R.string.msg_ok);

                return;

            }

            root.put("comment", etComment.getText().toString());


            if(!assistanceConfirmed)
            {
                AlertDialogUtils.showDialog(MatchFoundPage.this,"Confirm Assistance Receipt",
                        "Please confirm receipt of assistance via biometric or photo capture, as available."
                        ,R.string.msg_ok);

                return;

            }

            root.put("assistanceConfirmed", assistanceConfirmed);

            root.put("biometricConfirmation", biometricConfirmation);
            root.put("photoConfirmation", photoConfirmation);
            root.put("photoBase64", photoBase64);

            root.put("enumerator", secureStore.getEnumerator());

            if(saveAssistance(dto.activityCode,dto.id, family.getHohid(), household_id, root))
            {
                Intent result = new Intent();
                //result.putExtra(EXTRA_RESULT_JSON, root.toString());
                setResult(Activity.RESULT_OK, result);
                finish();
            }
            else {

                String beneficiary_subject = DistributionType.INDIVIDUAL.name().toLowerCase();

                if(DistributionType.fromCode(dto.type) == DistributionType.FAMILY)
                    beneficiary_subject = DistributionType.FAMILY.name().toLowerCase();

                Toast.makeText(MatchFoundPage.this, String.format("Assistance has already been provided for this %s.", beneficiary_subject), Toast.LENGTH_SHORT).show();
                Toast.makeText(MatchFoundPage.this, "Please contact your supervisor.", Toast.LENGTH_SHORT).show();
            }


        } catch (JSONException e) {
            e.printStackTrace();
            setResult(Activity.RESULT_CANCELED);
            finish();
        }
    }

    private boolean saveAssistance(String activityCode,int distribution, String familyId, int individualId, JSONObject root) {

        return distributionService.saveAssistance( activityCode,distribution,  familyId,  individualId,  root.toString());

    }


}