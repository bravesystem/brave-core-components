package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;
import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.surveys.Survey;
import intl.iom.bravemobile.statics.DataPointType;

public class SurveyAdapter extends RecyclerView.Adapter<SurveyAdapter.VH> {

    private final List<Survey> items = new ArrayList<>();

    public SurveyAdapter( List<Survey> data) {
        items.addAll(data);
    }

    public void setItems(List<Survey> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }

    @Override public long getItemId(int position) {
        // stable-ish ID from title + type (adjust to your real unique key)
        Survey s = items.get(position);
        String key = (s.title == null ? "" : s.title) + "|" + (s.surveyType == null ? "" : s.surveyType.name());
        return key.hashCode();
    }

    @NonNull @Override
    public VH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_activity_survey, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VH h, int position) {
        Survey s = items.get(position);

        h.tvTitle.setText(s.title);
        h.tvDescription.setText(StringUtils.truncateWithDots(s.description));

        // Questions text
        int q = Math.max(0, s.totalQuestions());
        h.tvQuestions.setText(q == 1 ? "1 question" : (q + " questions"));

        // Type badge
        if (s.surveyType != null) {
            h.tvTypeBadge.setText(s.surveyType.name());
            // Optional tint by type
            int typeColor = (s.surveyType == DataPointType.INDIVIDUAL) ? 0xFF1976D2 : 0xFF00796B; // blue vs teal
            h.tvTypeBadge.getBackground().setTint(typeColor);
        } else {
            h.tvTypeBadge.setText("—");
            h.tvTypeBadge.getBackground().setTint(0xFF9E9E9E);
        }

        // Required badge visibility/tint
        if (s.isRequired) {
            //h.tvRequiredBadge.setVisibility(View.VISIBLE);
            h.tvRequiredBadge.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.tvRequiredBadge.getContext(), R.color.success_green)
            );
        } else {
            h.tvRequiredBadge.setText("Optional");
            h.tvRequiredBadge.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.tvRequiredBadge.getContext(), R.color.info_teal)
            );
        }

    }

    @Override public int getItemCount() { return items.size(); }

    static class VH extends RecyclerView.ViewHolder {
        final TextView tvTitle, tvDescription, tvQuestions, tvTypeBadge, tvRequiredBadge;
        VH(@NonNull View itemView) {
            super(itemView);
            tvTitle = itemView.findViewById(R.id.tvTitle);
            tvDescription = itemView.findViewById(R.id.tvDescription);
            tvQuestions = itemView.findViewById(R.id.tvQuestions);
            tvTypeBadge = itemView.findViewById(R.id.tvTypeBadge);
            tvRequiredBadge = itemView.findViewById(R.id.tvRequiredBadge);
        }
    }

}
