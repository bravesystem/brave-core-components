package intl.iom.bravemobile.ui;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.graphics.Color;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.ShapeDrawable;
import android.graphics.drawable.shapes.OvalShape;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.ImageView;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;
import android.widget.Toolbar;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.UUID;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.DashboardKpiAdapter;
import intl.iom.bravemobile.helpers.AggregationBinder;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.interfaces.DashboardService;
import intl.iom.bravemobile.interfaces.DeviceConfigService;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.ActivityAggregationResult;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.dashboard.CoreDashboardView;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.ui.activities.ActivityListPage;
import intl.iom.bravemobile.ui.distributions.MatchFoundPage;
import intl.iom.bravemobile.ui.registrations.MemberDetailsPage;

public class Dashboard extends AppCompatActivity {

    @Override
    public void onBackPressed() {
        return;
    }
    private SecureStore secureStore;

    private Menu dashboardMenu;

    BiometricFlow flow = new ClientBiometricFlow();

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.dashboard_settings, menu); // shows icons in Action Bar
        dashboardMenu = menu;
        updateMenuAuthorization();
        return true;
    }

    private void updateMenuAuthorization() {
        if (dashboardMenu != null) {
            dashboardMenu.findItem(R.id.action_app_settings).setVisible(enumeratorService.isSupervisor());
            dashboardMenu.findItem(R.id.action_bio_activation).setVisible(enumeratorService.isSupervisor());
            dashboardMenu.findItem(R.id.action_bio_preference).setVisible(enumeratorService.isSupervisor());

        }
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {

        if (item.getItemId() == R.id.action_logout) {
            enumeratorService.signOut();
            if(!enumeratorService.isAuthenticated())
            {
                secureStore.logout();
                Toast.makeText(this, "Logging out..", Toast.LENGTH_SHORT).show();
                finish();
            }


            return true;
        }

        if (item.getItemId() == R.id.action_bio_preference) {

            if(enumeratorService.isSupervisor())
            {
                try {
                    Bundle extras = new Bundle();
                    startActivity(flow.createIntent(Dashboard.this, BiometricFlow.Operation.PREFERENCES, extras));
                } catch (Exception e) {
                    //throw new RuntimeException(e);
                    Toast.makeText(Dashboard.this, getText(R.string.unable_to_launch_biometric), Toast.LENGTH_SHORT).show();
                }
            }


            return true;
        }

        if (item.getItemId() == R.id.action_bio_activation) {

            if(enumeratorService.isSupervisor())
            {
                try {
                    Bundle extras = new Bundle();
                    startActivity(flow.createIntent(Dashboard.this, BiometricFlow.Operation.ACTIVATION, extras));
                } catch (Exception e) {
                    //throw new RuntimeException(e);
                    Toast.makeText(Dashboard.this, getText(R.string.unable_to_launch_biometric), Toast.LENGTH_SHORT).show();
                }
            }


            return true;
        }

        if(item.getItemId() == R.id.action_test_capture){

            if(enumeratorService.isSupervisor())
            {
                Bundle extras = new Bundle();
                //BiometricFlow.EXTRA_IS_VERIFY_ONLY

                extras.putString(BiometricFlow.EXTRA_INDIVIDUAL_ID, UUID.randomUUID().toString());
                extras.putBoolean(BiometricFlow.EXTRA_IS_VERIFY_ONLY, true);
                try {
                    startActivity(flow.createIntent(Dashboard.this, BiometricFlow.Operation.CAPTURE_BIOMETRIC, extras));
                } catch (Exception e) {
                    //throw new RuntimeException(e);
                    Toast.makeText(Dashboard.this, getText(R.string.unable_to_launch_biometric), Toast.LENGTH_SHORT).show();
                }

            }

        }
        return false;

    }

    private EnumeratorService enumeratorService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.dashboard_kpi);

        flow.init(Dashboard.this);

        enumeratorService = ServiceLocator.enumeratorService(this);

        secureStore = ServiceLocator.secureStore(this);

        DashboardService dashboardService = ServiceLocator.dashboardService(this);
        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);

        String deviceSig = secureStore.getHouseholdPrefix();

        setTitle(String.format(" Dashboard - (Device %s)",deviceSig));

        ListView lvResults = findViewById(R.id.lvResults);
        //TextView emptyView = findViewById(R.id.emptyView);
        ImageView emptyImg = findViewById(R.id.emptyImg);


        // Load your image into the ImageView
        emptyImg.setImageResource(R.drawable.empty_dashboard);


        DashboardKpiAdapter adapter = new DashboardKpiAdapter(
                this,
                R.layout.activity_dashboard_first_two_mockups
        );

        List<RegistrationActivity> activityList = registrationActivityService.getAll();

        List<DashboardKpiAdapter.ActivityDashboardItem> items = new ArrayList<>();

        for (RegistrationActivity activity : activityList)
        {
            CoreDashboardView data = dashboardService.getViewData(activity);
            items.add(new DashboardKpiAdapter.ActivityDashboardItem(activity.code, activity.title, data));
        }

        adapter.setItems(items);

        lvResults.setAdapter(adapter);

        lvResults.setEmptyView(emptyImg);


        //DO NOT UNCOMMENT
        //String[] subjects = flow.listIds();
        //for(String s: subjects)flow.delete(s);

        /*ActivityAggregationResult data = GetAggregationResult();
        AggregationBinder.bind(this, data);*/

        findViewById(R.id.fabOpenActivities).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                startActivity( new Intent(Dashboard.this, ActivityListPage.class));
            }
        });

    }

    private List<String> getActivityCodes() {

        return null;
    }

   /* private ActivityAggregationResult GetAggregationResult() {

        ActivityAggregationResult result = new ActivityAggregationResult();

        result.environment = "DEV";
        result.enumerator = "Chris Smith";
        result.latest_activity = "#A-1023 \"Community Survey\"";
        result.last_sync = new Date();
        result.total_activities= 2;
        result.summary_registrations = "10 HHs 22 Inds";
        result.summary_distributions = "22 Inds";
        result.summary_verifications = "30 Inds";

        return result;
    }*/
}