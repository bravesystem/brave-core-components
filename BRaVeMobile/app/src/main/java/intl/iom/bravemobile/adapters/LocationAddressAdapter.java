package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.inputmethod.InputMethodManager;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.LocationAddress;
import intl.iom.bravemobile.models.viewmodels.VH_SelectAdm;

public class LocationAddressAdapter extends RecyclerView.Adapter< RecyclerView.ViewHolder >{

    public interface OptionsProvider {
        List<SelectItem> getOptionsFor(String lookupName);
        List<SelectItem> getOptionsFor(int lookupId, Integer integer);
    }

    public interface AnswerListener {
        void onAnswerChanged(AdminLevel level, String value);
    }

    public interface ConstraintValidator {

        void setValidation(String message);
        void setValidation(String message, boolean override);
        void clearValidation();
        void hide();
        void show();

        void updateOptions(Map<Integer, LocationAddressAdapter.ConstraintValidator> viewHolders, Map<Integer, Integer> selections, int level, List<SelectItem> newOptions);

    }

    private final LayoutInflater inflater;
    private final OptionsProvider optionsProvider;
    private final AnswerListener listener;
    private final List<AdminLevel> items = new ArrayList<>();

    //private final Map<Integer, Integer> answers = new HashMap<>();
    private final Map<Integer, Integer> admLocations = new HashMap<>();

    private final Map<Integer, ConstraintValidator> viewHolders = new HashMap<>(); // dp.id -> value


    public LocationAddressAdapter(LayoutInflater inflater, OptionsProvider optionsProvider, AnswerListener listener,List<AdminLevel> items, Map<Integer, Integer> admLocations) {

        this(inflater, optionsProvider, listener, items);

        if(admLocations!=null)
            this.admLocations.putAll(admLocations);
    }

    public LocationAddressAdapter(LayoutInflater inflater, OptionsProvider optionsProvider, AnswerListener listener,  List<AdminLevel> items) {
        this.inflater = inflater;
        this.optionsProvider = optionsProvider;
        this.listener = listener;
        this.items.addAll(items);
        setHasStableIds(true);
    }

    public void clearAnyFocus(@NonNull RecyclerView rv) {
        View focusedChild = rv.getFocusedChild();        // the child view holding current focus
        if (focusedChild != null) {
            View focusedInChild = focusedChild.findFocus(); // e.g., an EditText inside
            if (focusedInChild != null) focusedInChild.clearFocus();
            focusedChild.clearFocus();
            InputMethodManager imm = (InputMethodManager)
                    rv.getContext().getSystemService(Context.INPUT_METHOD_SERVICE);
            if (imm != null) imm.hideSoftInputFromWindow(focusedChild.getWindowToken(), 0);
        }
    }

    public void submit(List<AdminLevel> data) {
        items.clear();
        if (data != null) {
            // optional: sort by order
            ArrayList<AdminLevel> sorted = new ArrayList<>(data);
            Collections.sort(sorted, (a, b) -> Integer.compare(a.id, b.id));
            items.addAll(sorted);
        }
        notifyDataSetChanged();
    }

    public Map<Integer, Integer> getLocations(){
        return admLocations;
    }

    public boolean validate()
    {
        for(AdminLevel item : items)
        {
            if(item.isRequired )
            {

                if(admLocations.get(item.id)==null)
                {
                    if(viewHolders.get(item.id)==null)
                        continue;

                    viewHolders.get(item.id).setValidation("Required");

                    return false;
                }
                else
                {
                    viewHolders.get(item.id).clearValidation();
                }

            }

        }
        return true;
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = inflater.inflate(
                R.layout.item_select_admin_level,
                parent,
                false
        );
        return new VH_SelectAdm(view);
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {
        AdminLevel level = items.get(position);
        ((VH_SelectAdm) holder).bind(viewHolders, admLocations, optionsProvider, listener, level);
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

}
