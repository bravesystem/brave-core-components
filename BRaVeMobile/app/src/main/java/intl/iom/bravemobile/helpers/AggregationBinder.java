package intl.iom.bravemobile.helpers;

import android.app.Activity;
import android.view.View;
import android.widget.TextView;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.ActivityAggregationResult;

public final class AggregationBinder {

    private static final SimpleDateFormat SYNC_FMT =
            new SimpleDateFormat("dd/MM/yyyy  HH:mm", Locale.getDefault());

    private AggregationBinder() {}

    /** Call from Activity after setContentView(...). */
    public static void bind(Activity activity, ActivityAggregationResult m) {
        View root = activity.findViewById(android.R.id.content);
        bind(root, m);
    }

    /** Call from Fragment with your root view. */
    public static void bind(View root, ActivityAggregationResult m) {
        // Top info
        setText(root, R.id.tvEnvironment, nn(m.environment));
        setText(root, R.id.tvUser, nn(m.enumerator));
        setText(root, R.id.tvLatestActivity, nn(m.latest_activity));
        setText(root, R.id.tvSync, formatDate(m.last_sync));

        // Card content
        setText(root, R.id.tvActiveActivities, nni(m.total_activities));
        setText(root, R.id.tvRegistrationSummary, nn(m.summary_registrations));
        setText(root, R.id.tvDistributionSummary, nn(m.summary_distributions));
        setText(root, R.id.tvVerificationSummary, nn(m.summary_verifications));
    }

    /* ---------- tiny helpers ---------- */

    private static void setText(View root, int id, String value) {
        TextView tv = root.findViewById(id);
        if (tv != null) tv.setText(value);
    }

    private static String nn(String s) {
        return (s == null || s.trim().isEmpty()) ? "—" : s;
    }

    private static String nni(Integer i) {
        return i == null ? "—" : String.valueOf(i);
    }

    private static String formatDate(Date d) {
        return d == null ? "—" : SYNC_FMT.format(d);
    }
}
