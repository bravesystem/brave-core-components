package intl.iom.bravemobile.helpers;


import android.content.Context;
import androidx.appcompat.app.AlertDialog;
import com.google.android.material.progressindicator.CircularProgressIndicator;

import intl.iom.bravemobile.interfaces.LoadingHost;



import android.app.Activity;
import android.view.ViewGroup;
import android.widget.LinearLayout;
import android.widget.TextView;
import androidx.annotation.NonNull;
/**
 * Blocking (modal) loading host with spinner and optional text.
 * Compatible with API 21+.
 */
public class DialogLoadingHost implements LoadingHost {
    private final Activity activity;
    private AlertDialog dialog;
    private TextView messageView;
    private String initialMessage;

    public DialogLoadingHost(@NonNull Activity activity) {
        this.activity = activity;
    }

    /**
     * Optionally set an initial message shown when dialog is created.
     * You can also update message later via showLoading(String) or setMessage(String).
     */
    public DialogLoadingHost withInitialMessage(String message) {
        this.initialMessage = message;
        return this;
    }

    @Override
    public void showLoading() {
        showLoading(initialMessage);
    }

    /** Show with optional message (may be null or empty). */
    public void showLoading(String message) {
        if (activity.isFinishing()) return;
        activity.runOnUiThread(() -> {
            if (dialog != null && dialog.isShowing()) {
                // Update message if dialog already showing
                setMessage(message);
                return;
            }

            // Container layout
            LinearLayout container = new LinearLayout(activity);
            container.setOrientation(LinearLayout.HORIZONTAL); // you can switch to VERTICAL
            int padding = (int) (16 * activity.getResources().getDisplayMetrics().density);
            container.setPadding(padding, padding, padding, padding);
            container.setLayoutParams(new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WRAP_CONTENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT));

            // Spinner
            CircularProgressIndicator indicator = new CircularProgressIndicator(activity);
            indicator.setIndeterminate(true);

            // Message view
            messageView = new TextView(activity);
            messageView.setText(message != null ? message : "");
            messageView.setTextSize(16); // adjust as desired
            int spacing = (int) (12 * activity.getResources().getDisplayMetrics().density);
            messageView.setPadding(spacing, 0, 0, 0);

            // Add views
            container.addView(indicator,
                    new LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.WRAP_CONTENT,
                            ViewGroup.LayoutParams.WRAP_CONTENT));
            container.addView(messageView,
                    new LinearLayout.LayoutParams(
                            ViewGroup.LayoutParams.WRAP_CONTENT,
                            ViewGroup.LayoutParams.WRAP_CONTENT));

            dialog = new AlertDialog.Builder(activity)
                    .setView(container)
                    .setCancelable(false) // blocking
                    .create();
            dialog.setCanceledOnTouchOutside(false);
            dialog.show();
        });
    }

    /** Update the message while dialog is visible. */
    public void setMessage(String message) {
        activity.runOnUiThread(() -> {
            if (messageView != null) {
                messageView.setText(message != null ? message : "");
            }
        });
    }

    @Override
    public void hideLoading() {
        activity.runOnUiThread(() -> {
            if (dialog != null && dialog.isShowing()) {
                dialog.dismiss();
            }
            dialog = null;
            messageView = null;
        });
    }
}
