package intl.iom.bravemobile.ui.registrations;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.HouseholdAdapter;
import intl.iom.bravemobile.adapters.RegistrationActivityAdapter;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.LookupItemNotFound;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.interfaces.CommentCallback;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.CustomValidationResponse;
import intl.iom.bravemobile.models.DeviceActivationResult;
import intl.iom.bravemobile.models.activities.Consent;
import intl.iom.bravemobile.models.activities.ConsentFlow;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.jwt.ClaimResponse;
import intl.iom.bravemobile.models.registrations.FlaggedData;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.RetrofitService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.ConsentType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.ui.LoginPage;
import intl.iom.bravemobile.ui.activities.ActivityListPage;
import retrofit2.Call;
import retrofit2.Response;
import retrofit2.Retrofit;

public class RegistrationListPage extends AppCompatActivity {

    private static String TAG = RegistrationListPage.class.getSimpleName();

    private ActivityResultLauncher<Intent> updateHouseholdLauncher;
    private ListView lvResults;
    private TextView emptyView;
    private HouseholdAdapter adapter;
    private final BiometricFlow flow = new ClientBiometricFlow();

    private RegistrationActivityService registrationActivityService;
    private HouseholdRegistrationService householdRegistrationService;
    private SecureStore secureStore;
    private Retrofit client;

    @Override
    public void onBackPressed() {
        return;
    }

    String code ;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_registration_list_page);

        registrationActivityService = ServiceLocator.registrationActivityService(this);

        flow.init(RegistrationListPage.this);

        code = registrationActivityService.getCurrent();


        String title = String.format("Registration List - Activity : %s",code.toUpperCase());

        setTitle(title);


        RetrofitService retrofitService = null;
        try
        {
            retrofitService = new RetrofitService(this);
            client = retrofitService.getClient();
        }
        catch (GeneralSecurityException e)
        {
            throw new RuntimeException(e);
        }
        catch (IOException e)
        {
            throw new RuntimeException(e);
        }

        EnumeratorService enumeratorService = ServiceLocator.enumeratorService(this);

        secureStore = ServiceLocator.secureStore(this);

        LookupService lookupService = ServiceLocator.lookupService(this);

        List<Consent> consents = new ArrayList<>();

        try {
            Optional<RegistrationActivity> registrationActivity = ServiceLocator.registrationActivityService(this).getRegistrationActivity(code);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                if(registrationActivity.isPresent())
                {
                    if(registrationActivity.get().consents.isPresent())
                        for(Consent c : registrationActivity.get().consents.get()){
                            if(c.type== ConsentType.HOUSEHOLD)
                                consents.add(c);

                        }
                }
            }

        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }

        boolean is_biometric_required = false;
        int age_threshold = 5;

        try {
            is_biometric_required = (boolean)registrationActivityService.getPreference(
                    DefinedPreferences.BIOMETRIC_COLLECTION_ENABLED,
                    false
            );

            age_threshold =
                    (int)registrationActivityService.getPreference(
                            DefinedPreferences.BIOMETRIC_AGE_THRESHOLD,
                            5
                    );
        } catch (UnsupportedPreferenceType e) {
            Log.e(TAG, e.getMessage());
        } catch (RegistrationActivityNotFound e) {
            Log.e(TAG, e.getMessage());
        }



        householdRegistrationService = ServiceLocator.householdRegistrationService(this);

        List<Consent> finalConsents = consents;

        //findViewById(R.id.fabDownloadFlaggedData).setVisibility(View.GONE);

        findViewById(R.id.fabDownloadFlaggedData).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

             if(!enumeratorService.isSupervisor())
             {
                 AlertDialogUtils.showDialog(RegistrationListPage.this, "Supervisor Access Required", "Only supervisors are authorized to download the list of registration data marked for review.", R.string.msg_ok);
                return;
             }
            AlertDialogUtils.inputDialog(RegistrationListPage.this, "Enter the partition code to download registration data marked for review.", "", new AlertDialogUtils.InputCallback() {
                @Override
                public void onInput(String claim) {

                    //activateAsync(env, claim);
                    Toast.makeText(RegistrationListPage.this, "downloading..", Toast.LENGTH_SHORT).show();
                    downloadRegistrationListAsync(claim);


                }
            });

            }
        });

        findViewById(R.id.fabNewRegistration).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                try {

                    if(!registrationActivityService.isActive(code)){
                        AlertDialogUtils.showDialog(RegistrationListPage.this, getString(R.string.activity_closed_lbl), getString(R.string.activity_closed_text), R.string.msg_ok);
                        return;
                    }

                } catch (RegistrationActivityNotFound e) {
                    throw new RuntimeException(e); //not happening
                }

                ConsentFlow.start(
                        RegistrationListPage.this, // Activity
                        finalConsents, // List<Consent>
                        new ConsentFlow.Listener() {
                            @Override public void onCompleted(LinkedHashMap<Integer, Boolean> answers) {
                                // all done (or stopped because a required = No)
                                // answers: consentId -> true/false
                                //startActivity(new Intent(RegistrationListPage.this, RegistrationCreatePage.class));
                                Intent i = new Intent(RegistrationListPage.this, RegistrationCreatePage.class);
                                ConsentsFeedback feedback = new ConsentsFeedback();
                                feedback.answers = answers==null?new LinkedHashMap<>():answers;
                                i.putExtra(Extras.EXTRA_CONSENT_FLOW,feedback);
                                updateHouseholdLauncher.launch(i);

                            }
                            @Override public void onStopped(Consent stoppingConsent, LinkedHashMap<Integer, Boolean> answers) {
                                // stopped because a REQUIRED consent got "No"

                                AlertDialogUtils.showCommentDialog(RegistrationListPage.this, null, new CommentCallback() {
                                    @Override
                                    public void onComment(String text) {

                                        ConsentsFeedback feedback = new ConsentsFeedback();
                                        feedback.answers = answers;
                                        feedback.comment = text;

                                        String data = ObjectSerializer.serialize(feedback);

                                        householdRegistrationService.saveConsent(code, data);

                                        Toast.makeText(RegistrationListPage.this, "Consent feedback saved..", Toast.LENGTH_SHORT).show();
                                    }
                                });
                            }
                        }
                );

            }
        });


        lvResults = findViewById(R.id.lvResults);
        emptyView = findViewById(R.id.emptyView);
        lvResults.setEmptyView(emptyView);

        updateHouseholdLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {
                    adapter.setData(householdRegistrationService.getAll(code));
            }

        });

        //Household dummy = Household.getDummy();
        //dummy.activityCode = code;

        //householRegistrationService.saveHousehold(dummy);

        List<Household> households = householdRegistrationService.getAll(code);

        boolean finalIs_biometric_required = is_biometric_required;
        int finalAge_threshold = age_threshold;
        adapter = new HouseholdAdapter(this, households, new HouseholdAdapter.OnItemActionListener() {
            @Override
            public String getLookupItemLabel(int id, int domain) {

                String label = "-";
                try
                {
                    label = lookupService.getLookupItemLabel(id, domain);
                }
                catch (LookupItemNotFound ex)
                {
                    Toast.makeText(RegistrationListPage.this,ex.getMessage(),Toast.LENGTH_SHORT).show();
                }

                return label;
            }

            @Override
            public Individual getHeadOfHousehold(Household household) {

                try {
                    return householdRegistrationService.getHeadOfHousehold(household.householdId);
                }
                catch (Exception ex)
                {
                    Toast.makeText(RegistrationListPage.this,ex.getMessage(),Toast.LENGTH_SHORT).show();
                }

                return null;
            }

            @Override
            public CustomValidationResponse isValid(Household household) {

                if(!householdRegistrationService.IsComplete(household))
                    return new CustomValidationResponse(false, getString(R.string.incomplete_data_collected));

                if(!householdRegistrationService.HasExactlyOneHead(household))
                    return new CustomValidationResponse(false, getString(R.string.household_with_multiple_heads));

                if(!householdRegistrationService.IsAllBiometricCollected(household, finalIs_biometric_required, finalAge_threshold))
                    return new CustomValidationResponse(false, getString(R.string.required_biometric_not_collected));

                if(!householdRegistrationService.IsAllRequiredSurveysCollected(household, code))
                    return new CustomValidationResponse(false, getString(R.string.required_survey_not_collected));

                return new CustomValidationResponse(true, "");
            }

            @Override
            public void onEditClicked(Household item, int position) {

                householdRegistrationService.setUnsavedHousehold(item);

                updateHouseholdLauncher.launch(new Intent(RegistrationListPage.this, RegistrationDetailsPage.class));


            }

            @Override
            public void onDeleteClicked(Household item, int position) {

                AlertDialogUtils.showCommentDialog(RegistrationListPage.this, null, "A deletion reason is required and will be shared during sync.", "Please enter a comment", new CommentCallback() {
                    @Override
                    public void onComment(String text) {

                        try {

                            List<String> subjects =
                                householdRegistrationService.deleteHousehold(code, item.householdId, text);

                            for(String s: subjects)
                                flow.delete(s);

                            List<Household> data = householdRegistrationService.getAll(code);

                            adapter.setData(data);

                            Toast.makeText(RegistrationListPage.this,getText(R.string.household_data_deleted_success),Toast.LENGTH_SHORT).show();

                        } catch (HouseholdError e) {
                            Toast.makeText(RegistrationListPage.this, e.getMessage(),Toast.LENGTH_SHORT).show();
                        }

                    }
                });

            }

        }, R.drawable.ic_no_photo_24);

        lvResults.setAdapter(adapter);

    }

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();

    private void downloadRegistrationListAsync(String partitionCode) {

        IO.execute(() -> {

            try {

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                int tenant = secureStore.getTenantId();

                //secureStore.getEnumerator();

                Call<FlaggedData> call =  endpoints.downloadFlaggedData(tenant, partitionCode);

                Response<FlaggedData> response = call.execute();

                if(response.isSuccessful())
                {
                    FlaggedData result = response.body();

                    householdRegistrationService.saveDnld(code, result);

                    runOnUiThread(() -> {

                        Toast.makeText(RegistrationListPage.this, getString(R.string.data_download_success_review), Toast.LENGTH_SHORT).show();

                         adapter.setData(householdRegistrationService.getAll(code));

                    } );

                }
                else
                {
                    Toast.makeText(RegistrationListPage.this,getString(R.string.download_failed_invalid_code),Toast.LENGTH_SHORT).show();
                }


            }
            catch (Exception e)
            {
                Toast.makeText(RegistrationListPage.this, getString(R.string.error_connection_failed) ,Toast.LENGTH_SHORT).show();
            }

        });

    }

}