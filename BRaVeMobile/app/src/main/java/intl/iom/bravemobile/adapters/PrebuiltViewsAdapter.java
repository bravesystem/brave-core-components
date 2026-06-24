package intl.iom.bravemobile.adapters;

import android.view.View;
import android.view.ViewGroup;
import android.view.ViewParent;
import android.widget.FrameLayout;
import android.widget.LinearLayout;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import intl.iom.bravemobile.models.registrations.Individual;

public class PrebuiltViewsAdapter extends RecyclerView.Adapter<PrebuiltViewsAdapter.VH> {

    private final List<LinearLayout> items;

    public PrebuiltViewsAdapter(List<LinearLayout> items) {
        this.items = items;
        setHasStableIds(true);
    }

    public void setData(List<LinearLayout> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }

    @Override public long getItemId(int position) { return position; }

    static class VH extends RecyclerView.ViewHolder {
        VH(View itemView) { super(itemView); }
    }

    @NonNull
    @Override
    public VH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        // Create a container; we'll insert your LinearLayout into it during onBind
        FrameLayout container = new FrameLayout(parent.getContext());
        container.setLayoutParams(new RecyclerView.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
        ));
        return new VH(container);
    }

    @Override
    public void onBindViewHolder(@NonNull VH holder, int position) {
        FrameLayout container = (FrameLayout) holder.itemView;
        container.removeAllViews();

        View row = items.get(position);

        // Detach from previous parent if necessary
        ViewParent oldParent = row.getParent();
        if (oldParent instanceof ViewGroup) {
            ((ViewGroup) oldParent).removeView(row);
        }

        container.addView(row, new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.WRAP_CONTENT
        ));
    }

    @Override
    public int getItemCount() {
        return items.size();
    }
}
