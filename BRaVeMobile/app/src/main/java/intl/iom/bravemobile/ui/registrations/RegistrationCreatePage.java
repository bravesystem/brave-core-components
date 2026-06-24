package intl.iom.bravemobile.ui.registrations;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.adapters.LocationAddressAdapter;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.FormValidator;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.SpinnerUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.services.GpsService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.statics.KnownLkp;
import intl.iom.bravemobile.statics.ReservedLookups;

public class RegistrationCreatePage extends AppCompatActivity {

    @Override
    public void onBackPressed() {
        return;
    }

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

    EditText etRegistrationToken;
    TextView etHouseholdId;
    TextView etActivityCode;

    EditText etHouseholdSize;
    EditText etExtraAddress;

    ConsentsFeedback consentsFeedback;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_registration_create_page);

        //setup all services
        lookupService = ServiceLocator.lookupService(this);
        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        AdmLocationService admLocationService =  ServiceLocator.admLocationService(this);
        householdRegistrationService  = ServiceLocator.householdRegistrationService(this);
        SecureStore secureStore = ServiceLocator.secureStore(this);
        String hhPrefix = secureStore.getHouseholdPrefix();
        Household household =  householdRegistrationService.createEmptyHousehold(hhPrefix);
        GpsService gpsService = ServiceLocator.gpsService(this);

        //get default preferences

        //initialize views
        TextView tvHeadSectionLabel = findViewById(R.id.tvHeadSectionLabel);
        TextView tvGpsResult = findViewById(R.id.tvGpsResult);
        etHouseholdId = findViewById(R.id.etHouseholdId);

        etActivityCode = findViewById(R.id.etActivityCode);
        etRegistrationToken = findViewById(R.id.etRegistrationToken);
        LinearLayout headSummaryContainer = findViewById(R.id.headSummaryContainer);
        tvHeadName = findViewById(R.id.tvHeadName);
        tvHeadGender = findViewById(R.id.tvHeadGender);
        tvHeadAge = findViewById(R.id.tvHeadAge);
        tvErrorMessage = findViewById(R.id.tvErrorMessage);
        imgPhoto = findViewById(R.id.imgPhoto);
        spHouseholdType = findViewById(R.id.spHouseholdType);
        Button btnSave = findViewById(R.id.btnSave);
        Button btnGpsCollect = findViewById(R.id.btnGpsCollect);
        Button btnGpsClear = findViewById(R.id.btnGpsClear);

        LinearLayout locationAddressContainer = findViewById(R.id.locationAddressContainer);
        etExtraAddress = findViewById(R.id.etExtraAddress);

        //get all intent bundles or variables needed
        String code = registrationActivityService.getCurrent();

        consentsFeedback = (ConsentsFeedback) getIntent().getSerializableExtra(Extras.EXTRA_CONSENT_FLOW);

        //set title, menus if needed
        setTitle(getString(R.string.add_registration));

        //get initial data & setup adapters and/or recycle views and/or listview etc..
        household.activityCode = code;
        etHouseholdId.setText(household.householdId);
        etActivityCode.setText(household.activityCode);

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

        tvHeadSectionLabel.setVisibility(View.GONE);
        headSummaryContainer.setVisibility(View.GONE);


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

        etHouseholdSize= findViewById(R.id.etHouseholdSize);

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
        etHouseholdSize.setText("0");

        household.collect_head_only = collect_head_of_household_data_only_enabled;

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
        }, adminLevels);

        if(data_ccollection_site_enabed)
        {
            rvAddressFields.setAdapter(admLevelAdapter);
        }

        //admLevelAdapter.submit(adminLevels);

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
                household.dpAnswers=adapter.getAnswers();

                if(admLevelAdapter.validate()  && adapter.validate())
                {
                    if(data_ccollection_site_enabed)
                    {
                        admLevelAdapter.clearAnyFocus(rvAddressFields);
                        household.address.admLocations = admLevelAdapter.getLocations();
                    }

                    household.address.siteAddress = etExtraAddress.getText().toString();
                    household.feedback = consentsFeedback;

                    save(household);
                }
                //else
                 //   Toast.makeText(RegistrationCreatePage.this, "The entry could not be saved. Review the errors and try again.", Toast.LENGTH_SHORT).show();


            }
        });


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

    private void save(Household household) {

        if(isValid(household))
        {
            //bind(household);
            collectInto(household);
            //SelectItem selectItem = (SelectItem) spGender.getSelectedItem();
            householdRegistrationService.saveHousehold(household);
            householdRegistrationService.moveToNextHouseholdId();
            householdRegistrationService.setUnsavedHousehold(household);

            Toast.makeText(RegistrationCreatePage.this, "Household data saved.",Toast.LENGTH_SHORT).show();

            Intent i= new Intent(RegistrationCreatePage.this, RegistrationDetailsPage.class);
            i.addFlags(Intent.FLAG_ACTIVITY_FORWARD_RESULT);
            startActivity(i);

            finish();
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



}