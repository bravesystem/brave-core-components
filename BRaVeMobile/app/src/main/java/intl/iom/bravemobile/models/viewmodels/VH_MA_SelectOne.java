package intl.iom.bravemobile.models.viewmodels;

import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.constraintlayout.widget.ConstraintSet;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.textfield.MaterialAutoCompleteTextView;
import com.google.android.material.textfield.TextInputLayout;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_MA_SelectOne extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator {
    final TextView tvOrder,tvLabel, tvRequired;
    final TextInputLayout til;
    final LinearLayout llWrapper;
    final MaterialAutoCompleteTextView actv;

    // Keep a snapshot of options for value<->label mapping
    List<SelectItem> options = java.util.Collections.emptyList();
    ArrayAdapter<String> labelsAdapter;

    public VH_MA_SelectOne(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
        actv = v.findViewById(R.id.actv);
        til = v.findViewById(R.id.tilLookup);
    }

    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, CollectionUnit> questionTypes, Map<Integer, String> answers, CollectionUnitAdapter.OptionsProvider optionsProvider, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
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
        actv.setThreshold(0); // start filtering after 1 typed character

        // 4) Restore selection from saved answer (Integer value id)
        String existing = answers.get(dp.id);

        int idx = -1;

        if(!(existing==null || existing.isEmpty()))
            /*actv.setText("", false);
        else*/
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
            if (listener != null) listener.onAnswerChanged(dp, chosenValue.toString());
        });


        // 6) Optional: when user clears text, clear the answer
        actv.setOnFocusChangeListener((v, hasFocus) -> {



            if (!hasFocus) {

                String txt = actv.getText() == null ? "" : actv.getText().toString().trim();
                Integer val = SelectItem.valueForLabel(options, txt);
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
            else actv.showDropDown();

        });
    }

    /*private int indexOfValue(List<SelectItem> items, int value) {
        for (int i = 0; i < items.size(); i++)
            if (items.get(i).getValue() == value) return i;
        return -1;
    }
    private Integer valueForLabel(List<SelectItem> items, String label) {
        if (label == null) return null;
        for (SelectItem si : items) if (label.equals(si.getLabel())) return si.getValue();
        return null;
    }*/

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

        // 2. Clear the constraints tied to llWrapper
        /*ConstraintLayout parent = (ConstraintLayout) this.itemView;

        ConstraintSet set = new ConstraintSet();
        set.clone(parent);

        set.clear(llWrapper.getId());  // <— collapse it fully

        set.applyTo(parent);*/

    }

    @Override
    public void show() {
        llWrapper.setVisibility(View.VISIBLE);

        // Restore constraints (optional but good practice)
        /*ConstraintSet set = new ConstraintSet();
        set.clone((ConstraintLayout) this.itemView);
        set.applyTo((ConstraintLayout) this.itemView);*/
    }

}
