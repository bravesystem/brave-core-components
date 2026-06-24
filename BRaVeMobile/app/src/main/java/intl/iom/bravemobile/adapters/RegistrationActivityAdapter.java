package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.content.Intent;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;

import java.text.DateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.Optional;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.DateUtils;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.ui.distributions.DistributionDetailsPage;

public class RegistrationActivityAdapter extends BaseAdapter {

    public interface OnItemActionListener {
        void onViewClicked(RegistrationActivity item, int position);
        void onRefreshClicked(RegistrationActivity item, int position);
        void onSendDataClicked(RegistrationActivity item, int position) ;
        boolean isActive(RegistrationActivity item);
    }

    private final LayoutInflater inflater;
    private final DateFormat dateFmt;
    private final List<RegistrationActivity> items = new ArrayList<>();
    private final OnItemActionListener listener;

    public RegistrationActivityAdapter(Context context,
                                       List<RegistrationActivity> initial,
                                       OnItemActionListener listener) {
        this.inflater = LayoutInflater.from(context);
        this.dateFmt = DateFormat.getDateInstance(DateFormat.MEDIUM, Locale.getDefault());
        if (initial != null) items.addAll(initial);
        this.listener = listener;
    }

    /** Replace data and refresh the list */
    public void setData(List<RegistrationActivity> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }

    @Override public int getCount() { return items.size(); }
    @Override public RegistrationActivity getItem(int position) { return items.get(position); }
    @Override public long getItemId(int position) {
        RegistrationActivity a = items.get(position);
        return (a != null && a.code != null) ? a.code.hashCode() : position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {
        ViewHolder vh;
        if (convertView == null) {
            convertView = inflater.inflate(R.layout.item_registration_activity, parent, false);
            vh = new ViewHolder(convertView);
            convertView.setTag(vh);
        } else {
            vh = (ViewHolder) convertView.getTag();
        }


        RegistrationActivity item = getItem(position);

        vh.tvCode.setText(safe(item.code));
        vh.tvTitle.setText(safe(item.title));
        vh.tvDescription.setText(safe(item.description));
        vh.tvDates.setText(DateUtils.formatDateRange(dateFmt,item.startDate, item.endDate));

        //show badge if lock
        if(!listener.isActive(item))
        {
            vh.ivLockBadge.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(parent.getContext(), R.color.error_red)
            );
            vh.ivLockBadge.setVisibility(View.VISIBLE);
        }
        else
        {
            vh.ivLockBadge.setVisibility(View.GONE);
        }

        // Button click handlers — capture the current item/position
        vh.btnDnld.setOnClickListener(v -> {
            if (listener != null) listener.onRefreshClicked(item, position);
        });
        vh.btnSendData.setOnClickListener(v -> {
            if (listener != null) listener.onSendDataClicked(item, position);
        });

        convertView.setOnClickListener(v -> {
            if (listener != null) listener.onViewClicked(item, position);
        });

        return convertView;
    }

    /*private String formatDateRange(Date start, Optional<Date> endOpt) {
        String startStr = start != null ? dateFmt.format(start) : "—";
        String endStr = null;
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {
            endStr = (endOpt != null && endOpt.isPresent())
                    ? dateFmt.format(endOpt.get())
                    : "Open-ended";
        }
        return startStr + " — " + endStr;
    }*/

    private static String safe(String s) { return s == null ? "" : s; }

    private static class ViewHolder {
        final TextView tvCode;
        final TextView tvTitle;
        final TextView tvDescription;
        final TextView tvDates;
        final Button btnDnld;
        final Button btnSendData;
        final ImageView ivLockBadge;

        ViewHolder(View root) {
            tvCode = root.findViewById(R.id.tvCode);
            tvTitle = root.findViewById(R.id.tvTitle);
            tvDescription = root.findViewById(R.id.tvDescription);
            tvDates = root.findViewById(R.id.tvDates);
            btnDnld = root.findViewById(R.id.btnDnld);
            btnSendData = root.findViewById(R.id.btnSendData);
            ivLockBadge = root.findViewById(R.id.ivLockBadge);
        }
    }
}

