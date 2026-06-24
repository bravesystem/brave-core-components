package intl.iom.bravemobile.models.viewmodels;

import android.view.View;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_Boolean extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator{
    final CheckBox cb;
    final LinearLayout llWrapper;
    final TextView tvOrder, tvRequired;


    public VH_Boolean(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        cb = v.findViewById(R.id.cbValue);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
    }
    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, CollectionUnit> questionTypes, Map<Integer, String> answers, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
    {
        viewHolders.put(dp.id, this);

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        isRequired = dp.isRequired;

        if(showOrder)
            tvOrder.setText(String.format("Q.%d",dp.order));
        else
            tvOrder.setVisibility(View.GONE);

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
            if (listener != null) listener.onAnswerChanged(dp, isChecked?"true":"false");
        });
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
