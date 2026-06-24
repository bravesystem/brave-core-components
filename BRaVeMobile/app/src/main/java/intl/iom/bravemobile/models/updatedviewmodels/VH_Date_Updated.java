package intl.iom.bravemobile.models.updatedviewmodels;

import android.app.DatePickerDialog;
import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

// DATE
public class VH_Date_Updated extends LinearLayout implements ViewValidator {
    final SurveyDataContext surveyDataContext;
    final TextView tvOrder,tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final EditText etDate;
    final Button btnClearDate;

    final SimpleDateFormat dateFmt = new SimpleDateFormat("yyyy-MM-dd", Locale.getDefault());

    final  int questionId;

    public VH_Date_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_date, this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        etDate = v.findViewById(R.id.etDate);
        btnClearDate = v.findViewById(R.id.btnClearDate);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    public void bind()
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;

        if(!surveyDataContext.answers.containsKey(dp.id))
            surveyDataContext.answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        Object existing = answers.get(dp.id);
        if ( existing!=null && DateUtils.isValidIsoDate(existing.toString())) {
            etDate.setText(existing.toString());
        }
        else {
            etDate.setText("");
        }

        etDate.setFocusable(false);
        etDate.setOnClickListener(v -> showDatePicker(answers, dp));

        btnClearDate.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                answers.put(dp.id, null);
                etDate.setText( "" );
            }
        });

    }

    private void showDatePicker(Map<Integer, String> answers,CollectionUnit dp) {
        final Calendar c = Calendar.getInstance();
        String existing = answers.get(dp.id);
        if (!StringUtils.isBlank(existing))
        {

            try {
                c.setTime(DateUtils.parse(existing));
            } catch (ParseException e) {
                throw new RuntimeException(e);
            }

        }

        DatePickerDialog dlg = new DatePickerDialog(getContext(),
                (view, y, m, d) -> {
                    Calendar picked = Calendar.getInstance();
                    picked.set(y, m, d, 0, 0, 0);
                    Date val = picked.getTime();
                    String dt = DateUtils.toString(val);
                    answers.put(dp.id, dt);
                    etDate.setText( dt );

                    //show or hide here
                    surveyDataContext.bubbleDown(dp.id, dp.order);

                },
                c.get(Calendar.YEAR), c.get(Calendar.MONTH), c.get(Calendar.DAY_OF_MONTH));
        dlg.show();
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
