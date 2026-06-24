package intl.iom.bravemobile.models.dashboard;

public final class SurveyCompletionItem {
    public final int surveyId;
    public final boolean required;
    public final int surveyType; // 1 = HH, 2 = IND (from activityService.getSurveyType)
    public final int collectedCount;
    public final int notCollectedCount;

    public SurveyCompletionItem(int surveyId, boolean required, int surveyType,
                                int collectedCount, int notCollectedCount) {
        this.surveyId = surveyId;
        this.required = required;
        this.surveyType = surveyType;
        this.collectedCount = collectedCount;
        this.notCollectedCount = notCollectedCount;
    }
}
