package intl.iom.bravemobile.adapters;

import androidx.recyclerview.widget.RecyclerView;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.statics.DataPointType;


public class DataPointAdapter extends RecyclerView.Adapter<DataPointAdapter.VH> {

    private final List<DataPoint> items;

    public DataPointAdapter(List<DataPoint> items) {
        this.items = items;
    }

    @Override public VH onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_datapoint, parent, false);
        return new VH(v);
    }

    @Override public void onBindViewHolder(VH h, int position) {
        DataPoint dp = items.get(position);
        h.tvDpText.setText(dp.getDefaultText());
        h.tvDpCategory.setText(dp.dataPointType.getLabel());
        h.tvDpType.setText(dp.answerType.getLabel());

        int typeColor = (dp.dataPointType == DataPointType.INDIVIDUAL) ? 0xFF1976D2 : 0xFF00796B; // blue vs teal
        h.tvDpCategory.getBackground().setTint(typeColor);

        if (dp.isRequired) {
            h.tvDpRequired.setText("Required");
            // ✅ Tint background green for required
            h.tvDpRequired.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.itemView.getContext(), R.color.success_green)
            );
        } else {
            // either hide it or show "Optional"
            h.tvDpRequired.setText("Optional");
            h.tvDpRequired.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.itemView.getContext(), R.color.info_teal)
            );
        }
    }

    @Override public int getItemCount() { return items.size(); }

    static class VH extends RecyclerView.ViewHolder {
        final TextView tvDpText, tvDpCategory,tvDpType, tvDpRequired;
        VH(View v) {
            super(v);
            tvDpText = v.findViewById(R.id.tvDpText);
            tvDpCategory = v.findViewById(R.id.tvDpCategory);
            tvDpType = v.findViewById(R.id.tvDpType);
            tvDpRequired = v.findViewById(R.id.tvDpRequired);
        }
    }
}

