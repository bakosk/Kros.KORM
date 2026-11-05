using Kros.KORM.Metadata.Attribute;
using System;
using System.Linq;
using Xunit;

namespace Kros.KORM.UnitTests.Query.Sql
{
    /// <summary>
    /// Translation of boolean values which do not depend on entity (local variables, static properties, literals)
    /// and conditional (ternary) operator with condition which does not depend on entity.
    /// </summary>
    public class LinqBooleanConstantTranslatorShould : LinqTranslatorTestBase
    {
        private const string SelectDocuments = "SELECT Id, IsDeleted, NumberingSequence FROM Documents";

        public static bool StaticFlag { get; set; } = true;

        [Theory]
        [InlineData(true, "(1 = 1)")]
        [InlineData(false, "(1 = 0)")]
        public void TranslateLocalBoolVariableInOrCondition(bool allowed, string expectedCondition)
        {
            var query = Query<Document>().Where(d => allowed || d.Id > 5);

            AreSame(query, SelectDocuments + $" WHERE (({expectedCondition} OR (Id > @1)))", 5);
        }

        [Theory]
        [InlineData(true, "(1 = 1)")]
        [InlineData(false, "(1 = 0)")]
        public void TranslateLocalBoolVariableInAndCondition(bool allowed, string expectedCondition)
        {
            var query = Query<Document>().Where(d => d.Id > 5 && allowed);

            AreSame(query, SelectDocuments + $" WHERE (((Id > @1) AND {expectedCondition}))", 5);
        }

        [Fact]
        public void TranslateStaticBoolPropertyInOrCondition()
        {
            var query = Query<Document>().Where(d => StaticFlag || d.Id > 5);

            AreSame(query, SelectDocuments + " WHERE (((1 = 1) OR (Id > @1)))", 5);
        }

        [Fact]
        public void TranslateNegatedLocalBoolVariable()
        {
            bool allowed = true;
            var query = Query<Document>().Where(d => !allowed || d.Id > 5);

            AreSame(query, SelectDocuments + " WHERE (((1 = 0) OR (Id > @1)))", 5);
        }

        [Fact]
        public void TranslateTrueLiteralAsWholeCondition()
        {
            var query = Query<Document>().Where(d => true);

            AreSame(query, SelectDocuments + " WHERE ((1 = 1))");
        }

        [Theory]
        [InlineData(true, "(1 = 1)")]
        [InlineData(false, "(1 = 0)")]
        public void TranslateLocalBoolVariableAsWholeCondition(bool allowed, string expectedCondition)
        {
            var query = Query<Document>().Where(d => allowed);

            AreSame(query, SelectDocuments + $" WHERE ({expectedCondition})");
        }

        [Fact]
        public void KeepTranslatingComparisonOfBoolColumnWithLocalVariable()
        {
            bool deleted = true;
            var query = Query<Document>().Where(d => d.IsDeleted == deleted);

            AreSame(query, SelectDocuments + " WHERE ((IsDeleted = 1))");
        }

        [Fact]
        public void TranslateConditionalWithLocalConditionToTrueBranch()
        {
            bool allowed = true;
            var query = Query<Document>().Where(d => allowed ? true : d.Id > 5);

            AreSame(query, SelectDocuments + " WHERE ((1 = 1))");
        }

        [Fact]
        public void TranslateConditionalWithLocalConditionToFalseBranch()
        {
            bool allowed = false;
            var query = Query<Document>().Where(d => allowed ? true : d.Id > 5);

            AreSame(query, SelectDocuments + " WHERE ((Id > @1))", 5);
        }

        [Fact]
        public void TranslateConditionalWithLocalConditionToBoolColumnBranch()
        {
            bool onlyDeleted = true;
            var query = Query<Document>().Where(d => onlyDeleted ? d.IsDeleted : d.Id > 5);

            AreSame(query, SelectDocuments + " WHERE ((IsDeleted = 1))");
        }

        [Fact]
        public void TranslateConditionalWithLocalConditionInsideOrCondition()
        {
            bool allowed = false;
            var query = Query<Document>().Where(d => d.NumberingSequence == "of1" || (allowed ? true : d.Id > 5));

            AreSame(query, SelectDocuments + " WHERE (((NumberingSequence = @1) OR (Id > @2)))", "of1", 5);
        }

        [Fact]
        public void TranslateConditionalWithLocalConditionInComparison()
        {
            bool useLower = true;
            var query = Query<Document>()
                .Where(d => (useLower ? d.NumberingSequence.ToLower() : d.NumberingSequence) == "of1");

            AreSame(query, SelectDocuments + " WHERE ((LOWER(NumberingSequence) = @1))", "of1");
        }

        [Fact]
        public void ThrowNotSupportedExceptionForConditionalDependentOnEntity()
        {
            var query = Query<Document>().Where(d => d.Id > 5 ? d.IsDeleted : true);

            Assert.Throws<NotSupportedException>(() => AreSame(query, SelectDocuments));
        }

        [Alias("Documents")]
        public class Document
        {
            public int Id { get; set; }

            public bool IsDeleted { get; set; }

            public string NumberingSequence { get; set; }
        }
    }
}
