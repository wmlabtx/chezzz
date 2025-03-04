namespace ChezzzTests.San;

[TestClass]
public class BoardTests
{
    /// <summary>
    /// Tests the correct placement and FEN representation of kings on an empty board
    /// </summary>
    [TestMethod]
    public void Board_WithOnlyKings_GeneratesCorrectFen()
    {
        // Arrange
        const string expectedFen = "4k3/8/8/8/8/8/8/4K3 w - - 0 1";
        var board = new Chezzz.San.Board();
        var whiteKingPosition = new Chezzz.San.Position("e1");
        var blackKingPosition = new Chezzz.San.Position("e8");

        // Act
        board.PutPiece(whiteKingPosition, new Chezzz.San.Piece('w', 'k'));
        board.PutPiece(blackKingPosition, new Chezzz.San.Piece('b', 'k'));

        // Assert
        Assert.IsNotNull(board.GetPiece(whiteKingPosition), "White king should be present on e1");
        Assert.IsNotNull(board.GetPiece(blackKingPosition), "Black king should be present on e8");
        var fen = board.ToFen(true);
        Assert.AreEqual(expectedFen, fen, "Generated FEN string should match expected value");
    }


    /// <summary>
    /// Verifies that a newly initialized chess board has the correct starting position
    /// and generates the standard FEN notation for the initial game state
    /// </summary>
    [TestMethod]
    public void Board_WhenInitialized_HasCorrectStartingPosition()
    {
        // Arrange
        const string expectedInitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var board = new Chezzz.San.Board();

        // Act
        board.StartGame();

        // Assert
        var actualFen = board.ToFen();
        Assert.AreEqual(expectedInitialFen, actualFen,
            "Initial board position should match standard chess starting position");
    }

    /// <summary>
    /// Tests basic pawn movement validation and board state after moves
    /// </summary>
    [TestMethod]
    public void Move_ValidPawnMove_UpdatesBoardStateCorrectly()
    {
        // Arrange
        var board = new Chezzz.San.Board();
        board.StartGame();
        const string expectedFen = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

        // Act
        var moveSuccessful = board.Move("e4");

        // Assert
        Assert.IsTrue(moveSuccessful, "Valid move e4 should be accepted");
        Assert.AreEqual(expectedFen, board.ToFen(),
            "Board FEN should reflect e4 pawn move");
    }

    /// <summary>
    /// Tests invalid move handling
    /// </summary>
    [TestMethod]
    public void Move_InvalidMove_ReturnsFalse()
    {
        // Arrange
        var board = new Chezzz.San.Board();
        board.StartGame();

        // Act
        var moveSuccessful = board.Move("e5"); // Invalid as white's first move

        // Assert
        Assert.IsFalse(moveSuccessful, "Invalid move e5 should be rejected");
        Assert.AreEqual("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
            board.ToFen(),
            "Board should remain in initial position after invalid move");
    }

    /// <summary>
    /// Parses chess moves from PGN format, filtering out move numbers
    /// </summary>
    private static string[] GetMoves(string pgn)
    {
        return pgn.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(move => !move.Contains('.')) // Filter move numbers and check annotations
            .ToArray();
    }

    /// <summary>
    /// Tests a complete chess game sequence, verifying FEN positions at each move
    /// </summary>
    [TestMethod]
    public void PlayGame_CompleteGameSequence_GeneratesCorrectPositionsAtEachMove()
    {
        const string pgn =
            @"1. e4 e5 2. Nf3 Nf6 3. Nxe5 Nc6 4. d4 Nxe5 5. dxe5 Nxe4 6. Qe2 Ng5 7. f4 Ne6 8. Nc3 Bc5 9. Bd2 Nd4 " +
            "10. Qe4 Qh4+ 11. g3 Qg4 12. Be2 Nxe2 13. Nxe2 Qe6 14. f5 Qb6 15. Rf1 Qxb2 16. Bc3 Qb6 17. Qh4 Qc6 18. e6 dxe6 19. Bxg7 Rg8 " +
            "20. Qxh7 Rxg7 21. Qxg7 Bb4+ 22. c3 Bf8 23. Qg5 exf5";

        var fens = new[] {
            "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", // e4
            "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2", // e5
            "rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2", // Nf3
            "rnbqkb1r/pppp1ppp/5n2/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R w KQkq - 2 3", // Nf6
            "rnbqkb1r/pppp1ppp/5n2/4N3/4P3/8/PPPP1PPP/RNBQKB1R b KQkq - 0 3", // Nxe5
            "r1bqkb1r/pppp1ppp/2n2n2/4N3/4P3/8/PPPP1PPP/RNBQKB1R w KQkq - 1 4", // Nc6
            "r1bqkb1r/pppp1ppp/2n2n2/4N3/3PP3/8/PPP2PPP/RNBQKB1R b KQkq d3 0 4", // d4
            "r1bqkb1r/pppp1ppp/5n2/4n3/3PP3/8/PPP2PPP/RNBQKB1R w KQkq - 0 5", // Nxe5
            "r1bqkb1r/pppp1ppp/5n2/4P3/4P3/8/PPP2PPP/RNBQKB1R b KQkq - 0 5", // dxe5
            "r1bqkb1r/pppp1ppp/8/4P3/4n3/8/PPP2PPP/RNBQKB1R w KQkq - 0 6", // Nxe4
            "r1bqkb1r/pppp1ppp/8/4P3/4n3/8/PPP1QPPP/RNB1KB1R b KQkq - 1 6", // Qe2
            "r1bqkb1r/pppp1ppp/8/4P1n1/8/8/PPP1QPPP/RNB1KB1R w KQkq - 2 7", // Ng5
            "r1bqkb1r/pppp1ppp/8/4P1n1/5P2/8/PPP1Q1PP/RNB1KB1R b KQkq f3 0 7", // f4
            "r1bqkb1r/pppp1ppp/4n3/4P3/5P2/8/PPP1Q1PP/RNB1KB1R w KQkq - 1 8", // Ne6
            "r1bqkb1r/pppp1ppp/4n3/4P3/5P2/2N5/PPP1Q1PP/R1B1KB1R b KQkq - 2 8", // Nc3
            "r1bqk2r/pppp1ppp/4n3/2b1P3/5P2/2N5/PPP1Q1PP/R1B1KB1R w KQkq - 3 9", // Bc5
            "r1bqk2r/pppp1ppp/4n3/2b1P3/5P2/2N5/PPPBQ1PP/R3KB1R b KQkq - 4 9", // Bd2
            "r1bqk2r/pppp1ppp/8/2b1P3/3n1P2/2N5/PPPBQ1PP/R3KB1R w KQkq - 5 10", // Nd4
            "r1bqk2r/pppp1ppp/8/2b1P3/3nQP2/2N5/PPPB2PP/R3KB1R b KQkq - 6 10", // Qe4
            "r1b1k2r/pppp1ppp/8/2b1P3/3nQP1q/2N5/PPPB2PP/R3KB1R w KQkq - 7 11" // Qh4+
        };

        const string expectedFinalPosition = "r1b1kb2/ppp2p2/2q5/5pQ1/8/2P3P1/P3N2P/R3KR2 w Qq - 0 24";

        // Arrange
        var board = new Chezzz.San.Board();
        board.StartGame();

        // Act & Assert
        var moves = GetMoves(pgn);

        for (var i = 0; i < moves.Length; i++) {
            var move = moves[i];
            Assert.IsTrue(board.Move(move), $"Move {i + 1} ({move}) should be valid");

            if (i < fens.Length) {
                var expectedFen = fens[i];
                Assert.AreEqual(expectedFen, board.ToFen(),
                    $"Position after move {i + 1} ({move}) does not match expected FEN");
            }
        }

        // Verify final position
        Assert.AreEqual(expectedFinalPosition, board.ToFen(),
            "Final position does not match expected FEN");
    }

    /// <summary>
    /// Tests multiple complete chess games, verifying that each game reaches its expected final position.
    /// Each game is played through its entire sequence of moves and the final board state is compared
    /// against a known FEN string representation.
    /// </summary>
    [TestMethod]
    public void PlayGames_CompleteGameSequences_GenerateCorrectFinalPositions()
    {
        var pgns = new[] {
            "1. e4 c6 2. d4 d5 3. exd5 cxd5 4. Nc3 Nf6 5. Bb5+ Nc6 6. Bg5 Qa5 7. Bxf6 exf6 8. Qe2+ Be6 9. O-O-O Bd6 " + 
            "10. g3 Rc8 11. f4 O-O 12. Nf3 Rc7 13. Nh4 h5 14. f5 Nb4 15. a3 Bxf5 16. Nxf5 Rxc3 17. Nxd6 Rxc2+ 18. Qxc2 Nxc2 19. Kxc2 g5 " + 
            "20. a4 a6 21. Nxb7 Qb6 22. Nc5 axb5 23. Kb1 Qb8 24. Nd7 Qd6 25. Nxf8 bxa4 26. Rd3 Kxf8 27. Ra3 Qd8 28. Rxa4 Kg7 29. Ra3 Qe7 " + 
            "30. Ka2 g4 31. h4 Qe2 32. Rc1 f5 33. Rc5 Kf6 34. Rxd5 Qe3 35. Rxe3",

            "1. e4 c5 2. Nf3 e6 3. d4 cxd4 4. Nxd4 Nc6 5. Be3 Nf6 6. f3 e5 7. Nb3 b6 8. h4 Be7 9. Nc3 Bb7 " +
            "10. Qd2 Bb4 11. a3 Bxc3 12. Qxc3 Nh5 13. O-O-O O-O 14. f4 Nxf4 15. Rh2 Qe7 16. Bxf4 exf4 17. Qf3 Qf6 18. Qh5 Rad8 19. Be2 Ne5 " +
            "20. Qf5 Qxf5 21. exf5 f3 22. Bb5 fxg2 23. Rxg2 Bxg2 24. f6 Bc6 25. Ba6 d5 26. Nd4 Rd6 27. fxg7 Rfd8 28. Nf5 Re6 29. b4 Ng6 " +
            "30. b5 Ba8 31. h5 Ne7 32. Nd4 Rh6 33. Re1 Nc8 34. Bxc8 Rxc8 35. Nf5 Rxh5 36. Ne7+ Kxg7 37. Nxc8 d4 38. Nxa7 Rc5 39. Re8 Bf3 " +
            "40. Nc8 Rxb5 41. a4 Rb4 42. Re5 Kf6 43. Re1 Bh5 44. c3 dxc3 45. Kc2 Rxa4 46. Nxb6 Bg6+ 47. Kxc3 Ra3+ 48. Kd2 Kg7 49. Re5 Rd3+ " +
            "50. Ke1 Rd6 51. Rb5 Re6+ 52. Kf2 Rf6+ 53. Kg3 h5 54. Nd7 Rf5 55. Rxf5 Bxf5 56. Nb8 Bg4 57. Kf4 f5 58. Kg3 Kg6 59. Kf4 h4 " +
            "60. Nd7 h3 61. Ne5+ Kh5 62. Kg3 f4+ 63. Kxf4 h2 64. Nxg4 h1=Q 65. Ne5 Qh4+ 66. Kf5 Qg5+ 67. Ke6 Qg8+ 68. Kd6 Qf8+ 69. Kd5 Kg5 " +
            "70. Nc4 Qf3+ 71. Ke6 Qe2+ 72. Ne5 Kf4",

            "1. e4 e5 2. Nf3 Nf6 3. Nc3 Bc5 4. Bd3 d6 5. Nd5 Nxd5 6. exd5 O-O 7. O-O f5 8. Be2 e4 9. Ne1 f4 " +
            "10. Kh1 Qh4 11. d4 Rf6 12. Bxf4 Qxf4 13. dxc5 dxc5 14. Rc1 Nd7 15. g3 Qh6 16. f3 Ne5 17. f4 Qh3 18. Kg1 Ng4 19. Bxg4 Bxg4 " +
            "20. Qd2 Re8 21. Qe3 c4 22. b3 Qh5 23. Rf2 Qxd5 24. Ng2 cxb3 25. Qxb3 Be6 26. Qxd5 Bxd5 27. Ne3 Rd8 28. Rb1 b6 29. Rb4 Bb7 " +
            "30. Rc4 c5 31. g4 Rfd6 32. Rg2 Rd4 33. Rc3 Bc6 34. Rb3 Ra4 35. a3 Rad4 36. Re2 Bd7 37. Kg2 b5 38. Rb1 a5 39. Kg3 b4 " +
            "40. h4 Be8 41. g5 Bh5 42. Rh2 bxa3 43. f5 Rb4 44. Ra1 Ra4 45. Ra2 Bf7 46. Ra1 c4 47. h5 Be8 48. Kf4 c3 49. Rhh1 a2 " +
            "50. Ke5 h6 51. g6 Bb5 52. Rhd1 Rd2 53. f6 Bc4 54. f7+ Kf8 55. Rg1 Ba6 56. Rg4 Bb7 57. Rg2 Bc6 58. Kf5 Ke7 59. Rgg1 Rb4 " +
            "60. Rgf1 Bb5 61. Rh1 Bd7+ 62. Ke5 Rb5+ 63. Kf4 Rf2+ 64. Kg3 Rf3+ 65. Kh2 Rxe3 66. Rhf1 Rh3+ 67. Kg2 Rg5+ 68. Kf2 Bb5 69. f8=Q+ Kxf8 " +
            "70. Rfb1 e3+ 71. Ke1 Rg1#"
        };

        var fens = new[] {
            "8/5p2/5k2/3R1p1p/3P2pP/4R1P1/KP6/8 b - - 0 35",
            "8/8/4K3/4N3/5k2/8/4q3/8 w - - 16 73",
            "5k2/6p1/6Pp/pb5P/8/2p1p2r/p1P5/RR2K1r1 w - - 2 72"
        };

        for (var i = 0; i < pgns.Length; i++) {
            var pgn = pgns[i];
            var expectedFen = fens[i];
            var board = new Chezzz.San.Board();
            board.StartGame();
            var moves = GetMoves(pgn);
            for (var j = 0; j < moves.Length; j++) {
                var move = moves[j];
                Assert.IsTrue(board.Move(move), $"Move {j + 1} ({move}) should be valid");
            }

            var fen = board.ToFen();
            Assert.AreEqual(expectedFen, fen, $"Game {i + 1} final position does not match expected FEN");
        }
    }
}