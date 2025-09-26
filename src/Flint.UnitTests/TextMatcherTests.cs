using NSubstitute;

namespace Flint.UnitTests;

[TestClass]
public class TextMatcherTests
{
    private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ";
    private const string LoremWithJedi = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna Jedi.";
    private const string Darth = "I thought not. It's not a story the Jedi would tell you. It's a Sith legend. Darth Plagueis was a Dark Lord of the Sith, so powerful and so wise he could use the Force to influence the midichlorians to create life. He had such a knowledge of the dark side that he could even keep the ones he cared about from dying.";

    [TestMethod]
    public void Given_NullPatterns_When_Creating_Then_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TextMatcher(null!));
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithNullText_Then_ThrowsArgumentNullException()
    {
        var Matcher = new TextMatcher(["a"]);
        Assert.Throws<ArgumentNullException>(() => Matcher.Find(null!));
    }

    [TestMethod]
    public void Given_EmptyPatterns_When_Find_Then_ReturnsEmpty()
    {
        var Matcher = new TextMatcher([]);

        var results = Matcher.Find("any text");

        Assert.IsNotNull(results);
        Assert.IsFalse(results.Any());
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithNoValues_Then_ReturnsEmpty()
    {
        var Matcher = new TextMatcher(["x"]);

        var results = Matcher.Find("abc");

        Assert.IsNotNull(results);
        Assert.IsFalse(results.Any());
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithOverlappingValues_Then_ReturnsAllValues_OrderPreserved()
    {
        var patterns = Substitute.For<IEnumerable<string>>();
        patterns.GetEnumerator().Returns(_ => new List<string> { "a", "aa" }.GetEnumerator());

        var Matcher = new TextMatcher(patterns);

        var Values = Matcher.Find("aa").ToList();

        Assert.HasCount(3, Values);

        Assert.AreEqual(0, Values[0].StartIndex);
        Assert.AreEqual(0, Values[0].EndIndex);
        Assert.AreEqual("a", Values[0].Value);

        Assert.AreEqual(0, Values[1].StartIndex);
        Assert.AreEqual(1, Values[1].EndIndex);
        Assert.AreEqual("aa", Values[1].Value);

        Assert.AreEqual(1, Values[2].StartIndex);
        Assert.AreEqual(1, Values[2].EndIndex);
        Assert.AreEqual("a", Values[2].Value);
    }

    [TestMethod]
    public void Given_Patterns_When_FindMultipleTextsUsingFindAll_Then_ReturnsMapping_ForOneText()
    {
        var patterns = new[] { "Lorem", "elit", "magna" };
        var Matcher = new TextMatcher(patterns);

        var texts = new[] { Lorem, Darth };

        var map = Matcher.FindAll(texts);

        Assert.IsNotNull(map);
        Assert.IsTrue(map.ContainsKey(Lorem));
        Assert.IsTrue(map.ContainsKey(Darth));

        var Values = map[Lorem].ToList();
        foreach (var pattern in patterns)
        {
            var expectedIndex = Lorem.IndexOf(pattern, StringComparison.Ordinal);
            Assert.IsTrue(Values.Any(m => m.Value == pattern && m.StartIndex == expectedIndex && m.EndIndex == expectedIndex + pattern.Length - 1));
        }

        var darthValues = map[Darth].ToList();
        Assert.HasCount(0, darthValues);
    }

    [TestMethod]
    public void Given_Patterns_When_FindMultipleTextsUsingFindAll_Then_ReturnsMapping_ForEachText()
    {
        var patterns = new[] { "Jedi" };
        var Matcher = new TextMatcher(patterns);

        var texts = new[] { LoremWithJedi, Darth };

        var map = Matcher.FindAll(texts);

        Assert.IsNotNull(map);

        foreach (var text in texts)
        {
            Assert.IsTrue(map.ContainsKey(text));
            var Values = map[text].ToList();

            foreach (var pattern in patterns)
            {
                var expectedIndex = text.IndexOf(pattern, StringComparison.Ordinal);
                Assert.IsGreaterThanOrEqualTo(0, expectedIndex);
                Assert.IsTrue(Values.Any(m => m.Value == pattern && m.StartIndex == expectedIndex && m.EndIndex == expectedIndex + pattern.Length - 1));
            }
        }
    }

    [TestMethod]
    public void Given_Patterns_When_ReplaceWithNullText_Then_ThrowsArgumentNullException()
    {
        var matcher = new TextMatcher(["a"]);
        Assert.Throws<ArgumentNullException>(() => matcher.Replace(null!, "x"));
    }

    [TestMethod]
    public void Given_Patterns_When_ReplaceWithNullReplacement_Then_ThrowsArgumentNullException()
    {
        var matcher = new TextMatcher(["a"]);
        Assert.Throws<ArgumentNullException>(() => matcher.Replace("abc", (string)null!));
    }

    [TestMethod]
    public void Given_Patterns_When_ReplaceWithNullProvider_Then_ThrowsArgumentNullException()
    {
        var matcher = new TextMatcher(["a"]);
        Assert.Throws<ArgumentNullException>(() => matcher.Replace("abc", (Func<Match, string>)null!));
    }

    [TestMethod]
    public void Given_Patterns_When_ReplaceSimple_Then_ReplacesAllOccurrences()
    {
        var matcher = new TextMatcher(["a"]);
        var result = matcher.Replace("banana", "x");
        Assert.AreEqual("bxnxnx", result);
    }

    [TestMethod]
    public void Given_Patterns_When_ReplaceWithProvider_TheN_UsesProviderForEachMatch()
    {
        var patterns = new[] { "Jedi" };
        var matcher = new TextMatcher(patterns);

        var result = matcher.Replace(LoremWithJedi, m => $"[{m.Value}]");

        Assert.Contains("[Jedi]", result);
    }

    [TestMethod]
    public void Given_OverlappingPatterns_When_Replace_Then_SkipsOverlappingMatches()
    {
        var patterns = Substitute.For<IEnumerable<string>>();
        patterns.GetEnumerator().Returns(_ => new List<string> { "a", "aa" }.GetEnumerator());

        var matcher = new TextMatcher(patterns);
        var result = matcher.Replace("aa", "-");

        Assert.AreEqual("--", result);
    }

    [TestMethod]
    public void Given_Replacements_When_CountDiffersFromPatterns_Then_ThrowsArgumentException()
    {
        var patterns = new[] { "a", "b" };
        var matcher = new TextMatcher(patterns);

        var shortReplacements = new[] { "x" };
        Assert.Throws<ArgumentException>(() => matcher.Replace("ab", shortReplacements));
    }

    [TestMethod]
    public void Given_Replacements_When_MappingByIndex_Then_ReplacesEachPattern()
    {
        var patterns = new[] { "Lorem", "elit", "magna" };
        var replacements = new[] { "EPIC", "TRUE", "REPLACEMENT" };
        var matcher = new TextMatcher(patterns);

        var result = matcher.Replace(Lorem, replacements);

        Assert.Contains("EPIC", result);
        Assert.DoesNotContain("Lorem", result);
        Assert.Contains("TRUE", result);
        Assert.DoesNotContain("elit", result);
        Assert.Contains("REPLACEMENT", result);
        Assert.DoesNotContain("magna", result);
    }

    [TestMethod]
    public void Given_OverlappingPatterns_When_ReplaceWithReplacements_UsesFirstMatchingPatternPerPosition()
    {
        var patterns = new[] { "a", "aa" };
        var replacements = new[] { "-", "=" };
        var matcher = new TextMatcher(patterns);

        var result = matcher.Replace("aa", replacements);

        Assert.AreEqual("--", result);
    }

    [TestMethod]
    public void Given_NoStringComparison_When_Find_Then_MatchesCase()
    {
        var matcher = new TextMatcher(["jedi"]);
        var results = matcher.Find(LoremWithJedi).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void Given_OrdinalIgnoreCase_When_Find_Then_MatchesIrrespectiveOfCase()
    {
        var matcher = new TextMatcher(["jedi"], StringComparison.OrdinalIgnoreCase);
        var results = matcher.Find(LoremWithJedi).ToList();

        Assert.IsGreaterThan(0, results.Count);
    }

    [TestMethod]
    public void Given_IgnoreCaseComparison_When_Replace_Then_ReplacesMatchesIrrespectiveOfCase()
    {
        var matcher = new TextMatcher(["jedi"], StringComparison.CurrentCultureIgnoreCase);

        var replaced = matcher.Replace(LoremWithJedi, "[X]");

        Assert.Contains("[X]", replaced);
        Assert.DoesNotContain("Jedi", replaced);
    }
}
