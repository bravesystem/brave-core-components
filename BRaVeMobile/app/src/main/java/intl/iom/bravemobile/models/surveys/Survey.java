package intl.iom.bravemobile.models.surveys;

import com.google.gson.annotations.SerializedName;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.models.activities.QuestionModel;
import intl.iom.bravemobile.models.activities.SurveyModel;
import intl.iom.bravemobile.models.datapoints.SurveyQuestion;
import intl.iom.bravemobile.statics.DataPointType;

public class Survey
{
    public Survey(int code, String title, String description, DataPointType surveyType, boolean isRequired, List<SurveyQuestion> questionList)
    {
        this.code = code;
        this.title = title;
        this.description = description;
        this.surveyType = surveyType;
        this.isRequired = isRequired;
        this.questionList = questionList;
    }
    public Survey(Survey copy)
    {
        //this.code = ;
        this.title = copy.title;
        this.description = copy.description;
        this.surveyType = copy.surveyType;
        this.isRequired = copy.isRequired;
        this.questionList = copy.questionList;
    }

    public Survey(SurveyModel restored, boolean isRequired) {
        this.isRequired = isRequired;
        code = restored.surveyId;
        title = restored.title;
        description = restored.description;
        surveyType = DataPointType.fromCode(restored.type);

        List<SurveyQuestion> tmp = new ArrayList<>();

        for(QuestionModel q: restored.questions)
        {
            tmp.add(new SurveyQuestion(q));
        }

        questionList = tmp;

    }

    public int totalQuestions()
    {
        if(questionList==null)
            return 0;

        return questionList.size();
    }

    @SerializedName("surveyId")
    public int code;

    public int getCode()
    {
        return code;
    }

    public String title;
    public String description;
    public DataPointType surveyType;
    public boolean isRequired;
    public List<SurveyQuestion> questionList;
}
