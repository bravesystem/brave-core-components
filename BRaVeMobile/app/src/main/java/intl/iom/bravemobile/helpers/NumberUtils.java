package intl.iom.bravemobile.helpers;

public class NumberUtils {

    public static String formatNum(double n) {
        // Use up to 15 significant digits and strip trailing zeros
        java.text.DecimalFormat df = new java.text.DecimalFormat("#.###############");
        df.setDecimalSeparatorAlwaysShown(false);
        return df.format(n);
    }
}
