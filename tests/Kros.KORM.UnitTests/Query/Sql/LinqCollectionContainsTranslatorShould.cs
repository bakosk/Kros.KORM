using Kros.KORM.Metadata.Attribute;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    /// <summary>
    /// Translation of <c>collection.Contains(column)</c> into SQL <c>IN</c> operator (issue #122).
    /// </summary>
    public class LinqCollectionContainsTranslatorShould : LinqTranslatorTestBase
    {
        private const string SelectDocuments = "SELECT Id, NumberingSequence FROM Documents";

        [Fact]
        public void TranslateListContainsToIn()
        {
            var ids = new List<int> { 1, 2, 3 };
            var query = Query<Document>().Where(d => ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((Id IN (@1, @2, @3)))", 1, 2, 3);
        }

        [Fact]
        public void TranslateArrayContainsToIn()
        {
            // Since C# 14 this binds to MemoryExtensions.Contains(ReadOnlySpan<T>, T).
            int[] ids = [1, 2, 3];
            var query = Query<Document>().Where(d => ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((Id IN (@1, @2, @3)))", 1, 2, 3);
        }

        [Fact]
        public void TranslateInlineArrayContainsToIn()
        {
            var query = Query<Document>().Where(d => new[] { 4, 5 }.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((Id IN (@1, @2)))", 4, 5);
        }

        [Fact]
        public void TranslateEnumerableContainsToIn()
        {
            IEnumerable<int> ids = new List<int> { 1, 2, 3 };
            var query = Query<Document>().Where(d => ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((Id IN (@1, @2, @3)))", 1, 2, 3);
        }

        [Fact]
        public void TranslateHashSetContainsToIn()
        {
            var ids = new HashSet<int> { 1, 2 };
            var query = Query<Document>().Where(d => ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((Id IN (@1, @2)))", 1, 2);
        }

        [Fact]
        public void TranslateStringCollectionContainsToIn()
        {
            var sequences = new List<string> { "of1", "of2" };
            var query = Query<Document>().Where(d => sequences.Contains(d.NumberingSequence));

            AreSame(query, SelectDocuments + " WHERE ((NumberingSequence IN (@1, @2)))", "of1", "of2");
        }

        [Fact]
        public void TranslateContainsCombinedWithOtherConditionsWithCorrectParameterOrder()
        {
            var ids = new List<int> { 10, 20 };
            var query = Query<Document>().Where(d => d.Id > 5 && ids.Contains(d.Id) && d.NumberingSequence == "of1");

            AreSame(
                query,
                SelectDocuments + " WHERE ((((Id > @1) AND (Id IN (@2, @3))) AND (NumberingSequence = @4)))",
                5, 10, 20, "of1");
        }

        [Fact]
        public void TranslateNegatedContainsToNotIn()
        {
            var ids = new List<int> { 1, 2 };
            var query = Query<Document>().Where(d => !ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE (NOT (Id IN (@1, @2)))", 1, 2);
        }

        [Fact]
        public void TranslateContainsOfEmptyCollectionToFalseCondition()
        {
            var ids = new List<int>();
            var query = Query<Document>().Where(d => ids.Contains(d.Id));

            AreSame(query, SelectDocuments + " WHERE ((1 = 0))");
        }

        [Fact]
        public void PassNullValueInCollectionAsParameter()
        {
            // Standard SQL behavior: "column IN (NULL)" never matches rows where column is NULL.
            var sequences = new List<string> { "of1", null };
            var query = Query<Document>().Where(d => sequences.Contains(d.NumberingSequence));

            AreSame(query, SelectDocuments + " WHERE ((NumberingSequence IN (@1, @2)))", "of1", null);
        }

        [Fact]
        public void KeepTranslatingStringContainsToLike()
        {
            var query = Query<Document>().Where(d => d.NumberingSequence.Contains("of"));

            AreSame(query, SelectDocuments + " WHERE ((NumberingSequence LIKE '%' + @1 + '%'))", "of");
        }

        [Alias("Documents")]
        public class Document
        {
            public int Id { get; set; }

            public string NumberingSequence { get; set; }
        }
    }
}
