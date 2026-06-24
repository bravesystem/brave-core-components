package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.AdminLocationProvider;
import intl.iom.bravemobile.interfaces.OptionsProvider;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_SelectMultiple_Updated extends LinearLayout implements ViewValidator {
    final SurveyDataContext surveyDataContext;
    final TextView tvOrder, tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final LinearLayout container; // will add CheckBoxes dynamically
    final int questionId;
    final Context context;

    public VH_SelectMultiple_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.questionId = questionId;

        this.context = context;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_select_multiple,  this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
        container = v.findViewById(R.id.container);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    public void bind()
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;
        OptionsProvider optionsProvider = surveyDataContext.optionsProvider;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        List<SelectItem> options = optionsProvider != null && dp.lookupId.isPresent() ? optionsProvider.getOptionsFor(dp.lookupId.get()) : Collections.emptyList();

        // restore existing selections
        Set<Integer> selected = new HashSet<>();
        String existing = answers.get(dp.id);

        for (String o : getOptions(existing)) {
            if (o != null) selected.add(Integer.parseInt(o));
        }

        container.removeAllViews();

        for (SelectItem opt : options) {
            CheckBox cb = new CheckBox(context);
            cb.setText(opt.getLabel());
            cb.setChecked(selected.contains(opt.getValue()));
            cb.setOnCheckedChangeListener((buttonView, isChecked) -> {
                Set<Integer> cur = new HashSet<>();
                String ex = answers.get(dp.id);

                for (String o : getOptions(ex)) {
                    if (o != null) cur.add(Integer.parseInt(o));
                }

                if (isChecked) cur.add(opt.getValue());
                else cur.remove(opt.getValue());

                String selection = joinIntegers(cur);

                answers.put(dp.id, selection);

                //show or hide here
                surveyDataContext.bubbleDown(dp.id, dp.order);

            });
            container.addView(cb);
        }

    }

    private String[] getOptions(String existing) {
        if(existing==null || existing.trim().isEmpty())
            return new String[0];

        return existing.trim().split(",");
    }

    public static String joinIntegers(Set<Integer> set) {
        return set.stream()
                .map(String::valueOf)
                .collect(Collectors.joining(","));
    }


    @Override
    public void setValidation(String message) {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        tvRequired.setVisibility( dp.isRequired ? View.VISIBLE : View.GONE);
        tvRequired.setText(message);
    }

    @Override
    public void clearValidation() {
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }

}