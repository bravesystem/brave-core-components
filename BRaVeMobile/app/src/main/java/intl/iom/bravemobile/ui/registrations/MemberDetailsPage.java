package intl.iom.bravemobile.ui.registrations;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.MenuItem;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.biometrics.NBiometricStatus;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.ListIterator;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.IndividualError;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.DatePickerHelper;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.FormValidator;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.registrations.BiometricNotCollected;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.services.CameraCaptureHelper;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.statics.KnownLkp;
import intl.iom.bravemobile.statics.ReservedLookups;

public class MemberDetailsPage extends AppCompatActivity {

    private final String TAG = MemberDetailsPage.class.getSimpleName();

    private ActivityResultLauncher<Intent> surveyListLauncher;
    private CameraCaptureHelper cameraHelper;
    EditText etIndividualId ;
    EditText etFirstName ;
    EditText etMiddleName ;
    EditText etLastName ;
    EditText etDob ;
    EditText etNoBioReasonOther ;
    Button btnClearDob;
    EditText etAgeYears ;
    EditText etAgeMonths ;
    EditText etAgeDays ;
    Spinner spRelationship ;
    Spinner spGender ;
    Spinner spNoBioReason ;
    ImageView imgPhoto ;
    CheckBox cbBiometricCollected ;
    Button btnCapturePhoto ;
    Button btnCaptureBiometric ;
    TextView tvErrorMessage;
    TextView tvBiometricBadge;
    TextView tvNotSavedWarning;
    Button btnSave ;

    boolean collect_biometric_enabled = false;
    boolean biometric_collection_enabled = true;
    int head_of_household_min_age = 17;
    int head_of_household_max_age = 200;
    boolean picture_required = true;
    boolean re_verify_biometric_enabled = false;
    int age_threshold = 5;

    private final BiometricFlow flow = new ClientBiometricFlow();

    private  ActivityResultLauncher<Intent> biometricLauncher;


    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == android.R.id.home)
        {
            setResult(RESULT_OK);

            /*if( householdRegistrationService.deleteTmpSubject( DataCollectionUtils.getIndividualId(household.householdId,individual.individualId), flow) )
            {
                Toast.makeText(MemberDetailsPage.this,R.string.biometric_collected_not_saved,Toast.LENGTH_SHORT).show();
            }*/

            finish();
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    Household household;
    HouseholdRegistrationService householdRegistrationService;

    Individual individual = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_member_details_page);


        //setup all services
        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        householdRegistrationService = ServiceLocator.householdRegistrationService(this);
        LookupService lookupService = ServiceLocator.lookupService(this);
        //Camera helper/service
        cameraHelper = new CameraCaptureHelper(this, this);

        //get default preferences
        GetActivityDefaultPreferences(registrationActivityService);

        //initialize views

        etIndividualId = findViewById(R.id.etIndividualId);
        etFirstName = findViewById(R.id.etFirstName);
        etMiddleName = findViewById(R.id.etMiddleName);
        etLastName = findViewById(R.id.etLastName);
        etDob = findViewById(R.id.etDob);
        etAgeYears = findViewById(R.id.etAgeYears);
        etAgeMonths = findViewById(R.id.etAgeMonths);
        etAgeDays = findViewById(R.id.etAgeDays);
        spRelationship = findViewById(R.id.spRelationship);
        spGender = findViewById(R.id.spGender);
        imgPhoto = findViewById(R.id.imgPhoto);
        //cbBiometricCollected = findViewById(R.id.cbBiometricCollected);
        btnCapturePhoto = findViewById(R.id.btnCapturePhoto);
        btnCaptureBiometric = findViewById(R.id.btnCaptureBiometric);
        btnClearDob = findViewById(R.id.btnClearDob);
        tvErrorMessage = findViewById(R.id.tvErrorMessage);
        tvBiometricBadge = findViewById(R.id.tvBiometricBadge);
        tvNotSavedWarning = findViewById(R.id.tvNotSavedWarning);
        btnSave = findViewById(R.id.btnSave);

        spNoBioReason = findViewById(R.id.spNoBioReason);
        etNoBioReasonOther  = findViewById(R.id.etNoBioReasonOther);

        FloatingActionButton btnListSurveys = findViewById(R.id.fabListSurveys);

        Bundle bundle = getIntent().getExtras();

        int individualId = bundle.getInt(Extras.EXTRA_INDIVIDUAL_ID);
        household = householdRegistrationService.getUnsavedHousehold();

        flow.init(MemberDetailsPage.this);

        /*String[] subjects = flow.listIds();
        for(String s: subjects)
            flow.delete(s);*/

        //householdRegistrationService.deleteTmpSubjects(household.activityCode);



        try {
            individual =  householdRegistrationService.getIndividual(household.householdId, individualId);
        } catch (HouseholdError e) {
            throw new RuntimeException(e);
        } catch (IndividualError e) {
            throw new RuntimeException(e);
        }

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

                    Bundle payload = cr.payload;//.getString(NeurotecBiometricFlow.EXTRA_PERSON_ID);

                    //Toast.makeText(MemberDetailsPage.this, "2", Toast.LENGTH_SHORT).show();


                    if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_DELETE_IDENTIFIED_SUBJECT))
                    {
                        //Toast.makeText(MemberDetailsPage.this, "3", Toast.LENGTH_SHORT).show();

                        String subjectId = DataCollectionUtils.getIndividualId(household.householdId, individual.individualId);

                        flow.delete(subjectId);

                        householdRegistrationService.deleteTmpSubject( subjectId );

                        individual.biometricBase64 = null;

                        tvBiometricBadge.getBackground().setColorFilter(0xFF9E9E9E, PorterDuff.Mode.SRC_IN);

                        Toast.makeText(MemberDetailsPage.this, R.string.biometric_permanently_removed, Toast.LENGTH_SHORT).show();

                        tvNotSavedWarning.setVisibility(View.VISIBLE);

                        //save(individual, household);

                    }

                    if(payload!=null && payload.containsKey(BiometricFlow.EXTRA_RAW_DATA))
                    {

                        //Toast.makeText(MemberDetailsPage.this, "4", Toast.LENGTH_SHORT).show();

                        byte[] bytes = payload.getByteArray(BiometricFlow.EXTRA_RAW_DATA);

                        individual.biometricBase64 = Bytes.toBase64(bytes);

                        //save subject Id, in case we want to remove from the Matcher db
                        householdRegistrationService.saveTmpSubject(
                                household.activityCode,
                                DataCollectionUtils.getIndividualId(household.householdId, individual.individualId)
                        );

                        int badgeColor = 0xFF9E9E9E;

                        if(individual.isBiometricCollected())
                        {
                            Toast.makeText(MemberDetailsPage.this, getString(R.string.biometric_collection_success), Toast.LENGTH_SHORT).show();
                            badgeColor = 0xFF2E7D32;
                        }

                        tvBiometricBadge.getBackground().setColorFilter(badgeColor, PorterDuff.Mode.SRC_IN);

                        tvNotSavedWarning.setVisibility(View.VISIBLE);

                    }

                    break;
                case ACTIVATION:
                    // maybe no payload, just confirm success
                    Toast.makeText(MemberDetailsPage.this, "5", Toast.LENGTH_SHORT).show();


                    Bundle test1 = cr.payload;
                    break;
            }
        });

        etIndividualId.setText(DataCollectionUtils.getIndividualId(household.householdId, individualId));

        // Attach date picker, limit selection to today or earlier
        DatePickerHelper.attach(this, etDob, true);

        setTitle(getString(R.string.edit_individual));

        surveyListLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {

            }

        });

        //get initial data & setup adapters and/or recycle views and/or listview etc..
        List<SelectItem> relationships = new ArrayList<>(), genders = new ArrayList<>(), noBioReasons = new ArrayList<>();
        relationships.add(new SelectItem(-1, "-- relationship --"));
        genders.add(new SelectItem(-1, "-- gender --"));
        noBioReasons.add(new SelectItem(-1, "-- select reason --"));

        Optional<List<SelectItem>> _relationships = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_RELATIONSHIPS);
        Optional<List<SelectItem>> _genders = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_GENDERS);
        Optional<List<SelectItem>> _noBioReasons = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_BIOMETRIC_NOT_COLLECTED);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            if(_relationships.isPresent())
            {
                relationships.addAll(_relationships.get());
            }

            if(_genders.isPresent())
            {
                genders.addAll(_genders.get());
            }

            if(_noBioReasons.isPresent())
            {
                noBioReasons.addAll(_noBioReasons.get());
            }
        }

        ArrayAdapter<SelectItem> relationshipsAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, relationships);
        relationshipsAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spRelationship.setAdapter(relationshipsAdapter);

        ArrayAdapter<SelectItem> genderAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, genders);
        genderAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spGender.setAdapter(genderAdapter);

        ArrayAdapter<SelectItem> noBioAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, noBioReasons);
        noBioAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spNoBioReason.setAdapter(noBioAdapter);

        bind(individual);

        Individual finalIndividual = individual;

        //Show the datapoints if needed
        List<CollectionUnit> dataPoints = new ArrayList<>();

        try {
            Optional<RegistrationActivity> registrationActivity=
                    registrationActivityService.getRegistrationActivity(household.activityCode);

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                if(registrationActivity.isPresent())
                {
                    Optional<List<DataPoint>> dps =
                            registrationActivity.get().datapoints;

                    if(dps.isPresent() && dps.get().size()>0)
                        dataPoints.addAll(
                                dps.get().stream()
                                        .filter(dp->dp.dataPointType== DataPointType.INDIVIDUAL)
                                        .collect(Collectors.toList())
                        );
                }
            }

        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }

        RecyclerView rv = findViewById(R.id.rvDataPoints);
        rv.setLayoutManager(new LinearLayoutManager(this));

        Map<Integer,String> answers = new HashMap<>();

        if(individual.dpAnswers!=null)
            answers = individual.dpAnswers;


        /*if(!collect_biometric_enabled)
            btnCaptureBiometric.setVisibility(View.GONE);*/

        CollectionUnitAdapter adapter = new CollectionUnitAdapter(LayoutInflater.from(this), new CollectionUnitAdapter.DatasetColumnsProvider() {
            @Override
            public List<DatasetColumn> getDatasetColumnsFor(int datasetId) {
                return null;
            }
        }, new CollectionUnitAdapter.OptionsProvider() {
            @Override
            public List<SelectItem> getOptionsFor(String lookupName) {
                return null;
            }

            @Override
            public List<SelectItem> getOptionsFor(int lookupId) {
                Optional<List<SelectItem>> options = lookupService.getLookupItemList(lookupId);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                    if (options.isPresent()) {
                        return options.get();
                    }
                }
                return null;
            }
        }, new CollectionUnitAdapter.AnswerListener() {
            @Override
            public void onAnswerChanged(CollectionUnit dp, String value) {

            }
        }, answers, false);
        rv.setAdapter(adapter);

        adapter.submit(dataPoints);

        btnListSurveys.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                Intent i = new Intent(MemberDetailsPage.this, SurveyListPage.class);

                i.putExtra(Extras.EXTRA_SURVEY_TYPE, DataPointType.INDIVIDUAL );

                registrationActivityService.setSurveyTarget(
                        new SurveyTarget(household.activityCode, household.householdId, individualId)
                );

                surveyListLauncher.launch(i);

            }
        });
        btnSave.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                adapter.clearAnyFocus(rv);

                if( adapter.validate())
                {
                    finalIndividual.dpAnswers = adapter.getAnswers();
                    save(finalIndividual, household);
                }


            }
        });

        btnClearDob.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                etDob.setText("");
                individual.dob = null;
            }
        });


        btnCapturePhoto.setOnClickListener(v ->
                cameraHelper.capture(new CameraCaptureHelper.Listener() {
                    @Override public void onCaptured(CameraCaptureHelper.Result r) {
                        // preview
                        if (r.bitmap != null) imgPhoto.setImageBitmap(r.bitmap);
                        finalIndividual.photoBase64 =  r.base64;
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
                })
        );

        btnCaptureBiometric.setOnClickListener(v -> {

            try
            {
                Bundle extras = new Bundle();
                //BiometricFlow.EXTRA_IS_VERIFY_ONLY
                extras.putString(BiometricFlow.EXTRA_INDIVIDUAL_ID, DataCollectionUtils.getIndividualId(household.householdId, individual.individualId));
                extras.putBoolean(BiometricFlow.EXTRA_IS_VERIFY_ONLY, false);
                extras.putBoolean(BiometricFlow.EXTRA_IS_REGISTRATION_ONLY, true);

                if(individual.isBiometricCollected())
                    extras.putBoolean(BiometricFlow.EXTRA_ALLOW_DATA_UPDATE, true);

                biometricLauncher.launch(flow.createIntent(MemberDetailsPage.this, BiometricFlow.Operation.CAPTURE_BIOMETRIC, extras));

            }
            catch (ActivityNotFoundException e)
            {
                Toast.makeText(this,"Unable to start biometric capture.", Toast.LENGTH_LONG).show();
            }
            catch (Exception e){
                Toast.makeText(this,e.getMessage(), Toast.LENGTH_LONG).show();
            }

        });

    }


    private void save(Individual individual, Household household) {

        if(isValid(individual))
        {
            tvErrorMessage.setText("");

            collectInto(individual);

            try
            {
                householdRegistrationService.updateIndividual(household, individual);

                householdRegistrationService.deleteTmpSubject(
                        DataCollectionUtils.getIndividualId(household.householdId, individual.individualId)
                );

                household.individuals = householdRegistrationService.getIndividuals(household.householdId); //ok

                updateIndividualInHousehold(individual, household);

                Toast.makeText(MemberDetailsPage.this, "Individual data saved.",Toast.LENGTH_SHORT).show();

            } catch (HouseholdError e) {
                throw new RuntimeException(e);
            }
            //setResult(RESULT_OK);
            //finish();

            tvNotSavedWarning.setVisibility(View.GONE);
        }
    }

    private static void updateIndividualInHousehold(Individual individual, Household household) {
        ListIterator<Individual> it = household.individuals.listIterator();
        while (it.hasNext()) {
            if (it.next().individualId == individual.individualId) {
                it.set(individual);   // replaces in the same index
                return;
            }
        }
    }

    private boolean isValid(Individual individual)
    {

        int currentAge = -1;

        try {
             currentAge = DataCollectionUtils.computeAgeInYears(etDob, etAgeYears, etAgeMonths, etAgeYears);
        }catch (IllegalArgumentException e){

            Log.e(TAG, e.getMessage());
            tvErrorMessage.setText("Please provide either the date of birth or an approximate age — not both.");
            return false;

        }
        catch(IllegalStateException e)
        {
            Log.e(TAG, e.getMessage());
            tvErrorMessage.setText("Please provide either date of birth or an approximate age — this information is required.");
            return false;
        }

        String min_age_error = String.format(getString(R.string.individual_min_age),  head_of_household_min_age);

        FormValidator.ErrorMessageHolder holder = new FormValidator.ErrorMessageHolder();

        int finalCurrentAge = currentAge;

        boolean ok = FormValidator.all(
                () -> FormValidator.requireText(etFirstName, "*"),
                () -> FormValidator.requireText(etLastName, "*"),
                () -> FormValidator.requireSelection(spRelationship, "*"),
                () -> FormValidator.requireSelection(spGender, "*"),
                () -> FormValidator.requireSelection(spNoBioReason, "*"),
                () -> FormValidator.validateSelection(spNoBioReason, 1, biometric_collection_enabled, individual.isBiometricCollected(), "*"),
                () -> FormValidator.requireTextIfOther(SpinnerUtils.getSelected(spNoBioReason), 99, etNoBioReasonOther, "*"),
                () -> FormValidator.validateMinAge(finalCurrentAge, head_of_household_min_age, SpinnerUtils.getSelected(spRelationship), min_age_error, holder),
                () -> FormValidator.validateMaxAge( finalCurrentAge, head_of_household_max_age, getString(R.string.individual_max_age), holder)

        );

        //biometric_collection_enabled

        if (!ok) {
            // proceed with save
            if(!holder.override)
                tvErrorMessage.setText("Please verify all required fields.");
            else
                tvErrorMessage.setText(holder.message);

            return false;
        }


        /*if(biometric_collection_enabled && StringUtils.isBlank(individual.biometricBase64))
        {
            tvErrorMessage.setText("Biometric capture is required.");
            return false;
        }*/



        /*if(!isDateValid)
        {
            tvErrorMessage.setText("Please provide either the date of birth or an approximate age — not both.");
            return false;
        }*/

        if(picture_required && StringUtils.isBlank(individual.photoBase64))
        {
            tvErrorMessage.setText("Photo is mandatory.");
            return false;
        }

        return true;
    }

    public boolean isAnyAgeProvided()
    {
        return !etAgeYears.getText().toString().trim().isEmpty() ||
                !etAgeMonths.getText().toString().trim().isEmpty() ||
                !etAgeDays.getText().toString().trim().isEmpty();
    }

    private void GetActivityDefaultPreferences(RegistrationActivityService registrationActivityService) {
        try {
            biometric_collection_enabled = (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.BIOMETRIC_COLLECTION_ENABLED,
                    biometric_collection_enabled
            );

            re_verify_biometric_enabled = (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RE_VERIFY_BIOMETRIC_ENABLED,
                    re_verify_biometric_enabled
            );

            picture_required = (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.PICTURE_REQUIRED,
                    picture_required
            );

            head_of_household_min_age = (int) registrationActivityService.getPreference(
                    DefinedPreferences.HEAD_OF_HOUSEHOLD_MIN_AGE,
                    head_of_household_min_age
            );

            head_of_household_max_age = (int) registrationActivityService.getPreference(
                    DefinedPreferences.HEAD_OF_HOUSEHOLD_MAX_AGE,
                    head_of_household_max_age
            );

            collect_biometric_enabled = (boolean)registrationActivityService.getPreference(
                    DefinedPreferences.BIOMETRIC_COLLECTION_ENABLED,
                    collect_biometric_enabled
            );


            age_threshold =(int)registrationActivityService.getPreference(
                    DefinedPreferences.BIOMETRIC_AGE_THRESHOLD,
                    14
            );


        } catch (UnsupportedPreferenceType e) {
            throw new RuntimeException(e);
        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }
    }

    public void bind(Individual individual)
    {
        etFirstName.setText(individual.firstName);
        etMiddleName.setText(individual.middleName);
        etLastName.setText(individual.lastName);

        if(individual.dob!=null)
            etDob.setText( DateUtils.toString( individual.dob ) );

        if(individual.ageInYears!=null)
            etAgeYears.setText(individual.ageInYears.toString());

        if(individual.ageInMonths!=null)
            etAgeMonths.setText(individual.ageInMonths.toString());

        if(individual.ageInDays!=null)
            etAgeDays.setText(individual.ageInDays.toString());



        BiometricNotCollected biometricNotCollected =  new BiometricNotCollected();

        if(individual.biometricNotCollected!=null)
            biometricNotCollected =  individual.biometricNotCollected;

        SpinnerUtils.selectByValue(spNoBioReason, biometricNotCollected.selectedReason);
        etNoBioReasonOther.setText(biometricNotCollected.reasonIfOther);

        SpinnerUtils.selectByValue(spGender, individual.gender);
        SpinnerUtils.selectByValue(spRelationship, individual.relationship);
        ImageHelper.setBase64Image(imgPhoto, individual.photoBase64);

        int badgeColor = individual.isBiometricCollected() ? 0xFF2E7D32 /*green*/ : 0xFF9E9E9E /*gray*/;

        tvBiometricBadge.getBackground().setColorFilter(badgeColor, PorterDuff.Mode.SRC_IN);

    }

    public void collectInto(Individual individual)
    {
        individual.firstName = EditTextUtils.text(etFirstName);

        individual.middleName = EditTextUtils.text(etMiddleName);

        individual.lastName = EditTextUtils.text(etLastName);

        individual.relationship = SpinnerUtils.getSelected(spRelationship);

        individual.gender = SpinnerUtils.getSelected(spGender);

        individual.biometricNotCollected = new BiometricNotCollected(SpinnerUtils.getSelected(spNoBioReason), EditTextUtils.text(etNoBioReasonOther));

        individual.dob = DatePickerHelper.getDateFromEditText(etDob);

        individual.ageInYears = null;

        if(!StringUtils.isBlank(EditTextUtils.text(etAgeYears)))
        {
            individual.ageInYears = Integer.parseInt(EditTextUtils.text(etAgeYears));
        }

        individual.ageInMonths = null;

        if(!StringUtils.isBlank(EditTextUtils.text(etAgeMonths)))
        {
            individual.ageInMonths= Integer.parseInt(EditTextUtils.text(etAgeMonths));
        }

        individual.ageInDays = null;

        if(!StringUtils.isBlank(EditTextUtils.text(etAgeDays)))
        {
            individual.ageInDays = Integer.parseInt(EditTextUtils.text(etAgeDays));
        }

    }
}