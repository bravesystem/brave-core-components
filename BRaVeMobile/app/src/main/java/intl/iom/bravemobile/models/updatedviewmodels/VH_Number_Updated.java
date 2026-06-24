package intl.iom.bravemobile.models.updatedviewmodels;


import android.content.Context;
import android.text.InputFilter;
import android.text.InputType;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.DebouncedTextWatcher;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.statics.AnswerType;

// NUMBER (INT/NUMERIC)
public class VH_Number_Updated extends LinearLayout implements ViewValidator {

    private final String TAG = VH_Number_Updated.class.getSimpleName();
    final SurveyDataContext surveyDataContext;
    final TextView tvOrder,tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final EditText etValue;

    final  int questionId;

    public VH_Number_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_number, this, true);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        etValue = v.findViewById(R.id.etValue);
        tvRequired = v.findViewById(R.id.tvRequired);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    public void bind()
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());
        //tvRequired.setVisibility(dp.isRequired ? View.VISIBLE : View.GONE);

        etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_DECIMAL | InputType.TYPE_NUMBER_FLAG_SIGNED);

        // If strictly INT, you can restrict decimals:
        if (dp.answerType == AnswerType.INT) {
            etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_SIGNED);
            // optional: limit length
            etValue.setFilters(new InputFilter[]{ new InputFilter.LengthFilter(10) });
        }

        Object existing = answers.get(dp.id);
        etValue.setText(existing != null ? String.valueOf(existing) : "");

        etValue.addTextChangedListener(new DebouncedTextWatcher(600) {
            @Override
            public void onDebouncedTextChanged(CharSequence s) {
                String text = (s == null) ? "" : s.toString().trim();

                // No value -> clear answer
                if (text.isEmpty()) {
                    answers.put(dp.id, null);
                    return;
                }

                if(text.equals(answers.get(dp.id)))
                    return;

                try {
                    // Validate numeric input if needed
                    if (dp.answerType == AnswerType.INT)
                    {
                        Integer.parseInt(text);
                    }
                    else if (dp.answerType == AnswerType.NUMERIC)
                    {
                        Double.parseDouble(text);
                    }

                    // Valid input: store raw string
                    answers.put(dp.id, text);

                    //check constraint



                    //show or hide here
                    surveyDataContext.bubbleDown(dp.id, dp.order);


                }
                catch (NumberFormatException e) {
                    // Invalid -> clear answer
                    answers.put(dp.id, null);
                }



            }
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
