package intl.iom.bravemobile.interfaces;

import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

public interface SectionAdapterFactory <T> {
    RecyclerView.Adapter<?> create(List<T> data);
}