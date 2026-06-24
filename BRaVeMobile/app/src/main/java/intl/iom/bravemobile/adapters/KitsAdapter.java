package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.distributions.Kit;

public class KitsAdapter extends RecyclerView.Adapter<KitsAdapter.KitVH>{


    private final List<Kit> kits;

    public KitsAdapter(List<Kit> kits) {
        this.kits = kits;
    }

    static class KitVH extends RecyclerView.ViewHolder {
        TextView tvKitTitle, tvKitTotal, tvKitId, tvRecordType, tvItemsHeader;
        RecyclerView rvItems;

        KitVH(@NonNull View itemView) {
            super(itemView);
            tvItemsHeader = itemView.findViewById(R.id.tvItemsHeader);
            tvRecordType = itemView.findViewById(R.id.tvRecordType);
            tvKitTitle = itemView.findViewById(R.id.tvKitTitle);
            tvKitTotal = itemView.findViewById(R.id.tvKitTotal);
            tvKitId = itemView.findViewById(R.id.tvKitId);
            rvItems = itemView.findViewById(R.id.rvItems);
        }
    }


    @NonNull
    @Override
    public KitVH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_kit, parent, false);
        return new KitVH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull KitVH holder, int position) {
        Kit kit = kits.get(position);

        if("I".equalsIgnoreCase(kit.targetType))
        {
            holder.tvRecordType.setText(R.string.label_record_type_item);
            holder.tvKitTitle.setVisibility(View.INVISIBLE);
            holder.tvKitId.setVisibility(View.GONE);
        }
        else
        {
            holder.tvRecordType.setText(R.string.label_record_type_kit);
        }

        if(kit.items.size()<=1)
        {
            holder.tvItemsHeader.setText(R.string.label_item_singular);
        }
        else
        {
            holder.tvItemsHeader.setText(R.string.label_item_plural);
        }

        holder.tvKitTitle.setText(kit.title);
        holder.tvKitTotal.setText("Total: " + kit.quantity);
        holder.tvKitId.setText(
                "ID: " + kit.kitId + (StringUtils.isBlank(kit.sku) ? "" : " • " + kit.sku)
        );

        holder.rvItems.setLayoutManager(new LinearLayoutManager(holder.itemView.getContext()));
        holder.rvItems.setNestedScrollingEnabled(false);
        holder.rvItems.setAdapter(new ItemsAdapter(kit.items));
    }

    @Override
    public int getItemCount() {
        return kits == null ? 0 : kits.size();
    }


}
