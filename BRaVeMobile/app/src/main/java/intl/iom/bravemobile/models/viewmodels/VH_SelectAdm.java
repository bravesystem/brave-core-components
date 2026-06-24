package intl.iom.bravemobile.models.viewmodels;

import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.textfield.MaterialAutoCompleteTextView;
import com.google.android.material.textfield.TextInputLayout;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.adapters.LocationAddressAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.LocationAddress;

public class VH_SelectAdm extends RecyclerView.ViewHolder implements LocationAddressAdapter.ConstraintValidator{

    final TextView tvLabel, tvRequired;
    final TextInputLayout til;
    final LinearLayout llWrapper;
    final MaterialAutoCompleteTextView actv;
    public VH_SelectAdm(@NonNull View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        actv = v.findViewById(R.id.actv);
        til = v.findViewById(R.id.tilLookup);
    }
    boolean isRequired;

    List<SelectItem> options = java.util.Collections.emptyList();
    ArrayAdapter<String> labelsAdapter;
    public void bind(Map<Integer, LocationAddressAdapter.ConstraintValidator> viewHolders, Map<Integer, Integer> admLocations, LocationAddressAdapter.OptionsProvider optionsProvider, LocationAddressAdapter.AnswerListener listener, AdminLevel level) {

        viewHolders.put(level.id, this);

        if(!admLocations.containsKey(level.id))
            admLocations.put(level.id, null);

        isRequired = level.isRequired;

        tvLabel.setText(level.name);

        // 0) selection from saved answer (Integer value id)
        Integer existing = admLocations.get(level.id);

        // 1) Load options using lookupname
        if(level.id > 1)
            options = optionsProvider.getOptionsFor(level.id, admLocations.get(level.id-1));
        else
            options = optionsProvider.getOptionsFor(level.id, null);


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
        int idx = -1;

        if(existing!=null)
            idx = SelectItem.indexOfValue(options, existing );

        if (idx >= 0) {
            // show the label in the text field
            actv.setText(options.get(idx).getLabel(), false);
        } else {
            actv.setText("", false);
            admLocations.put(level.id, null);
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

            if(admLocations.get(level.id)!=null && admLocations.get(level.id).equals(chosenValue))
                return;

            admLocations.put(level.id, chosenValue);

            List<SelectItem> newOptions = new ArrayList<>();

            if(admLocations.containsKey(level.id+1))
            {
                newOptions = optionsProvider.getOptionsFor(level.id+1, admLocations.get(level.id));

                viewHolders.get(level.id+1).updateOptions(viewHolders, admLocations,level.id+1, newOptions);
            }

        });


    }

    @Override
    public void updateOptions(Map<Integer, LocationAddressAdapter.ConstraintValidator> viewHolders, Map<Integer, Integer> selections, int level, List<SelectItem> newOptions) {
        // 1) update your SelectItem list (for later callbacks)
        this.options.clear();
        this.options.addAll(newOptions);

        // 2) rebuild labels for display
        List<String> newLabels = new ArrayList<>();
        for (SelectItem si : newOptions) newLabels.add(si.getLabel());

        // 3) refresh adapter data
        labelsAdapter.clear();
        labelsAdapter.addAll(newLabels);
        labelsAdapter.notifyDataSetChanged();

        // 4) enable/disable field
        actv.setEnabled(!newOptions.isEmpty());
        actv.setText("", false); // optional: clear current selection

        //cleanup
        if(selections.containsKey(level+1))
        {
            viewHolders.get(level+1).updateOptions(viewHolders,selections,level+1,new ArrayList<>());
        }

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

    }

    @Override
    public void show() {

    }
}
