package intl.iom.bravemobile.interfaces;

public interface Debouncer<T> {
    /** Schedule a value to be emitted after the debounce window. */
    void submit(T value);

    /** Cancel any pending emission. */
    void cancel();

    /** Immediately emit the last submitted value if pending, then clear. */
    void flush();

    /** Update the debounce window at runtime (optional). */
    void setDelayMillis(long delayMillis);

    /** Release resources if any (optional). */
    void dispose();
}