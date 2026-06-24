package intl.iom.bravemobile.ui.registrations;

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.graphics.PorterDuff;
import android.os.Bundle;
import android.view.View;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.RegistrationActivityAdapter;
import intl.iom.bravemobile.adapters.SurveyBaseAdapter;
import intl.iom.bravemobile.helpers.Bytes;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.Extras;

public class SurveyListPage extends AppCompatActivity {


    private SurveyBaseAdapter adapter;

    @Override
    public void onBackPressed() {
        return;
    }

    private ActivityResultLauncher<Intent> surveyLauncher;




    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_survey_list_page);

        TextView emptyView = findViewById(R.id.emptyView);
        ListView lvResults = findViewById(R.id.lvResults);
        FloatingActionButton fabExitSurveyList = findViewById(R.id.fabExitSurveyList);

        lvResults.setEmptyView(emptyView);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);

        HouseholdRegistrationService householdRegistrationService = ServiceLocator.householdRegistrationService(this);

        String code = registrationActivityService.getCurrent();

        setTitle(String.format("Survey List, Activity:%s", code));

        DataPointType surveyType = (DataPointType) getIntent().getSerializableExtra(Extras.EXTRA_SURVEY_TYPE);

        List<Survey> data = new ArrayList<>();

        List<Survey> tmp = registrationActivityService.getSurveys(code);

        SurveyTarget st = registrationActivityService.getSurveyTarget();

        String householdId = st.household_id;
        Integer individualId = st.individual_id;

        if(tmp!=null && !tmp.isEmpty())
        {
            for(Survey s: tmp)
            {
                if(s.surveyType==surveyType)
                    data.add(s);
            }
        }

        surveyLauncher = registerForActivityResult(new ActivityResultContracts.StartActivityForResult(), result -> {

            if (result.getResultCode() == RESULT_OK ) {

                List<Survey> _data = new ArrayList<>();

                for(Survey s: registrationActivityService.getSurveys(code)){
                    if(s.surveyType==surveyType)
                        _data.add(s);
                }

                adapter.setItems(_data);

            }

        });


        adapter = new SurveyBaseAdapter(this, data, new SurveyBaseAdapter.SurveyStatusChecker() {
            @Override
            public boolean isSurveyFilled( int surveyId ) {

                return householdRegistrationService.isSurveyCompleted(householdId,individualId, surveyId);
            }
        });
        lvResults.setAdapter(adapter);

        // Item click
        lvResults.setOnItemClickListener((parent, view, position, id) -> {
            Survey clicked = adapter.getItem(position);
            // handle click (open detail, start activity, etc.)
            //Toast.makeText(this, "Clicked: " + clicked.title, Toast.LENGTH_SHORT).show();
            Intent i = new Intent(SurveyListPage.this, FillSurveyPageUpdated.class);
            i.putExtra(Extras.EXTRA_ACTIVITY_CODE, code);
            i.putExtra(Extras.EXTRA_SURVEY_CODE, clicked.code);
            surveyLauncher.launch(i);
            //startActivity(i);
        });

        fabExitSurveyList.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                finish();
            }
        });


    }

}