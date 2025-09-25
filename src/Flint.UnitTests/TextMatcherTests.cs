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
        var Matcher = new TextMatcher(SingleA);
        Assert.Throws<ArgumentNullException>(() => Matcher.Find(null!));
    }

    [TestMethod]
    public void Given_EmptyPatterns_When_Find_Then_ReturnsEmpty()
    {
        var Matcher = new TextMatcher(Array.Empty<string>());

        var results = Matcher.Find("any text");

        Assert.IsNotNull(results);
        Assert.IsFalse(results.Any());
    }

    [TestMethod]
    public void Given_Patterns_When_FindWithNoValues_Then_ReturnsEmpty()
    {
        var Matcher = new TextMatcher(SingleX);

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

        Assert.HasCount<Match>(3, Values);

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
        Assert.HasCount<Match>(0, darthValues);
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
}
