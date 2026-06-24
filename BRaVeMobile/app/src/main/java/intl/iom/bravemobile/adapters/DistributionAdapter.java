package intl.iom.bravemobile.adapters;

import android.content.Intent;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.button.MaterialButton;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.interfaces.DistributionService;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;
import intl.iom.bravemobile.ui.activities.ActivityDetailsPage;
import intl.iom.bravemobile.ui.distributions.DistributionDetailsPage;
import intl.iom.bravemobile.ui.distributions.DistributionListPage;

import android.content.Context;

public class DistributionAdapter extends BaseAdapter {

    private final Context context;
    private final LayoutInflater inflater;
    private final List<Distribution> items = new ArrayList<>();

    private DistributionService distributionService;

    public interface OnItemActionListener
    {
        void onClick(Distribution d);
    }

    private final DistributionAdapter.OnItemActionListener listener;

    public DistributionAdapter(Context context, DistributionAdapter.OnItemActionListener listener) {
        this.listener = listener;
        this.context = context;
        this.inflater = LayoutInflater.from(context);
        distributionService = ServiceLocator.distributionService(context);
    }

    public void submitList(List<Distribution> newItems) {
        items.clear();
        if (newItems != null) {
            items.addAll(newItems);
        }
        notifyDataSetChanged();
    }

    @Override
    public int getCount() {
        return items.size();
    }

    @Override
    public Distribution getItem(int position) {
        return items.get(position);
    }

    @Override
    public long getItemId(int position) {
        // If you have a stable numeric ID, return it; otherwise position is fine.
        return position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        ViewHolder vh;
        if (convertView == null) {
            convertView = inflater.inflate(R.layout.item_distribution, parent, false);
            vh = new ViewHolder(convertView);
            convertView.setTag(vh);
        } else {
            vh = (ViewHolder) convertView.getTag();
        }

        Distribution distribution = getItem(position);

        // Bind data
        vh.tvTitle.setText(distribution.title != null ? distribution.title : "(No title)");
        String display = String.format("ID: %d • Total enrollments: %d",
                (distribution.distributionId != 0 ? distribution.distributionId : "—"),
                distributionService.getTotalEnrollments(distribution.activityCode,distribution.distributionId)
                );
        vh.tvId.setText(display);

        int kitsCount = (distribution.kits != null) ? distribution.kits.size() : 0;
        vh.tvKitsCount.setText("Kits: " + kitsCount);

        if (distribution.additionalInfo != null && !distribution.additionalInfo.trim().isEmpty()) {
            vh.tvAdditionalInfo.setText(distribution.toString());
            vh.tvAdditionalInfo.setVisibility(View.VISIBLE);
        } else {
            vh.tvAdditionalInfo.setText(distribution.toString());
            vh.tvAdditionalInfo.setVisibility(View.VISIBLE); // or View.GONE if you prefer hiding
        }

        // Per-item buttons (Toast actions)
        /*vh.btnFpScan.setOnClickListener(v ->
                Toast.makeText(context,
                        "FP Scan: " + (distribution.title != null ? distribution.title : distribution.id),
                        Toast.LENGTH_SHORT).show());

        vh.btnBarcode.setOnClickListener(v ->
                Toast.makeText(context,
                        "Barcode: " + (distribution.title != null ? distribution.title : distribution.id),
                        Toast.LENGTH_SHORT).show());
                        */

        // Optional: whole item click
        convertView.setOnClickListener(v -> {
                    Intent i = new Intent(context, DistributionDetailsPage.class);

                    distributionService.setCurrent(distribution.distributionId);

                    context.startActivity(i);
                 /*Toast.makeText(context,
                        "Selected: " + (distribution.title != null ? distribution.title : distribution.id),
                        Toast.LENGTH_SHORT).show()*/
        });

        vh.btnGetEnrollments.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                if (listener != null) listener.onClick(distribution);
            }
        });

        return convertView;
    }

    private static class ViewHolder {
        TextView tvTitle, tvId, tvKitsCount, tvAdditionalInfo;
        MaterialButton btnGetEnrollments;
        // If using plain Button, change type accordingly
        //MaterialButton btnFpScan, btnBarcode;

        ViewHolder(View itemView) {
            tvTitle = itemView.findViewById(R.id.tvTitle);
            tvId = itemView.findViewById(R.id.tvId);
            tvKitsCount = itemView.findViewById(R.id.tvKitsCount);
            tvAdditionalInfo = itemView.findViewById(R.id.tvAdditionalInfoPlaceholder);
            btnGetEnrollments = itemView.findViewById(R.id.btnGetEnrollments);

           /* btnFpScan = itemView.findViewById(R.id.btnItemFpScan);
            btnBarcode = itemView.findViewById(R.id.btnItemBarcode);*/
        }
    }
}
