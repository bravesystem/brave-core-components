package intl.iom.bravemobile.models.activities;

import com.google.gson.annotations.SerializedName;

import java.util.List;

import intl.iom.bravemobile.models.datapoints.SurveyQuestion;
import intl.iom.bravemobile.statics.DataPointType;

public class SurveyModel {
    public int surveyId;

    public String title;
    public String description;
    public int type;
    public boolean isActive;
    public List<QuestionModel> questions;
}
