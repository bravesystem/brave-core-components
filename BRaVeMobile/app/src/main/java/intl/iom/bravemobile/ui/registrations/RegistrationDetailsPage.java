package intl.iom.bravemobile.ui.registrations;

import androidx.activity.OnBackPressedCallback;
import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.annotation.SuppressLint;
import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Build;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.MenuItem;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.adapters.LocationAddressAdapter;
import intl.iom.bravemobile.exceptions.HouseholdError;
import intl.iom.bravemobile.exceptions.LookupItemNotFound;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.FormValidator;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.helpers.MathUtils;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.services.GpsService;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.statics.KnownLkp;
import intl.iom.bravemobile.statics.ReservedLookups;
import intl.iom.bravemobile.ui.activities.ActivityDetailsPage;

public class RegistrationDetailsPage extends AppCompatActivity {

    private ActivityResultLauncher<Intent> individualListLauncher;
    private ActivityResultLauncher<Intent> surveyListLauncher;

    boolean collect_head_of_household_data_only_enabled = false;
    boolean data_ccollection_site_enabed = false;
    HouseholdRegistrationService householdRegistrationService;
    LookupService lookupService;

    TextView tvHeadName ;
    TextView tvHeadGender ;
    TextView tvHeadAge ;
    TextView tvErrorMessage ;
    ImageView imgPhoto;
    Spinner spHouseholdType;

    TextView tvHeadSectionLabel ;
    TextView etHouseholdId ;
    TextView etActivityCode ;
    EditText etRegistrationToken;
    EditText etHouseholdSize;
    TextView tvBiometricBadge;
    EditText etExtraAddress;
    LinearLayout headSummaryContainer;

    @Override
    public void onBackPressed() {
        return;
    }

    // If you prefer handling Up via menu instead of setNavigationOnClickListener:
    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == android.R.id.home) {
            setResult(RESULT_OK);
            finish();
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_registration_details_page);

        //setup all services
        lookupService = ServiceLocator.lookupService(this);
        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        AdmLocationService admLocationService =  ServiceLocator.admLocationService(this);
        householdRegistrationService  = ServiceLocator.householdRegistrationService(this);
        GpsService gpsService = ServiceLocator.gpsService(this);

        //get default preferences

        //initialize views
        tvHeadSectionLabel = findViewById(R.id.tvHeadSectionLabel);
        etHouseholdId = findViewById(R.id.etHouseholdId);
        TextView tvGpsResult = findViewById(R.id.tvGpsResult);
        etActivityCode = findViewById(R.id.etActivityCode);
        etRegistrationToken = findViewById(R.id.etRegistrationToken);
        etHouseholdSize= findViewById(R.id.etHouseholdSize);
        headSummaryContainer = findViewById(R.id.headSummaryContainer);
        tvHeadName = findViewById(R.id.tvHeadName);
        tvHeadGender = findViewById(R.id.tvHeadGender);
        tvHeadAge = findViewById(R.id.tvHeadAge);
        tvErrorMessage = findViewById(R.id.tvErrorMessage);
        imgPhoto = findViewById(R.id.imgPhoto);
        spHouseholdType = findViewById(R.id.spHouseholdType);
        tvBiometricBadge = findViewById(R.id.tvBiometricBadge);
        Button btnSave = findViewById(R.id.btnSave);
        Button btnGpsCollect = findViewById(R.id.btnGpsCollect);
        Button btnGpsClear = findViewById(R.id.btnGpsClear);

        LinearLayout locationAddressContainer = findViewById(R.id.locationAddressContainer);
        etExtraAddress = findViewById(R.id.etExtraAddress);


        FloatingActionButton btnListIndividuals = findViewById(R.id.fabListIndividuals);
        FloatingActionButton btnListSurveys = findViewById(R.id.fabListSurveys);

        //get all intent bundles or variables needed

        //set title, menus if needed
        setTitle(getString(R.string.edit_registration));

        Household household = householdRegistrationService.getUnsavedHousehold();

        List<Individual> individuals= new ArrayList<>();

        try {
            individuals = householdRegistrationService.getIndividuals(household.householdId);
        } catch (HouseholdError e) {
            throw new RuntimeException(e);
        }

        household.individuals = individuals;

        individualListLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {
               bind(household);
            }
        });

        surveyListLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {

            }

        });

        List<SelectItem> items = new ArrayList<>();
        items.add(new SelectItem(-1, "-- Select household type --"));

        Optional<List<SelectItem>> hh_types = ServiceLocator.lookupService(this).getLookupItemList(ReservedLookups.LKP_HOUSEHOLD_TYPES);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            if(hh_types.isPresent())
            {
                items.addAll(hh_types.get());
            }
        }

        ArrayAdapter<SelectItem> hhTypesAdaptor = new ArrayAdapter<>(this, android.R.layout.simple_spinner_item, items);
        hhTypesAdaptor.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spHouseholdType.setAdapter(hhTypesAdaptor);

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
                                        .filter(dp->dp.dataPointType== DataPointType.HOUSEHOLD)
                                        .collect(Collectors.toList())
                        );
                }
            }

        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }

        RecyclerView rv = findViewById(R.id.rvDataPoints);
        rv.setLayoutManager(new LinearLayoutManager(this));

        RecyclerView rvAddressFields = findViewById(R.id.rvAddressFields);
        rvAddressFields.setLayoutManager(new LinearLayoutManager(this));

        bind(household);

        try {
            collect_head_of_household_data_only_enabled = (boolean)registrationActivityService.getPreference(
                    DefinedPreferences.COLLECT_HEAD_OF_HOUSEHOLD_DATA_ONLY_ENABLED,
                    collect_head_of_household_data_only_enabled
            );

            data_ccollection_site_enabed = (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.DATA_COLLECTION_SITE_ENABLED,
                    data_ccollection_site_enabed
            );
        } catch (UnsupportedPreferenceType e) {
            throw new RuntimeException(e);
        } catch (RegistrationActivityNotFound e) {
            throw new RuntimeException(e);
        }

        // If isHeadOnlyDataCollection = true → editable; else read-only with value 1
        etHouseholdSize.setEnabled(collect_head_of_household_data_only_enabled);

        household.collect_head_only = collect_head_of_household_data_only_enabled;

        Map<Integer,String> answers = new HashMap<>();

        if(household.dpAnswers!=null)
            answers = household.dpAnswers;

        if(!StringUtils.isBlank(household.gps_coordinates))
        {
            tvGpsResult.setText(household.gps_coordinates);
        }

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
                //answers.put(dp.id, value);
            }
        }, answers, false);

        rv.setAdapter(adapter);

        adapter.submit(dataPoints);

        List<AdminLevel> adminLevels = new ArrayList<>();

        if(data_ccollection_site_enabed)
            adminLevels = admLocationService.getAdmLevels();

        LocationAddressAdapter admLevelAdapter = new LocationAddressAdapter(LayoutInflater.from(this), new LocationAddressAdapter.OptionsProvider() {
            @Override
            public List<SelectItem> getOptionsFor(String lookupName) {
                return null;
            }

            @Override
            public List<SelectItem> getOptionsFor(int level, Integer parent) {

                if(level>1 && parent==null)
                    return new ArrayList<>();

                return admLocationService.getLocations(parent);

            }
        }, new LocationAddressAdapter.AnswerListener() {
            @Override
            public void onAnswerChanged(AdminLevel level, String value) {

            }
        }, adminLevels, household.address.admLocations);

        if(data_ccollection_site_enabed)
        {
            rvAddressFields.setAdapter(admLevelAdapter);
        }


        //admLevelAdapter.submit(adminLevels);


        btnListIndividuals.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                householdRegistrationService.setUnsavedHousehold(household);

                individualListLauncher.launch(new Intent(RegistrationDetailsPage.this, MemberListPage.class));

            }
        });

        btnListSurveys.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                if(household.individualCount()>0) {

                    Intent i = new Intent(RegistrationDetailsPage.this, SurveyListPage.class);

                    i.putExtra(Extras.EXTRA_SURVEY_TYPE, DataPointType.HOUSEHOLD);

                    registrationActivityService.setSurveyTarget(
                            new SurveyTarget(household.activityCode, household.householdId, 0)
                    );

                    surveyListLauncher.launch(i);
                }
                else
                {
                    AlertDialogUtils.showDialog(RegistrationDetailsPage.this,  "Notice","Please add the head of household before proceeding to the survey section.", R.string.msg_ok);

                }

            }
        });

        btnGpsClear.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                household.gps_coordinates = "";
                tvGpsResult.setText(R.string.no_gps_collected);
            }
        });

        btnGpsCollect.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                String gps_coordinates = gpsService.getGpsCoordinates();

                if(!StringUtils.isBlank(gps_coordinates))
                {
                    household.gps_coordinates = gps_coordinates;
                    tvGpsResult.setText( gps_coordinates );
                }

            }
        });

        btnSave.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                adapter.clearAnyFocus(rv);
                household.dpAnswers = adapter.getAnswers();

                if(admLevelAdapter.validate()  && adapter.validate())
                {
                    if(data_ccollection_site_enabed)
                    {
                        admLevelAdapter.clearAnyFocus(rvAddressFields);

                        household.address.admLocations = admLevelAdapter.getLocations();
                    }

                    household.address.siteAddress = etExtraAddress.getText().toString();

                    save(household);
                }
                //else
                  //  Toast.makeText(RegistrationDetailsPage.this, "The entry could not be saved. Review the errors and try again.", Toast.LENGTH_SHORT).show();

            }
        });


    }

    private void save(Household household) {

        if(isValid(household))
        {
            //bind(household);
            collectInto(household);
            //SelectItem selectItem = (SelectItem) spGender.getSelectedItem();
            householdRegistrationService.updateHousehold(household);
            Toast.makeText(RegistrationDetailsPage.this, "Household data saved.",Toast.LENGTH_SHORT).show();
            //setResult(RESULT_OK);
            //finish();
        }
    }

    private boolean isValid(Household household)
    {
        boolean ok = FormValidator.all(
                () -> FormValidator.requireText(etHouseholdSize, "*"),
                () -> FormValidator.requireSelection(spHouseholdType, "*")
        );

        if (!ok) {
            // proceed with save
            tvErrorMessage.setText("Please verify all required fields.");
            return false;
        }
        return true;
    }

    private void collectInto(Household household)
    {
        household.registrationToken = EditTextUtils.text( etRegistrationToken );

        String size = EditTextUtils.text( etHouseholdSize );

        if("".equals(size))
            household.householdSize = 0;
        else
            household.householdSize = Integer.parseInt(size);

        household.householdType = SpinnerUtils.getSelected(spHouseholdType);

    }


    @SuppressLint("DefaultLocale")
    private void bind(Household household) {

        etHouseholdId.setText(household.householdId);
        etActivityCode.setText(household.activityCode);
        etRegistrationToken.setText(household.registrationToken);
        etHouseholdSize.setText(String.format(Locale.US,"%d",household.getHouseholdSize()));
        SpinnerUtils.selectByValue(spHouseholdType, household.householdType);

        if(household.address!=null && !StringUtils.isBlank(household.address.siteAddress))
            etExtraAddress.setText(household.address.siteAddress);

        Individual head = household.getHead();

        if(head == null)
        {
            tvHeadSectionLabel.setVisibility(View.GONE);
            headSummaryContainer.setVisibility(View.GONE);
            return;
        }

        int badgeColor =  head.isBiometricCollected() ? 0xFF2E7D32 /*green*/ : 0xFF9E9E9E /*gray*/;
        // Requires a shape background (bg_badge_round). Apply tint:
        if (tvBiometricBadge.getBackground() != null)
        {
            tvBiometricBadge.getBackground().setColorFilter(badgeColor, PorterDuff.Mode.SRC_IN);
        } else
        {
            tvBiometricBadge.setBackgroundColor(badgeColor);
        }


        tvHeadSectionLabel.setVisibility(View.VISIBLE);
        headSummaryContainer.setVisibility(View.VISIBLE);

        // Full name: first + middle + last (skip empties)
        String fullName = StringUtils.joinNonEmpty(" ",
                StringUtils.nz(head.firstName, null),
                StringUtils.nz(head.middleName, null),
                StringUtils.nz(head.lastName, null));

        tvHeadName.setText(String.format(getString(R.string.head_of_household_name),fullName));

        try {

            tvHeadGender.setText(
                    String.format(getString(R.string.head_of_household_gender),
                            lookupService.getLookupItemLabel(head.gender, ReservedLookups.LKP_GENDERS))
                    );
        } catch (LookupItemNotFound e) {
            throw new RuntimeException(e);
        }

        tvHeadAge.setText(

                String.format(getString(R.string.head_of_household_age),
                        MathUtils.buildAgeLabel(head.dob, head.ageInYears, head.ageInMonths, head.ageInDays))
                );

        if(!StringUtils.isBlank( head.photoBase64))
            imgPhoto.setImageBitmap(ImageHelper.decodeBase64(head.photoBase64, 70, 70));
    }
}