package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_Boolean_Updated extends LinearLayout implements ViewValidator {

    final SurveyDataContext surveyDataContext;
    final CheckBox cb;
    final LinearLayout llWrapper;
    final TextView tvOrder, tvRequired;

    final  int questionId;
    public VH_Boolean_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_boolean, this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        cb = v.findViewById(R.id.cbValue);
        tvOrder = v.findViewById(R.id.tvOrder);
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

        cb.setText(dp.getDefaultText());
        //tvRequired.setVisibility(dp.isRequired ? View.VISIBLE : View.GONE);
        Object existing = answers.get(dp.id);
        cb.setOnCheckedChangeListener(null);

        boolean checked = false;
        try{
            checked = Boolean.parseBoolean(existing.toString());
        }
        catch (Exception e){

        }
        //existing instanceof Boolean && (Boolean) existing;
        cb.setChecked(checked);

        cb.setOnCheckedChangeListener((buttonView, isChecked) -> {
            answers.put(dp.id, isChecked?"true":"false");
            //show or hide here
            surveyDataContext.bubbleDown(dp.id, dp.order);
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
