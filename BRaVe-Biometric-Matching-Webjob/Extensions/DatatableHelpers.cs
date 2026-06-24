using BRaVe_Biometric_Matching_Webjob.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Extensions
{
    public static class DataTableHelpers
    {
        public static DataTable ToDataTable(this IEnumerable<Guid> ids, string columnName = "value")
        {
            var dt = new DataTable();
            dt.Columns.Add(new DataColumn(columnName, typeof(Guid)));

            foreach (var id in ids)
            {
                var row = dt.NewRow();
                row[columnName] = id;
                dt.Rows.Add(row);
            }

            return dt;
        }

        public static DataTable ToDataTable(this IEnumerable<ProcessedSubject> items, string tableName = "UuidStatusMapping")
        {
            var dt = new DataTable(tableName);

            // Define columns with exact types
            dt.Columns.Add(new DataColumn(nameof(ProcessedSubject.Id), typeof(Guid)));
            dt.Columns.Add(new DataColumn(nameof(ProcessedSubject.Status), typeof(int)));

            // Fast load block for large lists
            dt.BeginLoadData();
            try
            {
                foreach (var x in items)
                {
                    var row = dt.NewRow();
                    row[nameof(ProcessedSubject.Id)] = x.Id;
                    row[nameof(ProcessedSubject.Status)] = x.Status;
                    dt.Rows.Add(row);
                }
            }
            finally
            {
                dt.EndLoadData();
            }

            return dt;
        }

        public static DataTable ToDataTable(this IEnumerable<BiometricMatchStaging> items, string tableName = "MatchedTemplates")
        {
            var dt = new DataTable(tableName);

            // Define columns with exact types
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.Id), typeof(Guid)));
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.TenantId), typeof(int)));
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.MatchId), typeof(Guid)));
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.MatchTenantId), typeof(int)));
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.Score), typeof(int)));
            dt.Columns.Add(new DataColumn(nameof(BiometricMatchStaging.Status), typeof(int)));

            // Fast load block for large lists
            dt.BeginLoadData();
            try
            {
                foreach (var x in items)
                {
                    var row = dt.NewRow();
                    row[nameof(BiometricMatchStaging.Id)] = x.Id;
                    row[nameof(BiometricMatchStaging.TenantId)] = x.TenantId;
                    row[nameof(BiometricMatchStaging.MatchId)] = x.MatchId;
                    row[nameof(BiometricMatchStaging.MatchTenantId)] = x.MatchTenantId;
                    row[nameof(BiometricMatchStaging.Score)] = x.Score;
                    row[nameof(BiometricMatchStaging.Status)] = x.Status;
                    dt.Rows.Add(row);
                }
            }
            finally
            {
                dt.EndLoadData();
            }

            return dt;
        }

    }

}
