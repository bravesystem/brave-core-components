package intl.iom.bravemobile.models.datapoints;

import java.util.List;

import intl.iom.bravemobile.models.activities.QuestionModel;
import intl.iom.bravemobile.statics.AnswerType;

public class SurveyQuestion extends CollectionUnit
{
    public boolean isActive;
    public SurveyQuestion(int id, boolean isActive, int order, AnswerType answerType, Integer lookupId, Integer datasetId, boolean is_required, Integer minSelection, Integer maxSelection, String skipLogic, String restriction, String resultEvaluation, List<TranslatedText> translations) {
        super(id, order, answerType, lookupId, datasetId, is_required, minSelection, maxSelection, skipLogic, restriction, resultEvaluation, translations);
        this.isActive = isActive;
    }


    public SurveyQuestion(int id, boolean isActive, int order, AnswerType answerType, Integer lookupId, Integer datasetId, boolean is_required, Integer minSelection, Integer maxSelection, String skipLogic, String restriction, String resultEvaluation, String text) {
        super(id, order, answerType, lookupId, datasetId, is_required, minSelection, maxSelection, skipLogic, restriction, resultEvaluation, text);
        this.isActive = isActive;
    }

    public SurveyQuestion(QuestionModel restored) {
        super(restored.id, restored.order, AnswerType.fromCode(restored.type), restored.lookup, restored.dataset, restored.isRequired, restored.minVal, restored.maxVal, restored.skipLogic,  restored.restriction, restored.resultExpression, restored.texts);
        this.isActive = true; //will remove later
    }
}
