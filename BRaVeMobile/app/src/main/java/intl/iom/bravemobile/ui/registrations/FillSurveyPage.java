package intl.iom.bravemobile.ui.registrations;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.os.Build;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.Button;
import android.widget.Toast;

import com.google.android.gms.common.util.CollectionUtils;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Optional;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.datapoints.SurveyQuestion;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.DataPointType;
import intl.iom.bravemobile.statics.Extras;

public class FillSurveyPage extends AppCompatActivity {

    @Override
    public void onBackPressed() {
        return;
    }

    private SurveyTarget surveyTarget;


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_fill_survey_page);

        Button btnSave = findViewById(R.id.btnSave);
        Button btnCancel = findViewById(R.id.btnCancel);

        RecyclerView rv = findViewById(R.id.rvQuestions);
        rv.setLayoutManager(new LinearLayoutManager(this));

        rv.setHasFixedSize(false);
        rv.setItemAnimator(null);

        String activity_code = (String) getIntent().getSerializableExtra(Extras.EXTRA_ACTIVITY_CODE);
        int survey_code = (Integer) getIntent().getSerializableExtra(Extras.EXTRA_SURVEY_CODE);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        LookupService lookupService = ServiceLocator.lookupService(this);
        AdmLocationService admLocationService =  ServiceLocator.admLocationService(this);

        HouseholdRegistrationService  householdRegistrationService = ServiceLocator.householdRegistrationService(this);

        surveyTarget = registrationActivityService.getSurveyTarget();

        Survey survey = registrationActivityService.getSurveyById(activity_code, survey_code);

        setTitle(String.format("Survey :%s", survey.title));

        List<CollectionUnit> questions = new ArrayList<>();

        questions.addAll(survey.questionList);

        Map<Integer, String> answers = householdRegistrationService.getSurveyAnswers(surveyTarget, survey_code);

        for (CollectionUnit q: questions)
        {
            if(!answers.containsKey(q.id))
                answers.put(q.id, null);
        }
        CollectionUnitAdapter adapter = new CollectionUnitAdapter(LayoutInflater.from(this), new CollectionUnitAdapter.DatasetColumnsProvider() {
            @Override
            public List<DatasetColumn> getDatasetColumnsFor(int datasetId) {
                return registrationActivityService.getDatasetColumns(datasetId);
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
        }, new CollectionUnitAdapter.AdminLocationProvider() {
            @Override
            public List<AdminLevel> getAdminLevelsUpTo(int Level)
            {
                List<AdminLevel> adminLevels = new ArrayList<>();

                for(AdminLevel a : admLocationService.getAdmLevels())
                {
                    if(a.id <= Level)
                        adminLevels.add(a);
                }
                return adminLevels;
            }

            @Override
            public List<SelectItem> getOptionsFor(Integer parent) {
                return admLocationService.getLocations(parent);
            }

        }, new CollectionUnitAdapter.AnswerListener() {
            @Override
            public void onAnswerChanged(CollectionUnit dp, String value) {

            }
        },answers, true);

        rv.setAdapter(adapter);

        adapter.submit(questions);

        /*adapter.registerAdapterDataObserver(new RecyclerView.AdapterDataObserver() {
            @Override
            public void onChanged() {
                super.onChanged();
                rv.post(() -> {
                    //run after binding is complete
                    adapter.validate();
                });
            }
        });*/

        btnSave.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                adapter.clearAnyFocus(rv);

                if(adapter.validate())
                {
                    //adapter.getAnswers()
                    try
                    {
                        householdRegistrationService.saveSurveyAnswers(surveyTarget, survey_code, adapter.getAnswers());
                        Toast.makeText(FillSurveyPage.this, "Survey data saved successfully.", Toast.LENGTH_SHORT).show();
                        setResult(RESULT_OK);
                        finish();
                    }
                    catch (Exception e)
                    {

                    }
                    //Toast.makeText(FillSurveyPage.this, "valid", Toast.LENGTH_SHORT).show();
                }

            }
        });

        btnCancel.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                AlertDialogUtils.confirmDialog(FillSurveyPage.this, "Unsaved Changes Warning", "If you proceed, any unsaved data will be lost. Are you sure you want to continue?", new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {
                        finish();
                    }
                });

            }
        });


    }
}