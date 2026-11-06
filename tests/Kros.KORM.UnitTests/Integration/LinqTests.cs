using Kros.KORM.UnitTests.Base;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Integration
{
    public class LinqTests(KormTestsFixture kormContext) : DatabaseTestBase(kormContext)
    {
        #region Nested Classes

        private class Foo
        {
            public int Id { get; set; }
        }

        private class Document
        {
            public int Id { get; set; }

            public string NumberingSequence { get; set; }
        }

        #endregion

        #region SQL Scripts

        private static readonly string CreateTable_FooTable =
$@"CREATE TABLE [dbo].[Foo] (
    [Id] [int] NOT NULL
) ON [PRIMARY];";

        private static readonly string InsertIntoFooScript =
$@"INSERT INTO [Foo] VALUES (1);
INSERT INTO [Foo] VALUES (2);
INSERT INTO [Foo] VALUES (3);
INSERT INTO [Foo] VALUES (4);";

        private static readonly string CreateTable_DocumentTable =
$@"CREATE TABLE [dbo].[Document] (
    [Id] [int] NOT NULL,
    [NumberingSequence] [nvarchar](10) NULL
) ON [PRIMARY];";

        private static readonly string InsertIntoDocumentScript =
$@"INSERT INTO [Document] VALUES (1, 'xx');
INSERT INTO [Document] VALUES (2, 'of1');
INSERT INTO [Document] VALUES (3, 'of2');
INSERT INTO [Document] VALUES (4, 'of3');
INSERT INTO [Document] VALUES (5, NULL);";

        #endregion

        [Fact]
        public void QueryShouldReturnZeroWhenSumEmptyResultSet()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            {
                var sum = korm.Query<Foo>()
                    .Where(p => p.Id == -1)
                    .Sum(p => p.Id);

                Assert.Equal(0, sum);
            }
        }

        [Fact]
        public void QueryShouldFilterByCollectionContains()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_DocumentTable, InsertIntoDocumentScript))
            {
                var sequences = new List<string> { "of1", "of2", null };
                var ids = korm.Query<Document>()
                    .Where(d => sequences.Contains(d.NumberingSequence))
                    .OrderBy(d => d.Id)
                    .ToList()
                    .Select(d => d.Id);

                Assert.Equal([2, 3], ids);
            }
        }

        [Fact]
        public void QueryShouldFilterByArrayContainsAndNotContains()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_DocumentTable, InsertIntoDocumentScript))
            {
                int[] ids = [2, 4];

                Assert.Equal([2, 4], korm.Query<Document>().Where(d => ids.Contains(d.Id)).OrderBy(d => d.Id).ToList().Select(d => d.Id));
                Assert.Equal([1, 3, 5], korm.Query<Document>().Where(d => !ids.Contains(d.Id)).OrderBy(d => d.Id).ToList().Select(d => d.Id));
            }
        }

        [Fact]
        public void QueryShouldReturnNothingWhenContainsCollectionIsEmpty()
        {
            using (IDatabase korm = CreateDatabase(CreateTable_DocumentTable, InsertIntoDocumentScript))
            {
                var ids = new List<int>();

                Assert.Empty(korm.Query<Document>().Where(d => ids.Contains(d.Id)).ToList());
            }
        }
    }
}
