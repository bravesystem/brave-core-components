package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.core.content.ContextCompat;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.List;
import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.Enumerator;

public class EnumeratorAdapter extends BaseAdapter {

    private final Context context;
    private final List<Enumerator> enumerators;
    private final SimpleDateFormat sdf = new SimpleDateFormat("dd MMM yyyy, HH:mm", Locale.getDefault());

    public EnumeratorAdapter(Context context, List<Enumerator> enumerators) {
        this.context = context;
        this.enumerators = enumerators;
    }

    @Override
    public int getCount() {
        return enumerators.size();
    }

    @Override
    public Object getItem(int position) {
        return enumerators.get(position);
    }

    @Override
    public long getItemId(int position) {
        return position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        ViewHolder holder;

        if (convertView == null) {
            convertView = LayoutInflater.from(context)
                    .inflate(R.layout.item_enumerator_details, parent, false);
            holder = new ViewHolder();

            holder.tvFullName = convertView.findViewById(R.id.tvFullName);
            holder.tvCode = convertView.findViewById(R.id.tvCode);
            holder.tvTypeBadge = convertView.findViewById(R.id.tvTypeBadge);
            holder.tvNote = convertView.findViewById(R.id.tvNote);
            holder.tvActiveStatus = convertView.findViewById(R.id.tvActiveStatus);
            holder.tvLastUpdated = convertView.findViewById(R.id.tvLastUpdated);
            holder.tvExpiry = convertView.findViewById(R.id.tvExpiry);
            holder.iconEnumerator = convertView.findViewById(R.id.iconEnumerator);

            convertView.setTag(holder);
        } else {
            holder = (ViewHolder) convertView.getTag();
        }

        Enumerator e = enumerators.get(position);

        // Set basic fields
        holder.tvFullName.setText(e.fullName != null ? e.fullName : "Unnamed");
        holder.tvCode.setText("Code: " + (e.code != null ? e.code : "N/A"));
        holder.tvNote.setText("Note: " + (e.note != null ? e.note : "-"));

        // Supervisor vs Enumerator badge
        if (e.isSupervisor) {
            holder.tvTypeBadge.setText("SUPERVISOR");
            holder.tvTypeBadge.setBackgroundTintList(
                    ContextCompat.getColorStateList(context, android.R.color.holo_orange_dark));
        } else {
            holder.tvTypeBadge.setText("ENUMERATOR");
            holder.tvTypeBadge.setBackgroundTintList(
                    ContextCompat.getColorStateList(context, R.color.brave_blue));
        }

        // Active/Inactive
        if (e.isActive) {
            holder.tvActiveStatus.setText("Active");
            holder.tvActiveStatus.setTextColor(ContextCompat.getColor(context, android.R.color.holo_green_dark));
        } else {
            holder.tvActiveStatus.setText("Inactive");
            holder.tvActiveStatus.setTextColor(ContextCompat.getColor(context, android.R.color.holo_red_dark));
        }

        // Dates
        holder.tvLastUpdated.setText(" • Updated " + formatTimeAgo(e.lastUpdated));
        holder.tvExpiry.setText("Expires: " + sdf.format(new Date(e.expiredAt)));

        // Icon color
        holder.iconEnumerator.setImageResource(R.drawable.ic_person_24);
        holder.iconEnumerator.setColorFilter(ContextCompat.getColor(context, R.color.brave_blue));

        return convertView;
    }

    private String formatTimeAgo(long timeMs) {
        long diff = System.currentTimeMillis() - timeMs;
        long hours = diff / (1000 * 60 * 60);
        if (hours < 1) return "just now";
        else if (hours == 1) return "1 hour ago";
        else if (hours < 24) return hours + " hours ago";
        else return (hours / 24) + " days ago";
    }

    static class ViewHolder {
        TextView tvFullName, tvCode, tvTypeBadge, tvNote, tvPin, tvActiveStatus, tvLastUpdated, tvExpiry;
        ImageView iconEnumerator;
    }
}
