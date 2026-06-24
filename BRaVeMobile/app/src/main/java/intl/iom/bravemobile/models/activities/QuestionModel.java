package intl.iom.bravemobile.models.activities;

import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.models.datapoints.TranslatedText;
import intl.iom.bravemobile.statics.AnswerType;

public class QuestionModel {
    public int id;
    public int order;
    public String defaultLanguage;
    public boolean isRequired;
    public int type;
    public Integer lookup;
    public Integer dataset;

    public Integer minVal;
    public Integer maxVal;
    public String restriction;
    public String skipLogic;

    public String resultExpression;
    public List<TranslatedText> texts;
}
