package intl.iom.bravemobile.models.viewmodels;

import android.app.DatePickerDialog;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

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
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

// DATE
public class VH_Date extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator{
    final TextView tvOrder,tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final EditText etDate;
    final Button btnClearDate;

    final SimpleDateFormat dateFmt = new SimpleDateFormat("yyyy-MM-dd", Locale.getDefault());

    public VH_Date(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        etDate = v.findViewById(R.id.etDate);
        btnClearDate = v.findViewById(R.id.btnClearDate);
    }
    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, CollectionUnit> questionList, Map<Integer, String> answers, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
    {
        viewHolders.put(dp.id, this);

        /*if (vhVisibilityById.get(dp.id))
        {
            show();
        } else {
            hide();
        }*/

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        isRequired = dp.isRequired;

        if(showOrder)
            tvOrder.setText(String.format("Q.%d",dp.order));
        else
            tvOrder.setVisibility(View.GONE);

        tvLabel.setText(dp.getDefaultText());
        //tvRequired.setVisibility(dp.isRequired ? View.VISIBLE : View.GONE);

        Object existing = answers.get(dp.id);
        if ( existing!=null && DateUtils.isValidIsoDate(existing.toString())) {
        //if (existing instanceof Date) {
            etDate.setText(existing.toString());
            //etDate.setText(dateFmt.format((Date) existing));
        } /*else if (existing instanceof Long) {
            etDate.setText(dateFmt.format(new Date((Long) existing)));
        }*/ else {
            etDate.setText("");
        }

        etDate.setFocusable(false);
        etDate.setOnClickListener(v -> showDatePicker(answers,listener,dp));



        btnClearDate.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                answers.put(dp.id, null);
                etDate.setText( "" );
            }
        });

    }

    private void showDatePicker(Map<Integer, String> answers, CollectionUnitAdapter.AnswerListener listener,CollectionUnit dp) {
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

        DatePickerDialog dlg = new DatePickerDialog(itemView.getContext(),
                (view, y, m, d) -> {
                    Calendar picked = Calendar.getInstance();
                    picked.set(y, m, d, 0, 0, 0);
                    Date val = picked.getTime();
                    String dt = DateUtils.toString(val);
                    answers.put(dp.id, dt);
                    etDate.setText( dt );
                    //etDate.setText(dateFmt.format(val));
                    if (listener != null) listener.onAnswerChanged(dp, val.toString());
                },
                c.get(Calendar.YEAR), c.get(Calendar.MONTH), c.get(Calendar.DAY_OF_MONTH));
        dlg.show();
    }

    boolean isRequired;

    @Override
    public void onVisibilityChanged(int id, boolean visible) {

    }

    private boolean override = true;

    @Override
    public void setValidation(String message) {
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);
        if (override) {
            tvRequired.setText(message);
        }
        // else ignore and keep existing message
    }

    @Override
    public void setValidation(String message, boolean override) {
        this.override = override;
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);

        if (!override) {
            // allowed to update the message
            tvRequired.setText(message);
        }
        // else: just lock, do not override
    }


    @Override
    public void clearValidation() {
        override = true;
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }

    @Override
    public void hide() {
        llWrapper.setVisibility(View.GONE);
    }

    @Override
    public void show() {
        llWrapper.setVisibility(View.VISIBLE);
    }
}
