package intl.iom.bravemobile.ui.registrations;


import androidx.appcompat.app.AppCompatActivity;

import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.binders.SurveySectionBinder;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.DataCollectionUnitValidator;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.AdmLocationService;
import intl.iom.bravemobile.interfaces.AdminLocationProvider;
import intl.iom.bravemobile.interfaces.DatasetColumnsProvider;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.interfaces.OptionsProvider;
import intl.iom.bravemobile.interfaces.PreValidateAction;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.models.surveys.SurveyTarget;
import intl.iom.bravemobile.models.viewmodels.VH_AdminLocation;
import intl.iom.bravemobile.models.viewmodels.VH_Dataset;
import intl.iom.bravemobile.rules.RestrictionResult;
import intl.iom.bravemobile.rules.RuleEngineRevised;
import intl.iom.bravemobile.rules.SkipLogicResult;
import intl.iom.bravemobile.services.CameraCaptureHelper;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.AnswerType;
import intl.iom.bravemobile.statics.Extras;


public class FillSurveyPageUpdated extends AppCompatActivity {

    private final String TAG = FillSurveyPageUpdated.class.getSimpleName();
    @Override
    public void onBackPressed() {
        return;
    }

    private CameraCaptureHelper cameraHelper;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_fill_survey_page_updated);

        Button btnSave = findViewById(R.id.btnSave);
        Button btnCancel = findViewById(R.id.btnCancel);

        TextView  tvSurveyCode = findViewById(R.id.surveyCode);
        //TextView  tvSurveyTitle = findViewById(R.id.surveyTitle);

        cameraHelper = new CameraCaptureHelper(this, this);


        String activity_code = (String) getIntent().getSerializableExtra(Extras.EXTRA_ACTIVITY_CODE);
        int survey_code = (Integer) getIntent().getSerializableExtra(Extras.EXTRA_SURVEY_CODE);

        RegistrationActivityService registrationActivityService = ServiceLocator.registrationActivityService(this);
        LookupService lookupService = ServiceLocator.lookupService(this);
        AdmLocationService admLocationService =  ServiceLocator.admLocationService(this);

        HouseholdRegistrationService householdRegistrationService = ServiceLocator.householdRegistrationService(this);

        SurveyTarget surveyTarget = registrationActivityService.getSurveyTarget();

        Survey survey = registrationActivityService.getSurveyById(activity_code, survey_code);

        setTitle(String.format("Survey # %s", survey.title));

        if(surveyTarget.individual_id==0)
            tvSurveyCode.setText(String.format(getString(R.string.survey_reponse_household),surveyTarget.household_id));
        else
            tvSurveyCode.setText(String.format(getString(R.string.survey_response_individual), DataCollectionUtils.getIndividualId(surveyTarget.household_id, surveyTarget.individual_id)));

        //tvSurveyTitle.setText(String.format("Survey title: %s", survey.title));

       // List<CollectionUnit> questions = new ArrayList<>();

        // optional: sort by order
        ArrayList<CollectionUnit> questions = new ArrayList<>(survey.questionList);
        Collections.sort(questions, (a, b) -> Integer.compare(a.order, b.order));
        //questions.addAll(questions);

        Map<Integer, String> answers = householdRegistrationService.getSurveyAnswers(surveyTarget, survey_code);

        //questions.addAll(survey.questionList);

        SurveyDataContext surveyDataContext = new SurveyDataContext(cameraHelper, questions, answers, new OptionsProvider() {
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
        }, new AdminLocationProvider() {
            @Override
            public List<AdminLevel> getAdminLevelsUpTo(int Level) {
                List<AdminLevel> adminLevels = new ArrayList<>();

                for (AdminLevel a : admLocationService.getAdmLevels()) {
                    if (a.id <= Level)
                        adminLevels.add(a);
                }
                return adminLevels;
            }

            @Override
            public List<SelectItem> getOptionsFor(Integer parent) {
                return admLocationService.getLocations(parent);
            }
        }, new DatasetColumnsProvider() {
            @Override
            public List<DatasetColumn> getDatasetColumnsFor(int datasetId) {
                return registrationActivityService.getDatasetColumns(datasetId);
            }
        });

        //Map<Integer, View> itemViews = new HashMap<>();

        //get all views
        for (CollectionUnit q : questions) {

            try
            {

                SurveySectionBinder.bindSection(q, FillSurveyPageUpdated.this, surveyDataContext);

                /*itemViews.put(
                        q.id,
                        SurveySectionBinder.bindSection(q, FillSurveyPageUpdated.this, surveyDataContext)
                );*/

            } catch (Exception e) {
                throw new RuntimeException(e);
            }

        }

        //set initial cascading views
        for (CollectionUnit q : questions) {

            SkipLogicResult result = RuleEngineRevised.evaluate(q, surveyDataContext.questions, answers);

            //evaluate skip logic
            if(result.conditionTrue)
            {
                surveyDataContext.visible.put(q.id, true);
                //viewHolders.get(id).show();
            }
            else if(result.parentIds.size()>0)
            {
                //answers.put(id, null);
                surveyDataContext.visible.put(q.id, false);
                //viewHolders.get(id).hide();
            }
        }

        //show or hide
        updateInitialVisibility(surveyDataContext);


        btnSave.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                if(validate(surveyDataContext))
                {
                    //adapter.getAnswers()
                    try
                    {
                        householdRegistrationService.saveSurveyAnswers(surveyTarget, survey_code, answers);
                        Toast.makeText(FillSurveyPageUpdated.this, "Survey data saved successfully.", Toast.LENGTH_SHORT).show();
                        setResult(RESULT_OK);
                        finish();
                    }
                    catch (Exception e)
                    {

                    }
                }

            }
        });


        btnCancel.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {

                AlertDialogUtils.confirmDialog(FillSurveyPageUpdated.this, "Unsaved Changes Warning", "If you proceed, any unsaved data will be lost. Are you sure you want to continue?", new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {
                        finish();
                    }
                });

            }
        });


    }

    private void updateInitialVisibility (SurveyDataContext surveyDataContext) {

        Map<Integer, Boolean> visible = surveyDataContext.visible;
        Map<Integer, View> itemViews = surveyDataContext.itemViews;

        for (Integer key : visible.keySet())
        {
            if(visible.get(key))
                itemViews.get(key).setVisibility(View.VISIBLE);
            else
                itemViews.get(key).setVisibility(View.GONE);
        }

    }

    private boolean validate(SurveyDataContext surveyDataContext)
    {
        Map<Integer, String > answers = surveyDataContext.answers;
        Map<Integer, CollectionUnit > questions = surveyDataContext.questions;
        Map<Integer, View> itemViews = surveyDataContext.itemViews;

        List<Map.Entry<Integer, CollectionUnit>> entries =
                new ArrayList<>(questions.entrySet());

        Collections.sort(entries, new Comparator<Map.Entry<Integer, CollectionUnit>>() {
            @Override
            public int compare(Map.Entry<Integer, CollectionUnit> e1,
                               Map.Entry<Integer, CollectionUnit> e2) {
                int c = Integer.compare(e1.getValue().order, e2.getValue().order);
                if (c != 0) return c;
                // optional tie-breaker by id to get deterministic order
                return Integer.compare(e1.getKey(), e2.getKey());
            }
        });

        for (Map.Entry<Integer, CollectionUnit> e : entries) {

            CollectionUnit dp = e.getValue();

            RestrictionResult restrictionResult = RuleEngineRevised.validate(dp, answers.get(dp.id));

            if(!restrictionResult.isValid)
            {
                //show required text in view
                ((ViewValidator)itemViews.get(dp.id)).setValidation(restrictionResult.errorMessage);

                return false;
            }


            if(!answers.containsKey(dp.id))
            {
                //show required text in view
                ((ViewValidator)itemViews.get(dp.id)).setValidation("*");

                return false;
            }

            if(dp.answerType == AnswerType.ADMINLEVEL)
            {
                ((PreValidateAction)itemViews.get(dp.id)).submitAll();

                if(! ((PreValidateAction)itemViews.get(dp.id)).validate() )
                    return false;

                //continue;
            }

            if(dp.answerType == AnswerType.DATASET)
            {
                ((PreValidateAction)itemViews.get(dp.id)).submitAll();
            }

            if (!validateCollectionUnit(dp, itemViews, questions, answers))
            {
                //show required text in view
                ((ViewValidator)itemViews.get(dp.id)).setValidation("Required");

                return false;
            }

            try
            {
                //equal null when full page not scrolled.
                ((ViewValidator)itemViews.get(dp.id)).clearValidation();
            }
            catch (Exception ex)
            {
                Log.e(TAG, ex.getMessage());
            }

        }

        return true;
    }

    private boolean validateCollectionUnit(CollectionUnit dp, Map<Integer, View> itemViews, Map<Integer, CollectionUnit> questions, Map<Integer, String> answers)
    {

        //answer to question id = dp.id
        String answer = answers.get(dp.id);

        SkipLogicResult skipLogicResult = RuleEngineRevised.evaluate(dp, questions, answers);

        boolean result = skipLogicResult.conditionTrue;

        if (!result && !skipLogicResult.parentIds.isEmpty())
        {
            itemViews.get(dp.id).setVisibility(View.GONE);
            answers.put(dp.id, null);
        }
        else
        {
            itemViews.get(dp.id).setVisibility(View.VISIBLE);
        }


        if(StringUtils.isBlank( answer ) && dp.isRequired)
        {
            if( result )
                return false;

            if( StringUtils.isBlank(dp.skipLogic) )
                return false;
        }

        if(StringUtils.isBlank( answer ) && !dp.isRequired)
            return true;

        if(result && dp.answerType== AnswerType.SELECT_MULTIPLE && !selectMultipleValidate(dp, answer))
            return false;

        return true;

    }

    private static boolean selectMultipleValidate(CollectionUnit dp, String answer) {

        // restore existing selections
        Set<Integer> selected = new HashSet<>();

        for (String o : getOptions(answer)) {
            if (o != null) selected.add(Integer.parseInt(o));
        }

        int size = selected.size();

        // If neither min nor max is set, require at least one selection
        if (!dp.minSelection.isPresent() && !dp.maxSelection.isPresent()) {
            return size > 0;
        }

        // Boundaries with sensible defaults
        int min = dp.minSelection.orElse(0);
        int max = dp.maxSelection.orElse(Integer.MAX_VALUE);
        if (max < min) max = min; // guard against bad config

        if(size < min || size > max)
            return false;

        return true;
    }

    private static String[] getOptions(String existing) {
        if(existing==null || existing.trim().isEmpty())
            return new String[0];

        return existing.trim().split(",");
    }

}
