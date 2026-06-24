package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.distributions.DistItem;

public class ItemsAdapter extends RecyclerView.Adapter<ItemsAdapter.ItemVH>{

    private final List<DistItem> items;

    public ItemsAdapter(List<DistItem> items) {
        this.items = items;
    }

    static class ItemVH extends RecyclerView.ViewHolder {
        TextView tvItemName, tvItemQuantity, tvItemMeta;

        ItemVH(@NonNull View itemView) {
            super(itemView);
            tvItemName = itemView.findViewById(R.id.tvItemName);
            tvItemQuantity = itemView.findViewById(R.id.tvItemQuantity);
            tvItemMeta = itemView.findViewById(R.id.tvItemMeta);
        }
    }


    @NonNull
    @Override
    public ItemVH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_distribution_item, parent, false);
        return new ItemVH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull ItemVH holder, int position) {
        DistItem item = items.get(position);
        holder.tvItemName.setText(item.name);
        holder.tvItemQuantity.setText(item.quantity + " " + item.uoM);
        holder.tvItemMeta.setText("ID: " + item.id +
                (StringUtils.isBlank(item.sku)? "": " • " + item.sku)
        );
    }

    @Override
    public int getItemCount() {
        return items == null ? 0 : items.size();
    }



}
