package intl.iom.bravemobile.ui.registrations;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.DatePickerHelper;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.FormValidator;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.CameraCaptureHelper;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.statics.ReservedLookups;

public class MemberCreatePage extends AppCompatActivity {

    private final String TAG = MemberCreatePage.class.getSimpleName();

    private CameraCaptureHelper cameraHelper;

    EditText etFirstName ;
    EditText etMiddleName ;
    EditText etLastName ;
    EditText etDob ;
    Button btnClearDob;
    EditText etAgeYears ;
    EditText etAgeMonths ;
    EditText etAgeDays ;
    Spinner spRelationship ;
    Spinner spGender ;
    ImageView imgPhoto ;
    CheckBox cbBiometricCollected ;
    Button btnCapturePhoto ;
    Button btnCaptureBiometric ;
    TextView tvErrorMessage;
    TextView tvBiometricBadge;
    Button btnSave ;

    ConsentsFeedback consentsFeedback;


    @Override
    public void onBackPressed() {
        return;
    }

    boolean biometric_collection_enabled = true;
    int head_of_household_min_age = 17;
    int head_of_household_max_age = 150;
    boolean picture_required = true;
    boolean re_verify_biometric_enabled = false;

    int age_threshold = 5;
    Household household;

    HouseholdRegistrationService householdRegistrationService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_member_create_page);

        consentsFeedback = (ConsentsFeedback) getIntent().getSerializableExtra(Extras.EXTRA_CONSENT_FLOW);

        //setup all services
        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        householdRegistrationService = ServiceLocator.householdRegistrationService(this);
        LookupService lookupService = ServiceLocator.lookupService(this);
        //Camera helper/service
        cameraHelper = new CameraCaptureHelper(this, this);

        //get default preferences
        GetActivityDefaultPreferences(registrationActivityService);

        //initialize views
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
        btnSave = findViewById(R.id.btnSave);
        //btnCancel = findViewById(R.id.btnCancel);

        tvBiometricBadge.getBackground().setColorFilter(0xFF9E9E9E, PorterDuff.Mode.SRC_IN);


        //get all intent bundles or variables needed
        String code = registrationActivityService.getCurrent();
        household = householdRegistrationService.getUnsavedHousehold();
        // Attach date picker, limit selection to today or earlier
        DatePickerHelper.attach(this, etDob, true);

        //set title, menus if needed
        setTitle(getString(R.string.add_individual));

        //get initial data & setup adapters and/or recycle views and/or listview etc..
        List<SelectItem> relationships = new ArrayList<>(), genders = new ArrayList<>();
        relationships.add(new SelectItem(-1, "-- relationship --"));
        genders.add(new SelectItem(-1, "-- gender --"));

        Optional<List<SelectItem>> _relationships = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_RELATIONSHIPS);
        Optional<List<SelectItem>> _genders = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_GENDERS);

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
        }

        ArrayAdapter<SelectItem> relationshipsAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, relationships);
        relationshipsAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spRelationship.setAdapter(relationshipsAdapter);

        ArrayAdapter<SelectItem> genderAdapter = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, genders);
        genderAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spGender.setAdapter(genderAdapter);


        //Show the datapoints if needed
        List<CollectionUnit> dataPoints = new ArrayList<>();

        try {
            Optional<RegistrationActivity> registrationActivity=
                    registrationActivityService.getRegistrationActivity(code);

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

        btnCaptureBiometric.setVisibility(View.GONE);

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
        }, false);
        rv.setAdapter(adapter);

        adapter.submit(dataPoints);

        Household household=
        householdRegistrationService.getUnsavedHousehold();

        Individual individual=
        householdRegistrationService.createIndividual();

        if(household.individuals.size()==0)
        {
            SpinnerUtils.selectByValue(spRelationship,0);
            spRelationship.setEnabled(false);
        }
        else if(household.collect_head_only )
        {
            setResult(RESULT_OK);
            finish();
        }

        btnSave.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                adapter.clearAnyFocus(rv);

                if(  adapter.validate() )
                {
                    individual.dpAnswers = adapter.getAnswers();
                    individual.feedback = consentsFeedback;

                    save(individual, household);
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
                        individual.photoBase64 =  r.base64;
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

        });



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

    private void save(Individual individual, Household household) {

        if(isValid(individual))
        {
            tvErrorMessage.setText("");

            collectInto(individual);

            try {
                householdRegistrationService.addIndividual(household, individual);
                household.individuals = householdRegistrationService.getIndividuals(household.householdId); //ok
                if(!household.collect_head_only) {
                    household.householdSize = household.individuals.size();
                    householdRegistrationService.updateHousehold(household);
                }
                //household.individuals.add(individual);
            } catch (HouseholdError e) {
                throw new RuntimeException(e);
            }

            Toast.makeText(MemberCreatePage.this, "Individual data saved.",Toast.LENGTH_SHORT).show();

            // int individualId = bundle.getInt(Extras.EXTRA_INDIVIDUAL_ID);
            Intent i= new Intent(MemberCreatePage.this, MemberDetailsPage.class);
            i.putExtra(Extras.EXTRA_INDIVIDUAL_ID, individual.individualId);
            i.addFlags(Intent.FLAG_ACTIVITY_FORWARD_RESULT);
            startActivity(i);

            ////
            //setResult(RESULT_OK);  //to remove in case of flag activity forward not working
            finish();
        }
    }

    public void collectInto(Individual individual)
    {
        individual.firstName = EditTextUtils.text(etFirstName);

        individual.middleName = EditTextUtils.text(etMiddleName);

        individual.lastName = EditTextUtils.text(etLastName);

        individual.relationship = SpinnerUtils.getSelected(spRelationship);

        /*if(individual.relationship ==0)
            household.setHead(individual);*/

        individual.gender = SpinnerUtils.getSelected(spGender);

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


    private boolean isValid(Individual individual)
    {
        int currentAge = -1;

        try {
            currentAge = DataCollectionUtils.computeAgeInYears(etDob, etAgeYears, etAgeMonths, etAgeYears);
        }
        catch (IllegalArgumentException e){

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
                () -> FormValidator.validateMinAge(finalCurrentAge, head_of_household_min_age, SpinnerUtils.getSelected(spRelationship), min_age_error, holder),
                () -> FormValidator.validateMaxAge( finalCurrentAge, head_of_household_max_age, getString(R.string.individual_max_age), holder)
        );

        if (!ok) {
            // proceed with save
            if(!holder.override)
                tvErrorMessage.setText("Please verify all required fields.");
            else
                tvErrorMessage.setText(holder.message);
            
            return false;
        }

        /*boolean isDateProvided =
                !etDob.getText().toString().trim().isEmpty() || isAnyAgeProvided();

        if(!isDateProvided)
        {
            tvErrorMessage.setText("Please provide either date of birth or an approximate age — this information is required.");
            return false;
        }

        boolean isDateValid =
                !etDob.getText().toString().trim().isEmpty() ^ isAnyAgeProvided();

        if(!isDateValid)
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




}