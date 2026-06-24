package intl.iom.bravemobile.models.dashboard;

import java.util.List;

public class CoreDashboardView {

    public final KpiRowView kpiRowView;
    public final BiometricVerificationSummary biometricVerificationSummary;
    public final List<SurveyCompletionItem> surveyCompletionItems;
    public final BiometricPhotoComplianceSummary biometricPhotoComplianceSummary;

    public CoreDashboardView(KpiRowView kpiRowView,
                               BiometricVerificationSummary biometricVerificationSummary,
                               List<SurveyCompletionItem> surveyCompletionItems,
                               BiometricPhotoComplianceSummary biometricPhotoComplianceSummary) {
        this.kpiRowView = kpiRowView;
        this.biometricVerificationSummary = biometricVerificationSummary;
        this.surveyCompletionItems = surveyCompletionItems;
        this.biometricPhotoComplianceSummary = biometricPhotoComplianceSummary;
    }

}
