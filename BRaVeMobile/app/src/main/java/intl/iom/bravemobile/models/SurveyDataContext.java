package intl.iom.bravemobile.models;

import android.view.View;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.interfaces.AdminLocationProvider;
import intl.iom.bravemobile.interfaces.DatasetColumnsProvider;
import intl.iom.bravemobile.interfaces.OptionsProvider;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.rules.RuleEngineRevised;
import intl.iom.bravemobile.rules.SkipLogicResult;
import intl.iom.bravemobile.services.CameraCaptureHelper;

public class SurveyDataContext {

    public Map<Integer, String > answers = new HashMap<>();
    public Map<Integer, Boolean > visible = new HashMap<>();
    public Map<Integer, CollectionUnit> questions = new HashMap<>();
    public Map<Integer, View> itemViews = new HashMap<>();
    public List<CollectionUnit> ordered = new ArrayList<>();

    public OptionsProvider optionsProvider;
    public AdminLocationProvider adminLocationProvider;
    public DatasetColumnsProvider datasetColumnsProvider;

    public CameraCaptureHelper cameraHelper;

    public SurveyDataContext(CameraCaptureHelper cameraHelper, List<CollectionUnit> questions, Map<Integer, String> answers , OptionsProvider optionsProvider, AdminLocationProvider adminLocationProvider, DatasetColumnsProvider datasetColumnsProvider) {

        this.cameraHelper = cameraHelper;

        this.ordered = questions;

        for(CollectionUnit cu: questions)
        {
            this.questions.put(cu.id, cu);
            this.visible.put(cu.id, true);
        }


        this.answers = answers;

        this.optionsProvider = optionsProvider;
        this.adminLocationProvider = adminLocationProvider;
        this.datasetColumnsProvider = datasetColumnsProvider;
    }

    public void bubbleDown(int questionId, int order) {

        for(CollectionUnit cu: ordered){

            if(cu.order <= order)continue;

            SkipLogicResult result = RuleEngineRevised.evaluate(questions.get(cu.id), questions, answers);

            //evaluate skip logic
            if(result.conditionTrue)
            {
                visible.put(cu.id, true);
                itemViews.get(cu.id).setVisibility(View.VISIBLE);
            }
            else if(result.parentIds.size()>0)
            {
                //answers.put(id, null);
                visible.put(cu.id, false);
                itemViews.get(cu.id).setVisibility(View.GONE);
            }

        }
    }


}
