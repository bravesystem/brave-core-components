package intl.iom.bravemobile.ui;

import androidx.appcompat.app.AppCompatActivity;

import android.os.Bundle;
import android.view.View;
import android.widget.ListView;
import android.widget.TextView;
import androidx.appcompat.widget.SearchView;


import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.interfaces.EnumeratorService;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.adapters.EnumeratorAdapter;

public class EnumeratorListPage extends AppCompatActivity {

    private ListView lvResults;
    private TextView emptyView;
    private SearchView searchView;
    private EnumeratorAdapter adapter;
    private List<Enumerator> enumerators;          // Full list
    private List<Enumerator> filteredEnumerators;  // Filtered copy

    @Override
    public void onBackPressed() {
        // Disable back button if required
        return;
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_enumerator_list_page);

        setTitle("Enumerator List");

        lvResults = findViewById(R.id.lvResults);
        emptyView = findViewById(R.id.emptyView);
        searchView = findViewById(R.id.searchContainer);

        // ✅ Make the whole SearchView clickable
        searchView.setIconifiedByDefault(false);
        searchView.clearFocus();
        searchView.setOnClickListener(v -> {
            searchView.setIconified(false);
            searchView.requestFocus();
            android.view.inputmethod.InputMethodManager imm =
                    (android.view.inputmethod.InputMethodManager) getSystemService(android.content.Context.INPUT_METHOD_SERVICE);
            if (imm != null) {
                imm.showSoftInput(searchView.findFocus(), android.view.inputmethod.InputMethodManager.SHOW_IMPLICIT);
            }
        });

        EnumeratorService enumeratorService = ServiceLocator.enumeratorService(this);

        // Fetch the enumerator list
        enumerators = enumeratorService.listAll();
        filteredEnumerators = new ArrayList<>(enumerators);

        if (enumerators == null || enumerators.isEmpty()) {
            lvResults.setVisibility(View.GONE);
            emptyView.setVisibility(View.VISIBLE);
        } else {
            lvResults.setVisibility(View.VISIBLE);
            emptyView.setVisibility(View.GONE);

            adapter = new EnumeratorAdapter(this, filteredEnumerators);
            lvResults.setAdapter(adapter);
        }

        // 🔍 SearchView filtering logic
        searchView.setOnQueryTextListener(new SearchView.OnQueryTextListener() {
            @Override
            public boolean onQueryTextSubmit(String query) {
                filterList(query);
                return true;
            }

            @Override
            public boolean onQueryTextChange(String newText) {
                filterList(newText);
                return true;
            }
        });
    }

    private void filterList(String text) {
        if (enumerators == null) return;

        filteredEnumerators.clear();

        if (text == null || text.trim().isEmpty()) {
            filteredEnumerators.addAll(enumerators);
        } else {
            String query = text.toLowerCase();
            for (Enumerator e : enumerators) {
                if ((e.fullName != null && e.fullName.toLowerCase().contains(query)) ||
                        (e.code != null && e.code.toLowerCase().contains(query))) {
                    filteredEnumerators.add(e);
                }
            }
        }

        adapter.notifyDataSetChanged();

        if (filteredEnumerators.isEmpty()) {
            emptyView.setVisibility(View.VISIBLE);
            lvResults.setVisibility(View.GONE);
        } else {
            emptyView.setVisibility(View.GONE);
            lvResults.setVisibility(View.VISIBLE);
        }
    }
}
