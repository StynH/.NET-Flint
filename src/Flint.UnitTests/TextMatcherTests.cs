using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Flint;
using Flint.TextProcessing;

namespace Flint.UnitTests;

[TestClass]
public class TextMatcherTests
{
    private static readonly string[] SingleA = new[] { "a" };
    private static readonly string[] SingleX = new[] { "x" };

    private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. ";
    private const string LoremWithJedi = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna Jedi.";
    private const string Darth = "I thought not. It's not a story the Jedi would tell you. It's a Sith legend. Darth Plagueis was a Dark Lord of the Sith, so powerful and so wise he could use the Force to influence the midichlorians to create life. He had such a knowledge of the dark side that he could even keep the ones he cared about from dying.";

    [TestMethod]
    public void Given_NullPatterns_When_Creating_Then_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TextMatcher((IEnumerable<string>)null!));
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithNullText_Then_ThrowsArgumentNullException()
    {
        var matcher = new TextMatcher(SingleA);
        Assert.Throws<ArgumentNullException>(() => matcher.Find(null!));
    }

    [TestMethod]
    public void Given_EmptyPatterns_When_Find_Then_ReturnsEmpty()
    {
        var matcher = new TextMatcher(Array.Empty<string>());

        var results = matcher.Find("any text");

        Assert.IsNotNull(results);
        Assert.IsFalse(results.Any());
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithNoMatches_Then_ReturnsEmpty()
    {
        var matcher = new TextMatcher(SingleX);

        var results = matcher.Find("abc");

        Assert.IsNotNull(results);
        Assert.IsFalse(results.Any());
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithOverlappingMatches_Then_ReturnsAllMatches_OrderPreserved()
    {
        var patterns = Substitute.For<IEnumerable<string>>();
        patterns.GetEnumerator().Returns(_ => new List<string> { "a", "aa" }.GetEnumerator());

        var matcher = new TextMatcher(patterns);

        var matches = matcher.Find("aa").ToList();

        Assert.HasCount<Match>(3, matches);

        Assert.AreEqual(0, matches[0].startIndex);
        Assert.AreEqual(0, matches[0].endIndex);
        Assert.AreEqual("a", matches[0].match);

        Assert.AreEqual(0, matches[1].startIndex);
        Assert.AreEqual(1, matches[1].endIndex);
        Assert.AreEqual("aa", matches[1].match);

        Assert.AreEqual(1, matches[2].startIndex);
        Assert.AreEqual(1, matches[2].endIndex);
        Assert.AreEqual("a", matches[2].match);
    }

    [TestMethod]
    public void Given_Patterns_When_FindMultipleTextsUsingFindAll_Then_ReturnsMapping_ForOneText()
    {
        var patterns = new[] { "Lorem", "elit", "magna" };
        var matcher = new TextMatcher(patterns);

        var texts = new[] { Lorem, Darth };

        var map = matcher.FindAll(texts);

        Assert.IsNotNull(map);
        Assert.IsTrue(map.ContainsKey(Lorem));
        Assert.IsTrue(map.ContainsKey(Darth));

        var matches = map[Lorem].ToList();
        foreach (var pattern in patterns)
        {
            var expectedIndex = Lorem.IndexOf(pattern, StringComparison.Ordinal);
            Assert.IsTrue(matches.Any(m => m.match == pattern && m.startIndex == expectedIndex && m.endIndex == expectedIndex + pattern.Length - 1));
        }

        var darthMatches = map[Darth].ToList();
        Assert.HasCount<Match>(0, darthMatches);
    }

    [TestMethod]
    public void Given_Patterns_When_FindMultipleTextsUsingFindAll_Then_ReturnsMapping_ForEachText()
    {
        var patterns = new[] { "Jedi" };
        var matcher = new TextMatcher(patterns);

        var texts = new[] { LoremWithJedi, Darth };

        var map = matcher.FindAll(texts);

        Assert.IsNotNull(map);

        foreach (var text in texts)
        {
            Assert.IsTrue(map.ContainsKey(text));
            var matches = map[text].ToList();

            foreach (var pattern in patterns)
            {
                var expectedIndex = text.IndexOf(pattern, StringComparison.Ordinal);
                Assert.IsGreaterThanOrEqualTo(0, expectedIndex);
                Assert.IsTrue(matches.Any(m => m.match == pattern && m.startIndex == expectedIndex && m.endIndex == expectedIndex + pattern.Length - 1));
            }
        }
    }
}
