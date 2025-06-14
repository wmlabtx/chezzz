using Chezzz.Models;

namespace ChezzzTests;

[TestClass]
public class ScoreTests
{
    [TestMethod]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var score1 = new Score(100, false);
        var score2 = new Score(3, true);

        // Assert
        Assert.AreEqual(100, score1.Value);
        Assert.IsFalse(score1.IsMate);

        Assert.AreEqual(3, score2.Value);
        Assert.IsTrue(score2.IsMate);
    }

    [TestMethod]
    public void ToString_NonMatePositiveScore_ReturnsFormattedString()
    {
        // Arrange
        var score = new Score(145, false);

        // Act
        var result = score.ToString();

        // Assert
        Assert.AreEqual("+1.45", result);
    }

    [TestMethod]
    public void ToString_NonMateNegativeScore_ReturnsFormattedString()
    {
        // Arrange
        var score = new Score(-275, false);

        // Act
        var result = score.ToString();

        // Assert
        Assert.AreEqual("-2.75", result);
    }

    [TestMethod]
    public void ToString_MatePositiveScore_ReturnsFormattedString()
    {
        // Arrange
        var score = new Score(4, true);

        // Act
        var result = score.ToString();

        // Assert
        Assert.AreEqual("+M4", result);
    }

    [TestMethod]
    public void ToString_MateNegativeScore_ReturnsFormattedString()
    {
        // Arrange
        var score = new Score(-5, true);

        // Act
        var result = score.ToString();

        // Assert
        Assert.AreEqual("-M5", result);
    }

    [TestMethod]
    public void CompareTo_NullOther_Returns1()
    {
        // Arrange
        var score = new Score(100, false);

        // Act
        var result = ((IComparable<Score>)score).CompareTo(null);

        // Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void CompareTo_BothNonMate_ComparesValues()
    {
        // Arrange
        var score1 = new Score(100, false);
        var score2 = new Score(200, false);
        var score3 = new Score(100, false);

        // Act
        var result1 = ((IComparable<Score>)score1).CompareTo(score2);
        var result2 = ((IComparable<Score>)score2).CompareTo(score1);
        var result3 = ((IComparable<Score>)score1).CompareTo(score3);

        // Assert
        Assert.IsTrue(result1 < 0);
        Assert.IsTrue(result2 > 0);
        Assert.AreEqual(0, result3);
    }

    [TestMethod]
    public void CompareTo_PositiveMateVsNonMate_MateWins()
    {
        // Arrange
        var mate = new Score(3, true);
        var nonMate = new Score(1000, false);

        // Act
        var result = ((IComparable<Score>)mate).CompareTo(nonMate);

        // Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void CompareTo_NegativeMateVsNonMate_MateLoses()
    {
        // Arrange
        var mate = new Score(-3, true);
        var nonMate = new Score(-1000, false);

        // Act
        var result = ((IComparable<Score>)mate).CompareTo(nonMate);

        // Assert
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void CompareTo_BothPositiveMate_ShorterMateWins()
    {
        // Arrange
        var mate1 = new Score(2, true);
        var mate2 = new Score(5, true);

        // Act
        var result = ((IComparable<Score>)mate1).CompareTo(mate2);

        // Assert
        Assert.IsTrue(result > 0); // Shorter mate (2) is better than longer mate (5)
    }

    [TestMethod]
    public void CompareTo_PositiveMateVsNegativeMate_PositiveWins()
    {
        // Arrange
        var positiveMate = new Score(10, true);
        var negativeMate = new Score(-3, true);

        // Act
        var result = ((IComparable<Score>)positiveMate).CompareTo(negativeMate);

        // Assert
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void CompareTo_NegativeMateVsPositiveMate_NegativeLoses()
    {
        // Arrange
        var negativeMate = new Score(-4, true);
        var positiveMate = new Score(7, true);

        // Act
        var result = ((IComparable<Score>)negativeMate).CompareTo(positiveMate);

        // Assert
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void CompareTo_BothNegativeMate_LongerMateWins()
    {
        // Arrange
        var mate1 = new Score(-2, true);
        var mate2 = new Score(-5, true);

        // Act
        var result = ((IComparable<Score>)mate1).CompareTo(mate2);

        // Assert
        Assert.IsTrue(result < 0); // For negative mates, shorter mate (-2) is worse than longer mate (-5)
    }

    [TestMethod]
    public void Sorting_MixedScores_SortsCorrectly()
    {
        // Arrange
        var scores = new List<Score>
        {
            new(100, false),     // +1.00
            new(-200, false),    // -2.00
            new(5, true),        // +M5
            new(2, true),        // +M2
            new(-4, true),       // -M4
            new(-1, true),       // -M1
            new(0, false),       // 0.00
            new(500, false),     // +5.00
        };

        // Expected order (worst to best):
        // -M1, -M4, -2.00, 0.00, +1.00, +5.00, +M5, +M2

        // Act
        scores.Sort();

        // Assert
        Assert.AreEqual(-1, scores[0].Value);
        Assert.IsTrue(scores[0].IsMate);

        Assert.AreEqual(-4, scores[1].Value);
        Assert.IsTrue(scores[1].IsMate);

        Assert.AreEqual(-200, scores[2].Value);
        Assert.IsFalse(scores[2].IsMate);

        Assert.AreEqual(0, scores[3].Value);
        Assert.IsFalse(scores[3].IsMate);

        Assert.AreEqual(100, scores[4].Value);
        Assert.IsFalse(scores[4].IsMate);

        Assert.AreEqual(500, scores[5].Value);
        Assert.IsFalse(scores[5].IsMate);

        Assert.AreEqual(5, scores[6].Value);
        Assert.IsTrue(scores[6].IsMate);

        Assert.AreEqual(2, scores[7].Value);
        Assert.IsTrue(scores[7].IsMate);
    }

    [TestMethod]
    public void OperatorMinus_SubtractsScores_Correctly()
    {
        // Arrange
        var score1 = new Score(100, false);
        var score2 = new Score(50, false);
        // Act
        var result = score1 - score2;
        // Assert
        Assert.AreEqual(50, result.Value);
    }

    [TestMethod]
    public void OperatorMinus_SubtractsMateScores_Correctly()
    {
        // Arrange
        var score1 = new Score(3, true);
        var score2 = new Score(1, true);
        // Act
        var result = score1 - score2;
        // Assert
        Assert.AreEqual(-2, result.Value);
    }

    [TestMethod]
    public void OperatorMinus_SubtractsMixedScores_Correctly()
    {
        // Arrange
        var score1 = new Score(100, false);
        var score2 = new Score(5, true);
        // Act
        var result = score1 - score2;
        // Assert
        Assert.AreEqual(-10000, result.Value);
    }
}