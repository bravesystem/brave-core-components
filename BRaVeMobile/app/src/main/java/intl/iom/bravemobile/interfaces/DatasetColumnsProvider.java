package intl.iom.bravemobile.interfaces;

import java.util.List;

import intl.iom.bravemobile.models.DatasetColumn;

public interface DatasetColumnsProvider {
    List<DatasetColumn> getDatasetColumnsFor(int datasetId);
}
