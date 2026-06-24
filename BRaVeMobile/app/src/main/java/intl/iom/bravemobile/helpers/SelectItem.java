package intl.iom.bravemobile.helpers;

import java.util.List;

public final class SelectItem
{
    private final int value;
    private final String label;
    private final boolean selected;
    private final boolean disabled;

    public SelectItem(int value, String label) { this(value, label, false, false); }
    public SelectItem(int value, String label, boolean selected, boolean disabled) {
        this.value = value; this.label = label; this.selected = selected; this.disabled = disabled;
    }

    public int getValue() { return value; }
    public String getLabel() { return label; }
    public boolean isSelected() { return selected; }
    public boolean isDisabled() { return disabled; }

    @Override
    public String toString() { return label; } // handy for adapters


    public static int indexOfValue(List<SelectItem> items, int value) {
        for (int i = 0; i < items.size(); i++)
            if (items.get(i).getValue() == value) return i;
        return -1;
    }

    public static Integer valueForLabel(List<SelectItem> items, String label) {
        if (label == null) return null;
        for (SelectItem si : items) if (label.equals(si.getLabel())) return si.getValue();
        return null;
    }
}
