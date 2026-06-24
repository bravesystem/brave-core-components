package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.distributions.DistItem;

public class DistItemAdapter extends RecyclerView.Adapter<DistItemAdapter.VH> {

    private final List<DistItem> items;
    private final Set<Integer> selectedIds = new HashSet<>();


    public DistItemAdapter(@NonNull List<DistItem> items) {
        this.items = items;
        // Pre-select all by default
        for (DistItem it : items) {
            selectedIds.add(it.id);
        }
        setHasStableIds(true);
    }

    private boolean locked = false; // <-- NEW: locks UI if true
    /** Optional helper for array style export */
    public boolean isSelected(int id) {
        return locked || selectedIds.contains(id);
    }

    // ---------- NEW API ----------

    /** Lock: select all and disable user interaction */
    public void lock() {
        locked = true;
        // Ensure all are selected while locked
        for (DistItem it : items) selectedIds.add(it.id);
        notifyDataSetChanged();
    }

    /** Unlock: re-enable interaction (keeps current selections) */
    public void unlock() {
        locked = false;
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public DistItemAdapter.VH onCreateViewHolder(@NonNull ViewGroup parent, int position) {

        View view = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_dist, parent, false);
        return new VH(view);

    }

    @Override
    public void onBindViewHolder(@NonNull DistItemAdapter.VH holder, int position) {

        DistItem item = items.get(position);
        holder.tvTitle.setText(item.toString());

        // Detach listener to avoid triggering on setChecked()
        holder.cb.setOnCheckedChangeListener(null);

        // When locked, ensure it's checked; otherwise reflect current state
        boolean isChecked = locked || selectedIds.contains(item.id);
        holder.cb.setChecked(isChecked);

        // Disable interactivity when locked
        holder.cb.setEnabled(!locked);
        holder.itemView.setEnabled(!locked);

        if (!locked) {
            // Normal interactive mode
            holder.cb.setOnCheckedChangeListener((buttonView, checked) -> {
                if (checked) selectedIds.add(item.id); else selectedIds.remove(item.id);
            });

            // Tap on row toggles checkbox
            holder.itemView.setOnClickListener(v -> holder.cb.toggle());
        } else {
            // In locked mode, remove any click listeners to avoid toggles
            holder.itemView.setOnClickListener(null);
        }

    }

    @Override
    public int getItemCount() {
        return items.size();
    }


    static class VH extends RecyclerView.ViewHolder {
        CheckBox cb;
        TextView tvTitle;

        VH(@NonNull View itemView) {
            super(itemView);
            cb = itemView.findViewById(R.id.cbSelect);
            tvTitle = itemView.findViewById(R.id.tvTitle);
        }
    }

    private boolean no_selection = true;

    /** Returns a Map-like JSON of { "<id>": true/false } */
    public JSONObject getSelectionJson() {

        no_selection = true;

        JSONObject obj = new JSONObject();
        for (DistItem it : items) {
            boolean selected = selectedIds.contains(it.id);

            if(selected)
                no_selection=false;

            try {
                //obj.put(String.valueOf(it.id), selected);
                obj.put(it.toString(), selected);
            } catch (JSONException e) {
                // Handle if needed
            }
        }
        return obj;
    }

    public boolean emptySelection()
    {
        return no_selection;
    }

}
