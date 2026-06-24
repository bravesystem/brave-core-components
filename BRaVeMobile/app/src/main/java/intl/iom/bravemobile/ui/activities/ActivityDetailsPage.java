package intl.iom.bravemobile.ui.activities;

import androidx.appcompat.app.AppCompatActivity;
import androidx.preference.PreferenceManager;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.content.SharedPreferences;
import android.view.View;
import android.widget.TextView;

import android.os.Bundle;
import android.widget.LinearLayout;

import com.google.android.material.button.MaterialButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Arrays;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.Optional;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.ConsentAdapter;
import intl.iom.bravemobile.adapters.DataPointAdapter;
import intl.iom.bravemobile.adapters.PreferenceAdapter;
import intl.iom.bravemobile.adapters.SurveyAdapter;
import intl.iom.bravemobile.adapters.WhitelistEnumeratorAdapter;
import intl.iom.bravemobile.binders.SectionBinder;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.UnsupportedPreferenceType;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.CollectionUtils;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.activities.Consent;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DefinedPreferences;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.ui.distributions.DistributionListPage;
import intl.iom.bravemobile.ui.registrations.RegistrationListPage;
import intl.iom.bravemobile.ui.verifications.VerificationListPage;


public class ActivityDetailsPage extends AppCompatActivity
{

    private TextView tvCode, tvTitle, tvDescription,tvActivityPeriod, /*tvStartDate, tvEndDate,*/ tvDataPointsHeader, tvConsentsHeader,tvSurveysHeader, tvPreferencesHeader, tvWhitelistHeader, tvNoDataPoints, tvNoConsents, tvNoSurveys, tvNoPreferences, tvNoWhitelist;
    private LinearLayout endDateContainer;
    private RecyclerView rvDataPoints, rvConsents, rvSurveys, rvPreferences, rvWhitelist;

    SecureStore secureStore;

    private final SimpleDateFormat fmt = new SimpleDateFormat("yyyy-MM-dd HH:mm", Locale.getDefault());


    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_registration_details);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);

        String code = registrationActivityService.getCurrent();

        String title = String.format("Field Activity : %s",code.toUpperCase());

        setTitle(title);

        secureStore = ServiceLocator.secureStore(this);

        EnumeratorService enumeratorService = ServiceLocator.enumeratorService(this);

        SharedPreferences biometricPrefs =
                PreferenceManager.getDefaultSharedPreferences(getBaseContext());

        GetBiometricPreferences(registrationActivityService, biometricPrefs);

        try {
            Optional<RegistrationActivity> registrationActivity = registrationActivityService.getRegistrationActivity(code);

            if(registrationActivity.isPresent())
            {
                RegistrationActivity model =  registrationActivity.get();


                tvCode = findViewById(R.id.tvCode);
                tvTitle = findViewById(R.id.tvTitle);
                tvDescription = findViewById(R.id.tvDescription);
                tvActivityPeriod = findViewById(R.id.tvActivityPeriod);
               /* tvStartDate = findViewById(R.id.tvStartDate);
                tvEndDate = findViewById(R.id.tvEndDate);*/
                endDateContainer = findViewById(R.id.endDateContainer);

                MaterialButton btndistributions = findViewById(R.id.btndistributions);

                tvDataPointsHeader = findViewById(R.id.tvDataPointsHeader);
                tvConsentsHeader = findViewById(R.id.tvConsentsHeader);
                tvSurveysHeader = findViewById(R.id.tvSurveysHeader);
                tvPreferencesHeader = findViewById(R.id.tvPreferencesHeader);
                tvWhitelistHeader = findViewById(R.id.tvWhitelistHeader);

                tvNoDataPoints = findViewById(R.id.tvNoDataPoints);
                tvNoConsents = findViewById(R.id.tvNoConsents);
                tvNoSurveys = findViewById(R.id.tvNoSurveys);
                tvNoPreferences = findViewById(R.id.tvNoPreferences);
                tvNoWhitelist = findViewById(R.id.tvNoWhitelist);

                rvDataPoints = findViewById(R.id.rvDataPoints);
                rvConsents = findViewById(R.id.rvConsents);
                rvSurveys = findViewById(R.id.rvSurveys);
                rvPreferences = findViewById(R.id.rvPreferences);
                rvWhitelist = findViewById(R.id.rvWhitelist);

                rvDataPoints.setLayoutManager(new LinearLayoutManager(this));
                rvConsents.setLayoutManager(new LinearLayoutManager(this));
                rvSurveys.setLayoutManager(new LinearLayoutManager(this));
                rvPreferences.setLayoutManager(new LinearLayoutManager(this));

                rvWhitelist.setLayoutManager(new LinearLayoutManager(this));

                tvCode.setText(nz(model.code));
                tvTitle.setText(nz(model.title));
                tvDescription.setText(nz(model.description));

                DateFormat dateFmt = DateFormat.getDateInstance(DateFormat.MEDIUM, Locale.getDefault());

                tvActivityPeriod.setText(
                        DateUtils.formatDateRange(dateFmt, model.startDate, model.endDate)
                );
                //tvStartDate.setText(format(model.startDate));

                /*if (model.endDate.isPresent()) {
                    tvEndDate.setText(format(model.endDate.get()));
                    endDateContainer.setVisibility(View.VISIBLE);
                } else {
                    endDateContainer.setVisibility(View.GONE);
                }*/

                Optional<List<DataPoint>> dps = model.datapoints;
                Optional<List<Consent>> consents = model.consents;
                Optional<List<RegistrationPreference>> prefs = model.preferences;
                List<String> whitelist = model.whitelist;

                //to lower case elements of the list for better comparison
                CollectionUtils.toUpperCaseInPlace(whitelist);

                List<Survey> surveys = model.surveys;

                //Enumerators/Whitelist
                SectionBinder.bindSection(
                        whitelist,              // Optional<List<Consent>>
                        rvWhitelist,
                        tvWhitelistHeader,
                        tvNoWhitelist,
                        data -> new WhitelistEnumeratorAdapter(whitelist,enumeratorService.listAll())
                );

                // Preferences
                SectionBinder.bindSection(
                        prefs,                 // Optional<List<Preference>>
                        rvPreferences,
                        tvPreferencesHeader,
                        tvNoPreferences,
                        data -> new PreferenceAdapter(prefs.get())
                );

                //Consents
                SectionBinder.bindSection(
                        consents,              // Optional<List<Consent>>
                        rvConsents,
                        tvConsentsHeader,
                        tvNoConsents,
                        data -> new ConsentAdapter(consents.get())
                );

                //Datapoints
                SectionBinder.bindSection(
                        dps,              // Optional<List<Consent>>
                        rvDataPoints,
                        tvDataPointsHeader,
                        tvNoDataPoints,
                        data -> new DataPointAdapter(dps.get())
                );




                //Surveys
                SectionBinder.bindSection(
                        surveys,              // Optional<List<Consent>>
                        rvSurveys,
                        tvSurveysHeader,
                        tvNoSurveys,
                        data -> new SurveyAdapter(surveys)
                );


                if(!model.allowDistribution)
                    btndistributions.setVisibility(View.GONE);

                if(!model.allowVerification)
                    findViewById(R.id.btnVerifications).setVisibility(View.GONE);

                if(!model.allowRegistration)
                    findViewById(R.id.btnRegistrations).setVisibility(View.GONE);


                btndistributions.setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {

                        if(whitelist==null || whitelist.isEmpty()|| whitelist.contains(secureStore.getEnumerator().toUpperCase(Locale.ROOT)) )
                        {
                            Intent i = new Intent(ActivityDetailsPage.this, DistributionListPage.class);
                            startActivity(i);
                        }
                        else
                        {
                            AlertDialogUtils.showDialog(ActivityDetailsPage.this,  "Notice","The enumerator is not authorized or listed in the whitelist.", R.string.msg_ok);
                        }

                    }
                });

                findViewById(R.id.btnVerifications).setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {

                        if(whitelist==null || whitelist.isEmpty()|| whitelist.contains(secureStore.getEnumerator().toUpperCase(Locale.ROOT)) )
                        {
                            Intent i = new Intent(ActivityDetailsPage.this, VerificationListPage.class);
                            startActivity(i);
                        }
                        else
                        {
                            AlertDialogUtils.showDialog(ActivityDetailsPage.this,  "Notice","The enumerator is not authorized or listed in the whitelist.", R.string.msg_ok);
                        }



                    }
                });

                findViewById(R.id.btnRegistrations).setOnClickListener(new View.OnClickListener() {
                    @Override
                    public void onClick(View view) {

                        if(whitelist==null || whitelist.isEmpty()|| whitelist.contains(secureStore.getEnumerator().toUpperCase(Locale.ROOT)) )
                        {
                            Intent i = new Intent(ActivityDetailsPage.this, RegistrationListPage.class);
                            i.putExtra(Extras.EXTRA_ACTIVITY_CODE, code);
                            startActivity(i);
                        }
                        else
                        {
                            AlertDialogUtils.showDialog(ActivityDetailsPage.this,  "Notice","The enumerator is not authorized or listed in the whitelist.", R.string.msg_ok);
                        }


                    }
                });

            }

        }
        catch (RegistrationActivityNotFound e)
        {
            throw new RuntimeException(e);
        }
        catch (Exception e)
        {

        }


    }

    private void GetBiometricPreferences(RegistrationActivityService registrationActivityService, SharedPreferences prefs) {

        SharedPreferences.Editor editor = prefs.edit();

        try {

            editor.putBoolean("prefLEFT_LITTLE_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.LEFT_LITTLE,
                    false
            ));
            editor.putBoolean("prefLEFT_RING_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.LEFT_RING,
                    false
            ));
            editor.putBoolean("prefLEFT_MIDDLE_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.LEFT_MIDDLE,
                    false
            ));
            editor.putBoolean("prefLEFT_INDEX_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.LEFT_INDEX,
                    false
            ));
            editor.putBoolean("prefLEFT_THUMB", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.LEFT_THUMB,
                    true
            ));
            editor.putBoolean("prefRIGHT_LITTLE_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RIGHT_LITTLE,
                    false
            ));
            editor.putBoolean("prefRIGHT_RING_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RIGHT_RING,
                    false
            ));
            editor.putBoolean("prefRIGHT_MIDDLE_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RIGHT_MIDDLE,
                    false
            ));
            editor.putBoolean("prefRIGHT_INDEX_FINGER", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RIGHT_INDEX,
                    false
            ));
            editor.putBoolean("prefRIGHT_THUMB", (boolean) registrationActivityService.getPreference(
                    DefinedPreferences.RIGHT_THUMB,
                    true
            ));

            editor.commit();

        }
        catch (UnsupportedPreferenceType e)
        {

        }
        catch (Exception e)
        {

        }


    }

    private String nz(String s) { return s == null || s.trim().isEmpty() ? "—" : s; }
    private String format(Date d) { return d == null ? "—" : fmt.format(d); }

}