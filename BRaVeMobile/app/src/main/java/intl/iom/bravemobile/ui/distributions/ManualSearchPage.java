package intl.iom.bravemobile.ui.distributions;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.EditTextUtils;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.CustomCallback;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.interfaces.EnrollmentCallback;
import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.registrations.Verification;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.verifications.VerificationListPage;

public class ManualSearchPage extends AppCompatActivity {

    @Override
    public void onBackPressed() {
        return;
    }

    private ActivityResultLauncher<Intent> showMatchLauncher;

    private SecureStore secureStore;

    WithLoading withLoading;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_manual_search_page);

        setTitle("Family/Individual lookup");

        DistributionService distributionService = ServiceLocator.distributionService(this);

        secureStore = ServiceLocator.secureStore(this);

        EditText etInputText = findViewById(R.id.etInputText);

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);

        DistDto dto = distributionService.getCurItems();

        showMatchLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {

                Toast.makeText(ManualSearchPage.this, "Beneficiary marked as received.", Toast.LENGTH_SHORT).show();

            }

        });

        findViewById(R.id.btnSearch).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                if(EditTextUtils.Empty(etInputText) ||
                        !DataCollectionUtils.validHouseholdId(secureStore.getHouseholdPrefix(),etInputText.getText().toString())
                )
                {
                    Toast.makeText(ManualSearchPage.this,
                            String.format("Enter a valid beneficiary ID.", secureStore.getHouseholdPrefix().substring(0, 2)),
                            Toast.LENGTH_SHORT).show();

                    Toast.makeText(ManualSearchPage.this,
                            String.format("It must be 10–11 characters and start with ‘%s’.", secureStore.getHouseholdPrefix().substring(0, 2)),
                            Toast.LENGTH_SHORT).show();
                }
                else
                {
                    String input = etInputText.getText().toString();

                    distributionService.checkEnrollment(dto.activityCode, dto.id, input, new EnrollmentCallback() {
                        @Override
                        public void onSuccess() {
                            Intent i = new Intent(ManualSearchPage.this, MatchFoundPage.class);
                            i.putExtra(IntentKeys.HOUSEHOLD_ID, input);
                            showMatchLauncher.launch(i);
                        }

                        @Override
                        public void onFailure(Throwable t) {

                            Toast.makeText(ManualSearchPage.this, t.getMessage(), Toast.LENGTH_SHORT).show();


                        }

                        @Override
                        public void onFailureCheckOnline(Throwable t) {


                            //Toast.makeText(ManualSearchPage.this, "Fetching from backend...", Toast.LENGTH_SHORT).show();

                            //distributionService.mockSaveEnrollment(dto.id, input);?????
                            withLoading.run("Fetching from backend…",cb -> {

                                distributionService.fetchEnrollmentData( dto.activityCode, dto.id ,input, new BasicCallback() {
                                    @Override
                                    public void onSuccess() {

                                        runOnUiThread(() ->{

                                            Toast.makeText(ManualSearchPage.this, "Downloading beneficiary data if it exists", Toast.LENGTH_SHORT).show();

                                        });

                                        Log.e("Distribution Enrollment", "Request sent to server.");
                                        cb.onSuccess(null);
                                    }

                                    @Override
                                    public void onFailure(Throwable t) {

                                        Log.e("Distribution Enrollment", "Request failed: " + t.getMessage());

                                        runOnUiThread(() ->
                                                Toast.makeText(ManualSearchPage.this, t.getMessage(), Toast.LENGTH_SHORT).show()
                                        );

                                        cb.onFailure(t);
                                    }
                                });

                            });



                        }
                    });


                    /*if(distributionService.checkEnrollment(dto.id, input))
                    {
                        Intent i = new Intent(ManualSearchPage.this, MatchFoundPage.class);
                        i.putExtra(IntentKeys.HOUSEHOLD_ID, input);
                        showMatchLauncher.launch(i);
                    }
                    else
                    {
                        Toast.makeText(ManualSearchPage.this, "Beneficiary not found or not enrolled in the distribution.", Toast.LENGTH_SHORT).show();
                    }*/

                }


            }
        });

        findViewById(R.id.btnScan).setEnabled(false);
        findViewById(R.id.btnScan).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                Toast.makeText(ManualSearchPage.this, "Scan Barcode/QR feature has not been implemented yet", Toast.LENGTH_SHORT).show();
            }
        });
    }
}