package intl.iom.bravemobile.ui.registrations;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.MenuItem;
import android.view.View;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.HouseholdAdapter;
import intl.iom.bravemobile.adapters.IndividualAdapter;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.IndividualError;
import intl.iom.bravemobile.exceptions.LookupItemNotFound;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.interfaces.CommentCallback;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.CustomValidationResponse;
import intl.iom.bravemobile.models.activities.Consent;
import intl.iom.bravemobile.models.activities.ConsentFlow;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.ConsentType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;

public class MemberListPage extends AppCompatActivity {

    private static String TAG = RegistrationListPage.class.getSimpleName();

    private ListView lvResults;
    private TextView emptyView;
    private IndividualAdapter adapter;

    private final BiometricFlow flow = new ClientBiometricFlow();

    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == android.R.id.home) {
            setResult(RESULT_OK);
            finish();
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    private ActivityResultLauncher<Intent> addIndividualLauncher;

    String code;
    boolean biometric_collection_enabled = true;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_member_list_page);

        LookupService lookupService = ServiceLocator.lookupService(this);
        HouseholdRegistrationService householdRegistrationService =  ServiceLocator.householdRegistrationService(this);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);

        flow.init(MemberListPage.this);

        lvResults = findViewById(R.id.lvResults);
        emptyView = findViewById(R.id.emptyView);
        lvResults.setEmptyView(emptyView);
        FloatingActionButton addNewRegistration = findViewById(R.id.fabNewRegistration);

        code = registrationActivityService.getCurrent();

        //householdRegistrationService.deleteTmpSubjects();
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


        Household household = householdRegistrationService.getUnsavedHousehold();

        setTitle(String.format(getString(R.string.household_id),household.householdId));

        List<Individual> individuals = household.individuals;

        if(household.collect_head_only && individuals.size()>0)
        {
            addNewRegistration.setVisibility(View.GONE);
        }

        List<Consent> consents = new ArrayList<>();

        try {
            Optional<RegistrationActivity> registrationActivity = ServiceLocator.registrationActivityService(this).getRegistrationActivity(household.activityCode);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                if(registrationActivity.isPresent())
                {
                    if(registrationActivity.get().consents.isPresent())
                        for(Consent c : registrationActivity.get().consents.get()){
                            if(c.type== ConsentType.INDIVIDUAL)
                                consents.add(c);

                        }
                }
            }

        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }

        List<Consent> finalConsents = consents;

        boolean finalIs_biometric_required = is_biometric_required;
        int finalAge_threshold = age_threshold;

        adapter = new IndividualAdapter(this,individuals, new IndividualAdapter.OnItemActionListener(){

            @Override
            public String getLookupItemLabel(int id, int domain) {
                String label = "-";
                try
                {
                    label = lookupService.getLookupItemLabel(id, domain);
                }
                catch (LookupItemNotFound ex)
                {
                    Toast.makeText(MemberListPage.this,ex.getMessage(),Toast.LENGTH_SHORT).show();
                }

                return label;
            }

            @Override
            public void onEditClicked(Individual item, int position) {

                householdRegistrationService.setUnsavedHousehold(household);

                Intent i = new Intent(MemberListPage.this, MemberDetailsPage.class);
                i.putExtra(Extras.EXTRA_INDIVIDUAL_ID,item.individualId);
                addIndividualLauncher.launch(i);

            }

            @Override
            public void onDeleteClicked(Individual item, int position) {

                AlertDialogUtils.showCommentDialog(MemberListPage.this, null, "A deletion reason is required and will be shared during sync.", "Please enter a comment", new CommentCallback() {
                    @Override
                    public void onComment(String text) {

                        try {
                            householdRegistrationService.deleteIndividual(code, item.householdId,item.individualId, text);

                            String subject = DataCollectionUtils.getIndividualId(item.householdId, item.individualId);

                            //delete from matcher
                            flow.delete(subject);

                            List<Individual> data = householdRegistrationService.getIndividuals(item.householdId);

                            adapter.setData(data);

                            Toast.makeText(MemberListPage.this,getText(R.string.individual_data_deleted_success),Toast.LENGTH_SHORT).show();

                        } catch (IndividualError e) {
                            Toast.makeText(MemberListPage.this, e.getMessage(),Toast.LENGTH_SHORT).show();
                        } catch (HouseholdError e) {
                            throw new RuntimeException(e);
                        }

                    }
                });

            }

            @Override
            public CustomValidationResponse isValid(Individual item) {

                if(!householdRegistrationService.IsBiometricCollected(item, finalIs_biometric_required, finalAge_threshold))
                    return new CustomValidationResponse(false, getString(R.string.individual_biometric_not_collected));

                if(!householdRegistrationService.IsAllRequiredSurveysCollected(item, code ))
                    return new CustomValidationResponse(false, getString(R.string.individual_survey_not_collected));

                return new CustomValidationResponse(true, "");

            }


        }, R.drawable.ic_no_photo_24);

        lvResults.setAdapter(adapter);

        addIndividualLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {

                try {

                    List<Individual> refresh = householdRegistrationService.getIndividuals(household.householdId);

                    adapter.setData(refresh);

                    if(household.collect_head_only && refresh.size()>0)
                    {
                        addNewRegistration.setVisibility(View.GONE);
                    }


                } catch (HouseholdError e) {
                    throw new RuntimeException(e);
                }

            }


        });


        addNewRegistration.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                //addIndividualLauncher.launch(new Intent(MemberListPage.this, MemberCreatePage.class));

                ConsentFlow.start(
                        MemberListPage.this, // Activity
                        finalConsents, // List<Consent>
                        new ConsentFlow.Listener() {
                            @Override public void onCompleted(LinkedHashMap<Integer, Boolean> answers) {
                                // all done (or stopped because a required = No)
                                // answers: consentId -> true/false
                                //startActivity(new Intent(RegistrationListPage.this, RegistrationCreatePage.class));
                                Intent i = new Intent(MemberListPage.this, MemberCreatePage.class);
                                ConsentsFeedback feedback = new ConsentsFeedback();
                                feedback.answers = answers==null?new LinkedHashMap<>():answers;
                                i.putExtra(Extras.EXTRA_CONSENT_FLOW,feedback);
                                addIndividualLauncher.launch(i);

                            }
                            @Override public void onStopped(Consent stoppingConsent, LinkedHashMap<Integer, Boolean> answers) {
                                // stopped because a REQUIRED consent got "No"

                                AlertDialogUtils.showCommentDialog(MemberListPage.this, null, new CommentCallback() {
                                    @Override
                                    public void onComment(String text) {

                                        ConsentsFeedback feedback = new ConsentsFeedback();
                                        feedback.answers = answers;
                                        feedback.comment = text;

                                        String data = ObjectSerializer.serialize(feedback);

                                        householdRegistrationService.saveConsent(household.activityCode, data);

                                        Toast.makeText(MemberListPage.this, "Consent feedback saved..", Toast.LENGTH_SHORT).show();
                                    }
                                });
                            }
                        }
                );
            }
        });


    }

}