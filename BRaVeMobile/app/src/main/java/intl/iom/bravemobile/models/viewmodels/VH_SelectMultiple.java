package intl.iom.bravemobile.models.viewmodels;

import android.view.View;
import android.widget.CheckBox;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.stream.Collectors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_SelectMultiple extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator{
    final TextView tvOrder, tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final LinearLayout container; // will add CheckBoxes dynamically

    public VH_SelectMultiple(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
        container = v.findViewById(R.id.container);
    }

    @SuppressWarnings("unchecked")
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

        List<SelectItem> options = Collections.emptyList();

        if(optionsProvider != null && !dp.lookupId.isEmpty())
        {
            options = optionsProvider.getOptionsFor(dp.lookupId.get());
        }

        // restore existing selections
        Set<Integer> selected = new HashSet<>();
        String existing = answers.get(dp.id);

        for (String o : getOptions(existing)) {
            if (o != null) selected.add(Integer.parseInt(o));
        }

        container.removeAllViews();

        for (SelectItem opt : options) {
            CheckBox cb = new CheckBox(itemView.getContext());
            cb.setText(opt.getLabel());
            cb.setChecked(selected.contains(opt.getValue()));
            cb.setOnCheckedChangeListener((buttonView, isChecked) -> {
                Set<Integer> cur = new HashSet<>();
                String ex = answers.get(dp.id);

                for (String o : getOptions(ex)) {
                    if (o != null) cur.add(Integer.parseInt(o));
                }

                if (isChecked) cur.add(opt.getValue());
                else cur.remove(opt.getValue());

                String selection = joinIntegers(cur);

                answers.put(dp.id, selection);
                if (listener != null) listener.onAnswerChanged(dp, selection);
            });
            container.addView(cb);
        }

    }

    private String[] getOptions(String existing) {
        if(existing==null || existing.trim().isEmpty())
            return new String[0];

        return existing.trim().split(",");
    }

    public static String joinIntegers(Set<Integer> set) {
        return set.stream()
                .map(String::valueOf)
                .collect(Collectors.joining(","));
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