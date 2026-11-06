using Kros.KORM.UnitTests.Base;
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

        [Theory]
        [InlineData(true, 4)]
        [InlineData(false, 1)]
        public void QueryShouldTranslateExternalBoolFlag(bool allowAll, int expectedCount)
        {
            using (IDatabase korm = CreateDatabase(CreateTable_FooTable, InsertIntoFooScript))
            {
                Assert.Equal(expectedCount, korm.Query<Foo>().Where(p => allowAll || p.Id == 1).ToList().Count);
                Assert.Equal(expectedCount, korm.Query<Foo>().Where(p => allowAll ? true : p.Id == 1).ToList().Count);
            }
        }
    }
}
