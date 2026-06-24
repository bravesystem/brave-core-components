package intl.iom.bravemobile.helpers;

import android.widget.Spinner;

import java.util.List;

public final class SpinnerUtils {
    private SpinnerUtils() {}

    /** Select the first item whose value == targetValue. Returns true if found. */
    public static void selectByValue(Spinner spinner,  int targetValue)
    {
        if (spinner == null ) return ;

        for (int i = 0; i < spinner.getCount(); i++)
        {
            SelectItem selectItem = (SelectItem)spinner.getItemAtPosition(i);

            if (selectItem.getValue()==targetValue)
            {
                spinner.setSelection(i);
                return;
            }
        }
    }

    public static int getSelected(Spinner spinner)
    {
        SelectItem selectItem = (SelectItem)spinner.getSelectedItem();
        return selectItem.getValue();
    }


}
