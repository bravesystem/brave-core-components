package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.graphics.drawable.Drawable;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.surveys.Survey;

public class SurveyBaseAdapter extends BaseAdapter {


    public interface SurveyStatusChecker {
        boolean isSurveyFilled(int surveyId);
    }

    public enum DataPointType { INDIVIDUAL, HOUSEHOLD }

    private final LayoutInflater inflater;
    private final List<Survey> items = new ArrayList<>();

    private final SurveyStatusChecker statusChecker;

    public SurveyBaseAdapter(Context ctx, List<Survey> data, SurveyStatusChecker statusChecker) {
        this.inflater = LayoutInflater.from(ctx);
        this.statusChecker = statusChecker;
        if (data != null) items.addAll(data);

    }

    public void setItems(List<Survey> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }

    @Override public int getCount() { return items.size(); }
    @Override public Survey getItem(int position) { return items.get(position); }
    @Override public long getItemId(int position) {
        Survey s = items.get(position);
        String key = (s.title == null ? "" : s.title) + "|" + (s.surveyType == null ? "" : s.surveyType.name());
        return key.hashCode();
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        VH h;
        if (convertView == null) {
            convertView = inflater.inflate(R.layout.item_activity_survey, parent, false);
            h = new VH(convertView);
            convertView.setTag(h);
        } else {
            h = (VH) convertView.getTag();
        }

        Survey s = getItem(position);
        h.tvTitle.setText(ns(s.title));
        h.tvDescription.setText(ns(s.description));

        int q = Math.max(0, s.totalQuestions());
        h.tvQuestions.setText(q == 1 ? "1 question" : (q + " questions"));

        // Type badge
        h.tvTypeBadge.setVisibility(View.GONE);

        // Required badge
        if (s.isRequired) {
            //h.tvRequiredBadge.setVisibility(View.VISIBLE);
            tint(h.tvRequiredBadge.getBackground(), R.color.success_green);
        } else {
            h.tvRequiredBadge.setText("Optional");
            tint(h.tvRequiredBadge.getBackground(), R.color.info_teal);
        }

        h.imgSurveyStatus.setVisibility(View.VISIBLE);

        if(statusChecker.isSurveyFilled(s.code))
        {
            h.imgSurveyStatus.setImageResource(R.drawable.ic_survey_filled);
        }
        else
        {
            h.imgSurveyStatus.setImageResource(R.drawable.ic_survey_not_filled);
        }



        return convertView;
    }

    private static void tint(Drawable bg, int color) {
        if (bg != null) bg.setTint(color);
    }

    private static String ns(String s) { return s == null ? "" : s; }

    static class VH {
        final TextView tvTitle, tvDescription, tvQuestions, tvTypeBadge, tvRequiredBadge;
        final ImageView imgSurveyStatus;
        VH(View v) {
            tvTitle = v.findViewById(R.id.tvTitle);
            tvDescription = v.findViewById(R.id.tvDescription);
            tvQuestions = v.findViewById(R.id.tvQuestions);
            tvTypeBadge = v.findViewById(R.id.tvTypeBadge);
            tvRequiredBadge = v.findViewById(R.id.tvRequiredBadge);
            imgSurveyStatus = v.findViewById(R.id.imgSurveyStatus);
        }
    }
}
