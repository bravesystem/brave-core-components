package intl.iom.bravemobile.helpers;

import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.text.TextUtils;
import android.view.View;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.AutoCompleteTextView;

import com.google.android.material.textfield.TextInputLayout;

import intl.iom.bravemobile.R;

public final class FormValidator {

    private FormValidator() {}

    /** True if ImageView currently shows an image. */
    public static boolean hasImage(ImageView iv) {
        if (iv.getDrawable() == null) return false;
        Drawable d = iv.getDrawable();
        if (d instanceof BitmapDrawable) {
            Bitmap bmp = ((BitmapDrawable) d).getBitmap();
            return bmp != null && !bmp.isRecycled();
        }
        // Other drawables (Vector, Color) count as "has image"
        return false;
    }

    /** Adds/removes a simple red highlight (stroke) around an ImageView. */
    private static void highlightImageError(ImageView iv, boolean on) {
        // Option 1: change background to a red-stroked shape when error
        if (on) {
            iv.setBackgroundResource(R.drawable.bg_image_error_outline);
        } else {
            iv.setBackground(null);
        }
        // Option 2 (alternative): tint or alpha could be used if you prefer.
    }

    // --- EditText / TextInputLayout ---

    /** Require non-empty text. Works with plain EditText. */
    public static boolean requireText(EditText et, String errorMsg) {
        if (et == null) return true;
        CharSequence s = et.getText();
        if (TextUtils.isEmpty(s)) {
            et.setError(errorMsg);
            et.requestFocus();
            return false;
        } else {
            et.setError(null);
            return true;
        }
    }

    /** Require non-empty text using TextInputLayout (nicer error UI). */
    public static boolean requireText(TextInputLayout til, String errorMsg) {
        if (til == null) return true;
        CharSequence s = (til.getEditText() != null) ? til.getEditText().getText() : "";
        boolean ok = !TextUtils.isEmpty(s);
        til.setError(ok ? null : errorMsg);
        if (!ok && til.getEditText() != null) {
            til.getEditText().requestFocus();
        }
        return ok;
    }

    /** Require non-empty for AutoCompleteTextView (searchable select-one). */
    public static boolean requireText(AutoCompleteTextView actv, String errorMsg) {
        if (actv == null) return true;
        boolean ok = !TextUtils.isEmpty(actv.getText());
        if (!ok) {
            actv.setError(errorMsg);
            actv.requestFocus();
        } else {
            actv.setError(null);
        }
        return ok;
    }

    // --- Spinner (with placeholder at position 0) ---

    /**
     * Validates that spinner selection is not the first (placeholder).
     * Shows error using a sibling TextView (recommended).
     */
    public static boolean requireSelection(Spinner spinner, TextView errorLabel, String errorMsg) {
        if (spinner == null) return true;
        boolean ok = spinner.getSelectedItemPosition() > 0;
        if (errorLabel != null) {
            errorLabel.setVisibility(ok ? View.GONE : View.VISIBLE);
            errorLabel.setText(ok ? "" : errorMsg);
        } else {
            // Fallback: try to setError on the selected view (works if it's a TextView)
            setSpinnerViewError(spinner, ok ? null : errorMsg);
        }
        if (!ok) spinner.requestFocus();
        return ok;
    }

    /**
     * Validates spinner without a dedicated error TextView.
     * Attempts to set error on the selected view text.
     */
    public static boolean requireSelection(Spinner spinner, String errorMsg) {
        return requireSelection(spinner, null, errorMsg);
    }

    /** Clears spinner error (for use with error Label). */
    public static void clearSpinnerError(TextView errorLabel) {
        if (errorLabel != null) {
            errorLabel.setText("");
            errorLabel.setVisibility(View.GONE);
        }
    }

    private static void setSpinnerViewError(Spinner spinner, String errorMsg) {
        View v = spinner.getSelectedView();
        if (v instanceof TextView) {
            TextView tv = (TextView) v;
            tv.setError(errorMsg);     // adds the error icon
            tv.setText(tv.getText());  // force redraw of same text with error
        }
    }

    // --- Utility to focus first invalid result among multiple validations ---

    /**
     * Runs validators in order; returns true only if ALL are true.
     * Each validator should perform its own setError/requestFocus.
     */
    @SafeVarargs
    public static boolean all(Validator... validators) {
        boolean allOk = true;
        for (Validator v : validators) {
            if (v != null) {
                boolean ok = v.validate();
                if (!ok) //allOk = false;
                    return false;
            }
        }
        return allOk;
    }


    public static boolean requireTextIfOther(boolean isDefaultSelected, String othText) {

        // If "Other" is selected (value = 99), EditText is required
        if (isDefaultSelected && StringUtils.isBlank(othText)) {
            return false;
        }

        return true;

    }

    public static boolean requireTextIfOther(int selectedReason, int OthValue, EditText etNoBioReasonOther, String err) {

        // If "Other" is selected (value = 99), EditText is required
        if (selectedReason==OthValue)
        {
            if (etNoBioReasonOther.getText().toString().trim().isEmpty()) {
                etNoBioReasonOther.setError(err);
                etNoBioReasonOther.requestFocus();
                return false;
            }
            return true;
        }

        etNoBioReasonOther.setText("");
        etNoBioReasonOther.setError(null);

        return true;

    }

    public static boolean validateSelection(boolean isDefaultSelected,    boolean collectionEnabled,    boolean isCollected )
    {
        boolean isValid = true;

        if (collectionEnabled && !isCollected) {
            // Default value NOT allowed
            isValid = !isDefaultSelected;
        }
        else if (isCollected)
        {
            // Default value REQUIRED
            isValid = isDefaultSelected;
        }

        return isValid;
    }

    public static boolean validateSelection(Spinner spinner, int defaultValue,    boolean collectionEnabled,    boolean isCollected, String errMessage  )
    {
        int selectedValue = SpinnerUtils.getSelected(spinner);

        boolean isValid = true;

        if (collectionEnabled && !isCollected) {
            // Default value NOT allowed
            isValid = selectedValue != defaultValue;
        }
        else if (isCollected)
        {
            // Default value REQUIRED
            isValid = selectedValue == defaultValue;
        }

        setSpinnerViewError(spinner, isValid ? null : errMessage);
        return isValid;
    }

    public static class ErrorMessageHolder
    {
        public boolean override = false;
        public String message;
    }

    public static boolean validateMinAge(int currentAge, int minAge, int relationship, String message, ErrorMessageHolder holder)
    {
        if(relationship == 0 && currentAge < minAge)
        {
            holder.override = true;
            holder.message = message;
            return false;
        }

        return true;
    }

    public static boolean validateMaxAge(int currentAge, int maxAge, String message, ErrorMessageHolder holder) {

        if(currentAge <= maxAge)
        {
            return true;
        }

        holder.override = true;
        holder.message = message;

        return false;
    }

    // Simple functional interface
    public interface Validator { boolean validate(); }
}
