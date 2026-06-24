package intl.iom.bravemobile.ui.distributions;

import androidx.appcompat.app.AppCompatActivity;

import android.os.Bundle;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.DistributionAdapter;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.distributions.Kit;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.ui.verifications.VerificationListPage;

public class DistributionListPage extends AppCompatActivity {

    private ListView lvDistributions;
    private TextView emptyView;
    @Override
    public void onBackPressed() {
        return;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_distribution_list_page);

        lvDistributions = findViewById(R.id.lvResults);
        emptyView = findViewById(R.id.emptyView);
        lvDistributions.setEmptyView(emptyView);

        RegistrationActivityService registrationActivityService
                = ServiceLocator.registrationActivityService(this);

        String activityCode = registrationActivityService.getCurrent();

        //DistributionService distributionService = ServiceLocator.distributionService(this);

        setTitle("Distribution List");

        DistributionAdapter adapter = new DistributionAdapter(this, new DistributionAdapter.OnItemActionListener() {

            @Override
            public void onClick(Distribution d) {

                try {

                    if(!registrationActivityService.isActive(activityCode)){
                        AlertDialogUtils.showDialog(DistributionListPage.this, getString(R.string.activity_closed_lbl), getString(R.string.activity_closed_text), R.string.msg_ok);
                        return;
                    }

                } catch (RegistrationActivityNotFound e) {
                    throw new RuntimeException(e); //not happening
                }

                Toast.makeText(DistributionListPage.this, "The batch enrollment download feature has not been implemented yet", Toast.LENGTH_SHORT).show();
            }
        });

        lvDistributions.setAdapter(adapter);

        String code = registrationActivityService.getCurrent();

        List<Distribution> distributions = registrationActivityService.getDistributions(code);

        // Submit your data
        adapter.submitList(distributions);


    }

}