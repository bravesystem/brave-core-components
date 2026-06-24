package intl.iom.bravemobile.helpers;

import android.app.AlertDialog;
import android.content.Context;
import android.text.InputFilter;
import android.text.InputType;
import android.view.Gravity;
import android.view.inputmethod.InputMethodManager;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;

import intl.iom.bravemobile.interfaces.CommentCallback;
import intl.iom.bravemobile.models.registrations.Individual;

public final class AlertDialogUtils {

    public interface Callback{

        void onPositive();

    }


    public interface InputCallback {
        void onInput(String value);
    }





    public static void inputDialog(Context context, String title, String hint, InputCallback callback) {
        // Create EditText
        final EditText input = new EditText(context);
        input.setHint(hint);
        input.setInputType(InputType.TYPE_CLASS_TEXT);

        // Center text and apply margins
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        params.setMargins(50, 0, 50, 0); // left, top, right, bottom
        input.setLayoutParams(params);
        input.setGravity(Gravity.CENTER); // center text horizontally

        // Force uppercase input
        input.setFilters(new InputFilter[]{
                new InputFilter.AllCaps() // converts all input to uppercase
        });

        AlertDialog.Builder builder = new AlertDialog.Builder(context)
                .setTitle(title)
                .setView(input)
                .setCancelable(false)
                .setPositiveButton("OK", (dialog, which) -> {
                    String value = input.getText().toString().trim();
                    callback.onInput(value);
                })
                .setNegativeButton("Cancel", (dialog, which) -> dialog.dismiss());

        builder.show();
    }



    public static void confirmDialog(Context context, String title,String message, Callback callback)
    {
        AlertDialog.Builder b = new AlertDialog.Builder(context)
                .setTitle(title)
                .setMessage(message)
                .setCancelable(false)
                .setPositiveButton("Yes", (dialog, which) -> {
                    callback.onPositive();
                })
                .setNegativeButton("No", (dialog, which) -> {

                });

        b.show();
    }

    public static void showDialog(Context context, String title,String message, int resource)
    {
        new AlertDialog.Builder(context)
                .setTitle(title)
                .setMessage(message)
                .setPositiveButton(resource, (dialog, which) -> {
                    // Action when OK is clicked
                    dialog.dismiss();
                })
                .setCancelable(false) // Prevent dismissing by tapping outside
                .show();
    }

    public static void showDialog(Context context, String title,String message, int resource, Callback callback)
    {
        new AlertDialog.Builder(context)
                .setTitle(title)
                .setMessage(message)
                .setPositiveButton(resource, (dialog, which) -> {
                    // Action when OK is clicked
                    dialog.dismiss();
                    callback.onPositive();
                })
                .setCancelable(false) // Prevent dismissing by tapping outside
                .show();
    }


    public static void showCommentDialog(Context ctx, String initial, String title, String errorTxt, CommentCallback cb) {
        final EditText input = new EditText(ctx);
        input.setHint("Type your comment");
        input.setText(initial == null ? "" : initial);
        input.setSelection(input.getText().length());
        input.setMinLines(3);
        input.setMaxLines(6);
        input.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        input.setGravity(Gravity.TOP | Gravity.START);

        AlertDialog dialog = new AlertDialog.Builder(ctx)
                .setTitle(title)
                .setView(input)
                .setPositiveButton("OK", null)          // set null, we’ll override to validate
                .setNegativeButton("Cancel", (d, w) -> d.dismiss())
                .create();

        dialog.setOnShowListener(d -> {
            Button ok = dialog.getButton(AlertDialog.BUTTON_POSITIVE);
            ok.setOnClickListener(v -> {
                String txt = input.getText().toString().trim();
                if (txt.isEmpty()) {
                    input.setError(errorTxt);
                    input.requestFocus();
                } else {
                    dialog.dismiss();
                    if (cb != null) cb.onComment(txt);
                }
            });
            // show keyboard
            input.post(() -> {
                input.requestFocus();
                InputMethodManager imm = (InputMethodManager) ctx.getSystemService(Context.INPUT_METHOD_SERVICE);
                if (imm != null) imm.showSoftInput(input, InputMethodManager.SHOW_IMPLICIT);
            });
        });

        dialog.show();
    }


    public static void showCommentDialog(Context ctx, String initial, CommentCallback cb) {

        showCommentDialog(ctx, null, "Add a reason for not consenting to data collection.", "Please enter a comment", cb);
        /*final EditText input = new EditText(ctx);
        input.setHint("Type your comment");
        input.setText(initial == null ? "" : initial);
        input.setSelection(input.getText().length());
        input.setMinLines(3);
        input.setMaxLines(6);
        input.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_MULTI_LINE);
        input.setGravity(Gravity.TOP | Gravity.START);

        AlertDialog dialog = new AlertDialog.Builder(ctx)
                .setTitle("Add a reason for not consenting to data collection.")
                .setView(input)
                .setPositiveButton("OK", null)          // set null, we’ll override to validate
                .setNegativeButton("Cancel", (d, w) -> d.dismiss())
                .create();

        dialog.setOnShowListener(d -> {
            Button ok = dialog.getButton(AlertDialog.BUTTON_POSITIVE);
            ok.setOnClickListener(v -> {
                String txt = input.getText().toString().trim();
                if (txt.isEmpty()) {
                    input.setError("Please enter a comment");
                    input.requestFocus();
                } else {
                    dialog.dismiss();
                    if (cb != null) cb.onComment(txt);
                }
            });
            // show keyboard
            input.post(() -> {
                input.requestFocus();
                InputMethodManager imm = (InputMethodManager) ctx.getSystemService(Context.INPUT_METHOD_SERVICE);
                if (imm != null) imm.showSoftInput(input, InputMethodManager.SHOW_IMPLICIT);
            });
        });

        dialog.show();*/
    }

}
