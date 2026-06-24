package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.textfield.MaterialAutoCompleteTextView;
import com.google.android.material.textfield.TextInputLayout;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.AdminLocationProvider;
import intl.iom.bravemobile.interfaces.OptionsProvider;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_MA_SelectOne_Updated extends LinearLayout implements ViewValidator {
    final SurveyDataContext surveyDataContext;
    final TextView tvOrder,tvLabel, tvRequired;
    final TextInputLayout til;
    final LinearLayout llWrapper;
    final MaterialAutoCompleteTextView actv;

    // Keep a snapshot of options for value<->label mapping
    List<SelectItem> options = java.util.Collections.emptyList();
    ArrayAdapter<String> labelsAdapter;
    final private Context context;

    final  int questionId;

    public VH_MA_SelectOne_Updated(int questionId, Context context, SurveyDataContext surveyDataContext) {

        super(context);

        this.context = context;

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_select_one_material_auto_complete, this, true);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
        actv = v.findViewById(R.id.actv);
        til = v.findViewById(R.id.tilLookup);

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

        // 1) Load options using lookupname
        options = (optionsProvider != null && dp.lookupId.isPresent() )
                ? optionsProvider.getOptionsFor(dp.lookupId.get())
                : java.util.Collections.emptyList();

        // 2) Build label list for filtering
        List<String> labels = new ArrayList<>();
        for (SelectItem si : options) labels.add(si.getLabel());

        // 3) Set adapter (AutoCompleteTextView is filterable out of the box)
        labelsAdapter = new ArrayAdapter<>(context,
                android.R.layout.simple_list_item_1, labels);
        actv.setAdapter(labelsAdapter);
        actv.setEnabled(!options.isEmpty());
        actv.setThreshold(0); // start filtering after 1 typed character

        // 4) Restore selection from saved answer (Integer value id)
        String existing = answers.get(dp.id);

        int idx = -1;

        if(!(existing==null || existing.isEmpty()))
            idx = SelectItem.indexOfValue(options, Integer.parseInt( existing));

        if (idx >= 0) {
            // show the label in the text field
            actv.setText(options.get(idx).getLabel(), false);
        } else {
            actv.setText("", false);
        }

        til.setEndIconOnClickListener(v -> {
            // Focus the field, ensure no filter text, then show the full list
            actv.clearFocus();
            actv.showDropDown();

        });

        // 5) Selection handling
        actv.setOnItemClickListener((parent, view, position, id) -> {
            // position is in the *filtered* adapter; map label back to value
            String chosenLabel = (String) parent.getItemAtPosition(position);
            Integer chosenValue = SelectItem.valueForLabel(options, chosenLabel);
            answers.put(dp.id, chosenValue.toString());

            //show or hide here
            surveyDataContext.bubbleDown(dp.id, dp.order);

        });


        // 6) Optional: when user clears text, clear the answer
        actv.setOnFocusChangeListener((v, hasFocus) -> {

            if (!hasFocus) {

                String txt = actv.getText() == null ? "" : actv.getText().toString().trim();
                Integer val = SelectItem.valueForLabel(options, txt);
                if (val == null && txt.isEmpty()) {
                    answers.remove(dp.id);
                } else if (val != null) {
                    answers.put(dp.id, val.toString());
                } else {
                    // Text doesn’t match any option → reset to last valid or clear
                    actv.setText("", false);
                    answers.remove(dp.id);
                }
            }
            else actv.showDropDown();

        });
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
