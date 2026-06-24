package intl.iom.bravemobile.models.viewmodels;

import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.AutoCompleteTextView;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.Collection;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.statics.AnswerType;

public class VH_SelectOne extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator{
    final TextView tvOrder, tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final AutoCompleteTextView actv;

    // Keep a snapshot of options for value<->label mapping
    List<SelectItem> options = java.util.Collections.emptyList();
    ArrayAdapter<String> labelsAdapter;

    public VH_SelectOne(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        actv = v.findViewById(R.id.actv);
    }

    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, AnswerType> questionTypes, Map<Integer, String> answers, CollectionUnitAdapter.OptionsProvider optionsProvider, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
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

        // 1) Load options using lookupname
        options = (optionsProvider != null && dp.lookupId.isPresent() )
                ? optionsProvider.getOptionsFor(dp.lookupId.get())
                : java.util.Collections.emptyList();

        // 2) Build label list for filtering
        List<String> labels = new ArrayList<>();
        for (SelectItem si : options) labels.add(si.getLabel());

        // 3) Set adapter (AutoCompleteTextView is filterable out of the box)
        labelsAdapter = new ArrayAdapter<>(itemView.getContext(),
                android.R.layout.simple_list_item_1, labels);
        actv.setAdapter(labelsAdapter);
        actv.setEnabled(!options.isEmpty());
        actv.setThreshold(1); // start filtering after 1 typed character

        // 4) Restore selection from saved answer (Integer value id)
        String existing = answers.get(dp.id);

        int idx = -1;

        if(existing==null || existing.isEmpty())
            actv.setText("", false);
        else
            idx = indexOfValue(options, Integer.parseInt( existing));

        if (idx >= 0) {
            // show the label in the text field
            actv.setText(options.get(idx).getLabel(), false);
        } else {
            actv.setText("", false);
        }

        // 5) Selection handling
        actv.setOnItemClickListener((parent, view, position, id) -> {
            // position is in the *filtered* adapter; map label back to value
            String chosenLabel = (String) parent.getItemAtPosition(position);
            Integer chosenValue = valueForLabel(options, chosenLabel);
            answers.put(dp.id, chosenValue.toString());
            if (listener != null) listener.onAnswerChanged(dp, chosenValue.toString());
        });

        // 6) Optional: when user clears text, clear the answer
        actv.setOnFocusChangeListener((v, hasFocus) -> {
            if (!hasFocus) {
                String txt = actv.getText() == null ? "" : actv.getText().toString().trim();
                Integer val = valueForLabel(options, txt);
                if (val == null && txt.isEmpty()) {
                    answers.remove(dp.id);
                    if (listener != null) listener.onAnswerChanged(dp, null);
                } else if (val != null) {
                    answers.put(dp.id, val.toString());
                    if (listener != null) listener.onAnswerChanged(dp, val.toString());
                } else {
                    // Text doesn’t match any option → reset to last valid or clear
                    actv.setText("", false);
                    answers.remove(dp.id);
                    if (listener != null) listener.onAnswerChanged(dp, null);
                }
            }

        });
    }

    private int indexOfValue(List<SelectItem> items, int value) {
        for (int i = 0; i < items.size(); i++)
            if (items.get(i).getValue() == value) return i;
        return -1;
    }
    private Integer valueForLabel(List<SelectItem> items, String label) {
        if (label == null) return null;
        for (SelectItem si : items) if (label.equals(si.getLabel())) return si.getValue();
        return null;
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

