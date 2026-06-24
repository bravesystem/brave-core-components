package intl.iom.bravemobile.binders;

import android.content.Context;
import android.view.View;
import android.widget.ArrayAdapter;
import android.widget.ListView;
import android.widget.TextView;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.interfaces.SectionAdapterFactory;

public class SectionBinder {

    public static <T> void bindSection(
            List<T> optData,
            RecyclerView rv,
            TextView tvHeader,
            TextView tvEmpty,
            SectionAdapterFactory<T> adapterFactory) {

        // No data present -> hide list + header; show empty
        if (optData == null ) {
            if (rv != null) { rv.setVisibility(View.GONE); rv.setAdapter(null); }
            //if (tvHeader != null) tvHeader.setVisibility(View.GONE);
            //if (tvEmpty != null) tvEmpty.setVisibility(View.VISIBLE);
            return;
        }

        // Data is present -> show list, hide empty
        //List<T> data = optData.get();
        if (rv != null) {
            rv.setVisibility(View.VISIBLE);
            if (rv.getLayoutManager() == null) {
                rv.setLayoutManager(new LinearLayoutManager(rv.getContext()));
            }
        }
        //if (tvEmpty != null) tvEmpty.setVisibility(View.GONE);

        // If there are items, show header and set adapter; else hide header and clear adapter
        boolean hasItems = optData != null && !optData.isEmpty();
        //if (tvHeader != null) tvHeader.setVisibility(hasItems ? View.VISIBLE : View.GONE);

        tvEmpty.setVisibility(hasItems ? View.GONE : View.VISIBLE);

        if (rv != null) {
            if (hasItems) {
                rv.setAdapter(adapterFactory.create(optData));
            } else {
                rv.setAdapter(null); // avoid showing stale items
            }
        }
    }

    public static <T> void bindSection(
            Optional<List<T>> optData,
            RecyclerView rv,
            TextView tvHeader,
            TextView tvEmpty,
            SectionAdapterFactory<T> adapterFactory) {

        // No data present -> hide list + header; show empty
        if (optData == null || !optData.isPresent())
        {
            if (rv != null)
            {
                rv.setVisibility(View.GONE);
                rv.setAdapter(null);
            }
            //if (tvHeader != null) tvHeader.setVisibility(View.GONE);
            //if (tvEmpty != null) tvEmpty.setVisibility(View.VISIBLE);
            return;
        }

        // Data is present -> show list, hide empty
        List<T> data = optData.get();

        if (rv != null) {
            rv.setVisibility(View.VISIBLE);
            if (rv.getLayoutManager() == null) {
                rv.setLayoutManager(new LinearLayoutManager(rv.getContext()));
            }
        }
        //if (tvEmpty != null) tvEmpty.setVisibility(View.GONE);

        // If there are items, show header and set adapter; else hide header and clear adapter
        boolean hasItems = data != null && !data.isEmpty();
        //if (tvHeader != null) tvHeader.setVisibility(hasItems ? View.VISIBLE : View.GONE);

        tvEmpty.setVisibility(hasItems ? View.GONE : View.VISIBLE);

        if (rv != null) {
            if (hasItems) {
                rv.setAdapter(adapterFactory.create(data));
            } else {
                rv.setAdapter(null); // avoid showing stale items
            }
        }
    }
}
