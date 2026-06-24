package intl.iom.bravemobile.helpers;

import android.app.DatePickerDialog;
import android.content.Context;
import android.widget.EditText;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;

/**
 * Helper for attaching a DatePickerDialog to an EditText.
 * Displays the selected date in yyyy-MM-dd format.
 */
public class DatePickerHelper {

    private static final SimpleDateFormat DATE_FORMAT =
            new SimpleDateFormat("yyyy-MM-dd", Locale.US);


    public static Date getDateFromEditText(EditText etDob) {
        String dobText = etDob.getText().toString().trim();

        if (dobText.isEmpty()) {
            return null;
        }

        try {
            return DATE_FORMAT.parse(dobText);
        } catch (ParseException e) {
            e.printStackTrace();
            return null;
        }
    }


    /**
     * Attaches a DatePickerDialog to the provided EditText.
     *
     * @param context The Activity or Fragment context.
     * @param editText The EditText to attach the picker to.
     * @param maxToday If true, disables selecting future dates.
     */
    public static void attach(final Context context,
                              final EditText editText,
                              final boolean maxToday) {

        editText.setFocusable(false);
        editText.setOnClickListener(v -> showDatePicker(context, editText, maxToday));
        editText.setOnFocusChangeListener((v, hasFocus) -> {
            if (hasFocus) showDatePicker(context, editText, maxToday);
        });
    }

    /**
     * Opens the DatePickerDialog initialized to current EditText value or today.
     */
    private static void showDatePicker(Context context, EditText editText, boolean maxToday) {
        final Calendar cal = Calendar.getInstance();

        // Parse current value if any
        String current = editText.getText().toString().trim();
        if (!current.isEmpty()) {
            try {
                Date parsed = DATE_FORMAT.parse(current);
                if (parsed != null) cal.setTime(parsed);
            } catch (ParseException ignored) {}
        }

        int year = cal.get(Calendar.YEAR);
        int month = cal.get(Calendar.MONTH);
        int day = cal.get(Calendar.DAY_OF_MONTH);

        DatePickerDialog dialog = new DatePickerDialog(
                context,
                (view, y, m, d) -> {
                    Calendar selected = Calendar.getInstance();
                    selected.set(y, m, d);
                    editText.setText(DATE_FORMAT.format(selected.getTime()));
                    editText.clearFocus();
                },
                year, month, day
        );

        if (maxToday) dialog.getDatePicker().setMaxDate(System.currentTimeMillis());
        dialog.show();
    }
}
