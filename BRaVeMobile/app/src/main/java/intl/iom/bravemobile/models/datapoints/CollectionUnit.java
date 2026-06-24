package intl.iom.bravemobile.models.datapoints;

import android.os.Build;

import java.util.Arrays;
import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.TranslationUtils;
import intl.iom.bravemobile.statics.AnswerType;

public class CollectionUnit
{
    public int id;
    public int order;
    public AnswerType answerType;
    public Optional<Integer> lookupId;
    public Optional<Integer> datasetId;
    public boolean isRequired;
    public Optional<Integer> minSelection;
    public Optional<Integer> maxSelection;
    public String skipLogic;
    public String restriction;
    public String resultEvaluation;
    public List<TranslatedText> translations;

    public CollectionUnit(int id, int order, AnswerType answerType, Integer lookupId, Integer datasetId, boolean isRequired, Integer minSelection, Integer maxSelection, String skipLogic, String restriction, String resultEvaluation, String text)
    {
        this( id,  order, answerType, lookupId, datasetId, isRequired, minSelection,  maxSelection,  skipLogic,  restriction,  resultEvaluation,
               // Arrays.asList(new TranslatedText("en",text, true)));
                Arrays.asList(new TranslatedText("en",text)));
    }


    public CollectionUnit(int id, int order, AnswerType answerType, Integer lookupId, Integer datasetId, boolean isRequired, Integer minSelection, Integer maxSelection, String skipLogic, String restriction, String resultEvaluation, List<TranslatedText> translations)
    {
        this.id = id;
        this.order = order;
        this.answerType = answerType;

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            this.lookupId = Optional.ofNullable(lookupId);
            this.datasetId = Optional.ofNullable(datasetId);
            this.minSelection = Optional.ofNullable(minSelection);
            this.maxSelection = Optional.ofNullable(maxSelection);
        }

        this.isRequired = isRequired;
        this.skipLogic = skipLogic;
        this.restriction = restriction;
        this.resultEvaluation = resultEvaluation;
        this.translations = translations;

    }

    public String getDefaultText()
    {
        return TranslationUtils.get(this.translations, "en");
    }
}
