package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.text.InputType;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.DebouncedTextWatcher;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_Text_Updated extends LinearLayout implements ViewValidator {

    SurveyDataContext surveyDataContext;
    final TextView tvOrder, tvLabel;
    final LinearLayout llWrapper;
    final EditText etValue;
    final TextView tvRequired;
    final int questionId;

    public VH_Text_Updated(int questionId, Context context, SurveyDataContext surveyDataContext) {

        super(context);

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_text, this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        etValue = v.findViewById(R.id.etValue);
        tvRequired = v.findViewById(R.id.tvRequired);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    private void bind( )
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        etValue.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES);
        String existing = answers.get(dp.id);
        etValue.setText(existing instanceof String ? (String) existing : "");

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

                // Valid input: store raw string
                answers.put(dp.id, text);

                //show or hide here
                surveyDataContext.bubbleDown(dp.id, dp.order);
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
