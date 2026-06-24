package intl.iom.bravemobile.models.activities;

import android.app.Activity;
import android.app.AlertDialog;

import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;

public class ConsentFlow {

    public interface Listener {
        /** Called when the flow finishes (no required “No” encountered). */
        void onCompleted(LinkedHashMap<Integer, Boolean> answers);
        /** Called when a required consent was answered "No" and the flow stopped. */
        void onStopped(Consent stoppingConsent, LinkedHashMap<Integer, Boolean> answers);
    }

    public static void start(Activity activity, List<Consent> rawConsents, Listener listener) {
        if (activity == null || rawConsents == null || rawConsents.isEmpty()) {
            if (listener != null) listener.onCompleted(new LinkedHashMap<Integer, Boolean>());
            return;
        }

        // 1) sort by order DESC
        List<Consent> consents = new ArrayList<>(rawConsents);
        Collections.sort(consents, new Comparator<Consent>() {
            @Override public int compare(Consent a, Consent b) {
                return Integer.compare(a.order, b.order); // desc
            }
        });

        // 2) start chain
        new Chain(activity, consents, listener).next(0, new LinkedHashMap<Integer, Boolean>());
    }

    // ---- internal runner ----
    private static class Chain {
        private final WeakReference<Activity> activityRef;
        private final List<Consent> consents;
        private final Listener listener;

        Chain(Activity activity, List<Consent> consents, Listener listener) {
            this.activityRef = new WeakReference<>(activity);
            this.consents = consents;
            this.listener = listener;
        }

        void next(final int index, final LinkedHashMap<Integer, Boolean> answers) {
            final Activity act = activityRef.get();
            if (act == null || act.isFinishing()) {
                if (listener != null) listener.onCompleted(answers);
                return;
            }
            if (index >= consents.size()) {
                if (listener != null) listener.onCompleted(answers);
                return;
            }

            final Consent c = consents.get(index);

            AlertDialog.Builder b = new AlertDialog.Builder(act)
                    .setTitle(c.isRequired ? "Consent (Required)" : "Consent")
                    .setMessage(c.getDefaultText() == null ? "" : c.getDefaultText())
                    .setCancelable(false)
                    .setPositiveButton("Yes", (dialog, which) -> {
                        answers.put(c.id, true);
                        // proceed to next
                        next(index + 1, answers);
                    })
                    .setNegativeButton("No", (dialog, which) -> {
                        answers.put(c.id, false);
                        if (c.isRequired) {
                            // stop the flow
                            if (listener != null) listener.onStopped(c, answers);
                        } else {
                            // optional: continue regardless
                            next(index + 1, answers);
                        }
                    });

            b.show();
        }
    }
}
