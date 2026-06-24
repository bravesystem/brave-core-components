package intl.iom.bravemobile.models.datapoints;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.models.activities.DatapointModel;
import intl.iom.bravemobile.statics.AnswerType;
import intl.iom.bravemobile.statics.DataPointType;

public class DataPoint extends CollectionUnit {
    public DataPointType dataPointType;
    public DataPoint(int id, DataPointType dataPointType, int order, AnswerType answerType, Integer lookupId, Integer datasetId, boolean is_required, Integer minSelection, Integer maxSelection, String skipLogic, String restriction, String resultEvaluation, List<TranslatedText> translations)
    {
        super(id, order, answerType, lookupId, datasetId, is_required, minSelection, maxSelection, skipLogic, restriction, resultEvaluation, translations);
        this.dataPointType = dataPointType;
    }

    public DataPoint(DatapointModel restored, int dp_type, boolean is_required)
    {
        super(restored.id, restored.order, AnswerType.fromCode(restored.type), restored.lookup, restored.dataset, is_required, restored.minVal, restored.maxVal, null,  restored.restriction, null, restored.texts);
        this.dataPointType=DataPointType.fromCode(dp_type);
    }
}
