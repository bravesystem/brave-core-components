package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.dashboard.BiometricPhotoComplianceSummary;
import intl.iom.bravemobile.models.dashboard.BiometricVerificationSummary;
import intl.iom.bravemobile.models.dashboard.CoreDashboardView;
import intl.iom.bravemobile.models.dashboard.KpiRowView;
import intl.iom.bravemobile.models.dashboard.SurveyCompletionItem;


/**
 * Adapter for the ListView in dashboard_kpi.xml.
 * Each list entry is one activity; the item view is activity_dashboard_first_two_mockups.xml
 * filled with that activity's dashboard data (FirstTwoMockupsView).
 * <p>
 * Usage:
 * <pre>
 *   ListView lv = findViewById(R.id.lvResults);
 *   DashboardKpiAdapter adapter = new DashboardKpiAdapter(context, R.layout.activity_dashboard_first_two_mockups);
 *   adapter.setItems(activityItems);  // or setActivityCodes + setDashboardService
 *   lv.setAdapter(adapter);
 * </pre>
 */
public class DashboardKpiAdapter extends BaseAdapter {

    private final Context context;
    private final int itemLayoutResId;
    private final LayoutInflater inflater;

    /** List of (activityCode, label, dashboard data). Data can be null to show loading/empty. */
    private List<ActivityDashboardItem> items = new ArrayList<>();

    public DashboardKpiAdapter(@NonNull Context context, int itemLayoutResId) {
        this.context = context.getApplicationContext();
        this.itemLayoutResId = itemLayoutResId;
        this.inflater = LayoutInflater.from(context);
    }

    /**
     * Set the list of activities and their dashboard data. Each entry = one activity.
     */
    public void setItems(@NonNull List<ActivityDashboardItem> items) {
        this.items = new ArrayList<>(items);
        notifyDataSetChanged();
    }

    /**
     * One list entry: activity code, optional label, and the dashboard data for the first two mockups.
     */
    public static final class ActivityDashboardItem {
        @NonNull public final String activityCode;
        @Nullable public final String activityLabel;
        @Nullable public final CoreDashboardView dashboardView;

        public ActivityDashboardItem(@NonNull String activityCode,
                                     @Nullable String activityLabel,
                                     @Nullable CoreDashboardView dashboardView) {
            this.activityCode = activityCode;
            this.activityLabel = activityLabel != null ? activityLabel : activityCode;
            this.dashboardView = dashboardView;
        }

        public ActivityDashboardItem(@NonNull String activityCode,
                                     @Nullable CoreDashboardView dashboardView) {
            this(activityCode, activityCode, dashboardView);
        }
    }

    @Override
    public int getCount() {
        return items.size();
    }

    @Override
    public ActivityDashboardItem getItem(int position) {
        return items.get(position);
    }

    @Override
    public long getItemId(int position) {
        return position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        View row = convertView;
        ViewHolder holder;
        if (row == null) {
            row = inflater.inflate(itemLayoutResId, parent, false);
            holder = new ViewHolder(row);
            row.setTag(holder);
        } else {
            holder = (ViewHolder) row.getTag();
        }

        ActivityDashboardItem item = getItem(position);
        holder.bind(item);
        return row;
    }

    private static final class ViewHolder {
        // KPI cards (included layouts: root is CardView, children have fixed ids)
        View cardHouseholds;
        View cardIndividuals;
        View cardSurveys;
        View cardAssistance;
        View cardErrors;

        TextView verificationCountCreate;
        TextView verificationCountProcessing;
        TextView verificationCountNoMatch;
        TextView verificationCountMatch;
        View verificationBarCreate;
        View verificationBarProcessing;
        View verificationBarNoMatch;
        View verificationBarMatch;

        LinearLayout surveyCompletionContainer;
        ProgressBar biometricProgress;
        TextView biometricSummary;
        ProgressBar photoProgress;
        TextView photoSummary;
        TextView kpiTitle;

        ViewHolder(View root) {

            kpiTitle = root.findViewById(R.id.kpi_title);
            cardHouseholds = root.findViewById(R.id.card_households);
            cardIndividuals = root.findViewById(R.id.card_individuals);
            cardSurveys = root.findViewById(R.id.card_surveys);
            cardAssistance = root.findViewById(R.id.card_assistance);
            cardErrors = root.findViewById(R.id.card_errors);

            verificationCountCreate = root.findViewById(R.id.verification_count_create);
            verificationCountProcessing = root.findViewById(R.id.verification_count_processing);
            verificationCountNoMatch = root.findViewById(R.id.verification_count_no_match);
            verificationCountMatch = root.findViewById(R.id.verification_count_match);
            verificationBarCreate = root.findViewById(R.id.verification_bar_create);
            verificationBarProcessing = root.findViewById(R.id.verification_bar_processing);
            verificationBarNoMatch = root.findViewById(R.id.verification_bar_no_match);
            verificationBarMatch = root.findViewById(R.id.verification_bar_match);

            surveyCompletionContainer = root.findViewById(R.id.survey_completion_container);
            biometricProgress = root.findViewById(R.id.biometric_progress);
            biometricSummary = root.findViewById(R.id.biometric_summary);
            photoProgress = root.findViewById(R.id.photo_progress);
            photoSummary = root.findViewById(R.id.photo_summary);
        }

        void bind(ActivityDashboardItem item) {
            CoreDashboardView data = item.dashboardView;
            if (data == null) {
                setEmptyState();
                return;
            }

            kpiTitle.setText(String.format("KPI Summary - Activity %s", item.activityCode));

            bindKpiCards(data.kpiRowView);
            bindBiometricVerification(data.biometricVerificationSummary);
            bindSurveyCompletion(data.surveyCompletionItems);
            bindBiometricPhotoCompliance(data.biometricPhotoComplianceSummary);
        }

        private void setEmptyState() {
            setKpiCard(cardHouseholds, "Households", "-", null);
            setKpiCard(cardIndividuals, "Individuals", "-", null);
            setKpiCard(cardSurveys, "Surveys", "-", null);
            setKpiCard(cardAssistance, "Assistance", "-", null);
            setKpiCard(cardErrors, "Errors", "-", null);
            if (verificationCountCreate != null) {
                verificationCountCreate.setText("-");
                verificationCountProcessing.setText("-");
                verificationCountNoMatch.setText("-");
                verificationCountMatch.setText("-");
            }
            if (biometricSummary != null) biometricSummary.setText("—");
            if (photoSummary != null) photoSummary.setText("—");
        }

        private void bindKpiCards(KpiRowView kpi) {
            setKpiCard(cardHouseholds, "Households", String.valueOf(kpi.householdCount), null);
            setKpiCard(cardIndividuals, "Individuals", String.valueOf(kpi.individualCount), null);
            setKpiCard(cardSurveys, "Surveys", kpi.requiredSurveysNotCollected + " not collected", null);
            setKpiCard(cardAssistance, "Assistance", "HH: " + kpi.assistanceHouseholdCount + ", Ind: " + kpi.assistanceIndividualCount, null);
            setKpiCard(cardErrors, "Errors", "—", null); // optional: total errors if you add to KpiRowView
        }

        private void setKpiCard(View cardRoot, String title, String primary, String secondary) {
            if (cardRoot == null) return;
            TextView titleView = cardRoot.findViewById(R.id.kpi_card_title);
            TextView primaryView = cardRoot.findViewById(R.id.kpi_card_primary);
            TextView secondaryView = cardRoot.findViewById(R.id.kpi_card_secondary);
            if (titleView != null) titleView.setText(title);
            if (primaryView != null) primaryView.setText(primary);
            if (secondaryView != null) {
                secondaryView.setText(secondary != null ? secondary : "");
                secondaryView.setVisibility(secondary != null && !secondary.isEmpty() ? View.VISIBLE : View.GONE);
            }
        }

        private void bindBiometricVerification(BiometricVerificationSummary v) {
            if (verificationCountCreate == null) return;
            verificationCountCreate.setText(String.valueOf(v.createCount));
            verificationCountProcessing.setText(String.valueOf(v.processingCount));
            verificationCountNoMatch.setText(String.valueOf(v.noMatchCount));
            verificationCountMatch.setText(String.valueOf(v.matchCount));

            int total = v.createCount + v.processingCount + v.noMatchCount + v.matchCount;
            if (total <= 0) total = 1;
            setLayoutWeight(verificationBarCreate, v.createCount, total);
            setLayoutWeight(verificationBarProcessing, v.processingCount, total);
            setLayoutWeight(verificationBarNoMatch, v.noMatchCount, total);
            setLayoutWeight(verificationBarMatch, v.matchCount, total);
        }

        private void setLayoutWeight(View view, int part, int total) {
            if (view == null || view.getLayoutParams() == null) return;
            ViewGroup.LayoutParams lp = view.getLayoutParams();
            if (lp instanceof LinearLayout.LayoutParams) {
                ((LinearLayout.LayoutParams) lp).weight = part;
                view.setLayoutParams(lp);
            }
        }

        private void bindSurveyCompletion(List<SurveyCompletionItem> list) {
            if (surveyCompletionContainer == null) return;
            surveyCompletionContainer.removeAllViews();
            if (list == null || list.isEmpty()) return;


            LayoutInflater inflater = LayoutInflater.from(surveyCompletionContainer.getContext());
            for (SurveyCompletionItem s : list) {
                View row = inflater.inflate(R.layout.item_dashboard_survey_row, surveyCompletionContainer, false);
                TextView label = row.findViewById(R.id.survey_item_label);
                ProgressBar progress = row.findViewById(R.id.survey_item_progress);
                TextView counts = row.findViewById(R.id.survey_item_counts);
                bindSurveyRow(label, progress, counts, s);
                surveyCompletionContainer.addView(row);
            }

            /*TextView label1 = template.findViewById(R.id.survey_item_label_1);
            ProgressBar progress1 = template.findViewById(R.id.survey_progress_1);
            TextView counts1 = template.findViewById(R.id.survey_item_counts_1);
            bindSurveyRow(label1, progress1, counts1, list.get(0));*/
            // If you have multiple surveys, add more rows programmatically or use a separate item layout
        }

        private void bindSurveyRow(TextView label, ProgressBar progress, TextView counts, SurveyCompletionItem s) {
            if (label != null) label.setText("Survey #" + s.surveyId + " (" + (s.surveyType == 1 ? "HH" : "Ind") + ")" + (s.required ? " *" : ""));
            int total = s.collectedCount + s.notCollectedCount;
            int pct = total > 0 ? (s.collectedCount * 100 / total) : 0;
            if (progress != null) {
                progress.setMax(100);
                progress.setProgress(pct);
            }
            if (counts != null) counts.setText(s.collectedCount + " collected, " + s.notCollectedCount + " not collected");
        }

        private void bindBiometricPhotoCompliance(BiometricPhotoComplianceSummary c) {
            int bioTotal = c.biometricAllCollected + c.biometricSomeMissing;
            int bioPct = bioTotal > 0 ? (c.biometricAllCollected * 100 / bioTotal) : 0;
            if (biometricProgress != null) {
                biometricProgress.setMax(100);
                biometricProgress.setProgress(bioPct);
            }
            if (biometricSummary != null) {
                biometricSummary.setText(bioPct + "% all collected, " + (100 - bioPct) + "% some missing");
            }
            int photoTotal = c.photoCollected + c.photoMissing;
            int photoPct = photoTotal > 0 ? (c.photoCollected * 100 / photoTotal) : 0;
            if (photoProgress != null) {
                photoProgress.setMax(100);
                photoProgress.setProgress(photoPct);
            }
            if (photoSummary != null) {
                photoSummary.setText(photoPct + "% collected, " + (100 - photoPct) + "% missing");
            }
        }
    }
}

