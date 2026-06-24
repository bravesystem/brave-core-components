using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace BRaVe_Management_Backend.Services
{
    public class InMemoryLuceneSearchEngine //: ISearchEngine
    {
        /*private readonly LuceneVersion _version = LuceneVersion.LUCENE_48;
        private RAMDirectory _directory = new();
        private StandardAnalyzer _analyzer;
        private SearchIndexOptions? _options;
        private List<object> _sourceDocuments = new();

        public InMemoryLuceneSearchEngine()
        {
            _analyzer = new StandardAnalyzer(_version);

        }

        public async Task IndexAsync<T>(
      IReadOnlyCollection<T> documents,
      SearchIndexOptions options)
        {
            _options = options;
            _sourceDocuments = documents.Cast<object>().ToList();

            var config = new IndexWriterConfig(_version, _analyzer);
            using var writer = new IndexWriter(_directory, config);

            int id = 0;
            foreach (var doc in documents)
            {
                var luceneDoc = new Document
                {
                    new StoredField("_doc_id", id++)
                };

                foreach (var field in options.Fields)
                {
                    var value = ReflectionHelper.GetValue(doc, field.Name);
                    if (value == null) continue;

                    AddField(luceneDoc, field, value);
                }

                writer.AddDocument(luceneDoc);
            }

            writer.Commit();
            await Task.CompletedTask;
        }

        public async Task<SearchResult<T>> SearchAsync<T>(SearchRequest request)
        {
            using var reader = DirectoryReader.Open(_directory);
            var searcher = new IndexSearcher(reader);

            var booleanQuery = new BooleanQuery();

            if (!string.IsNullOrWhiteSpace(request.Text))
            {
                var parser = new MultiFieldQueryParser(
                    _version,
                    _options!.Fields
                        .Where(f => f.Type == SearchFieldType.Text)
                        .Select(f => f.Name)
                        .ToArray(),
                    _analyzer);

                booleanQuery.Add(parser.Parse(request.Text), Occur.MUST);
            }

            foreach (var filter in request.Filters)
            {
                var filterQuery = new ConstantScoreQuery(BuildFilter(filter));
                booleanQuery.Add(filterQuery, Occur.MUST);

            }

            Sort? sort = null;
            if (request.Sorts.Any())
            {
                var sortFields = request.Sorts.Select(s =>
                    new SortField(
                        s.Field,
                        SortFieldType.STRING,
                        s.Direction == SortDirection.Descending)).ToArray();

                sort = new Sort(sortFields);
            }

            var topDocs = searcher.Search(
                booleanQuery,
                null,
                request.Page * request.PageSize,
                sort);

            var hits = topDocs.ScoreDocs
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h =>
                {
                    var doc = searcher.Doc(h.Doc);
                    var idx = doc.GetField("_doc_id").GetInt32Value()!.Value;
                    return (T)_sourceDocuments[idx];
                })
                .ToList();

            return await Task.FromResult(new SearchResult<T>
            {
                Items = hits,
                Total = topDocs.TotalHits,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }


        public Task ClearAsync()
        {
            _directory = new RAMDirectory();
            _sourceDocuments.Clear();
            return Task.CompletedTask;
        }

        private void AddField(Document doc, SearchField field, object value)
        {
            switch (field.Type)
            {
                case SearchFieldType.Text:
                    doc.Add(new TextField(field.Name, value.ToString(), Field.Store.NO));
                    break;

                case SearchFieldType.Keyword:
                    doc.Add(new StringField(field.Name, value.ToString(), Field.Store.NO));
                    break;

                case SearchFieldType.Number:
                    doc.Add(new Int64Field(field.Name, Convert.ToInt64(value), Field.Store.NO));
                    break;

                case SearchFieldType.Date:
                    doc.Add(new Int64Field(field.Name, ((DateTime)value).Ticks, Field.Store.NO));
                    break;

                case SearchFieldType.Boolean:
                    doc.Add(new StringField(field.Name, value.ToString(), Field.Store.NO));
                    break;
            }
        }

        private Query BuildFilter(SearchFilter filter)
        {
            return filter.Operator switch
            {
                FilterOperator.Equals =>
                    new TermQuery(new Term(filter.Field, filter.Value.ToString())),

                FilterOperator.GreaterThan =>
                    NumericRangeQuery.NewInt64Range(
                        filter.Field,
                        Convert.ToInt64(filter.Value) + 1,
                        null,
                        true,
                        true),

                FilterOperator.GreaterThanOrEqual =>
                    NumericRangeQuery.NewInt64Range(
                        filter.Field,
                        Convert.ToInt64(filter.Value),
                        null,
                        true,
                        true),

                FilterOperator.LessThan =>
                    NumericRangeQuery.NewInt64Range(
                        filter.Field,
                        null,
                        Convert.ToInt64(filter.Value) - 1,
                        true,
                        true),

                FilterOperator.LessThanOrEqual =>
                    NumericRangeQuery.NewInt64Range(
                        filter.Field,
                        null,
                        Convert.ToInt64(filter.Value),
                        true,
                        true),

                _ => throw new NotSupportedException()
            };
        }

        */
    }
}
