using Chess;

namespace ChezzzTests.San;

[TestClass]
public class BoardTests
{
    private static string[] GetMoves(string pgn)
    {
        return pgn.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(move => !move.Contains('.'))
            .ToArray();
    }

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
            "70. Rfb1 e3+ 71. Ke1 Rg1#",

            "1. e4 e6 2. d4 d5 3. e5 c5 4. c3 cxd4 5. cxd4 Bb4+ 6. Nc3 Nc6 7. Nf3 Nge7 8. Bd3 O-O 9. Bxh7+ Kxh7 10. Ng5+ Kg6 11. h4 Nxd4 12. Qg4 f5 13. h5+ Kh6 14. Nxe6+ g5 15. hxg6#",

            "1. d4 e6 2. Bf4 d5 3. e3 c5 4. Nc3 Nc6 5. Nb5 e5 6. dxe5 a6 7. Nd6+ Bxd6 8. exd6 Be6 9. Be2 Qb6 " +
            "10. Rb1 Nf6 11. Bf3 Qa5+ 12. c3 Qxa2 13. Ne2 h6 14. O-O g5 15. Bg3 g4 16. Nf4 gxf3 17. Nxe6 fxg2 18. Kxg2 fxe6 19. Bh4 Qc4 " +
            "20. Bxf6 Qe4+ 21. f3 Qg6+ 22. Kf2 Qxf6 23. Rg1 Qh4+ 24. Kf1 Qxh2 25. Qe2 Qh3+ 26. Ke1 Qh4+ 27. Kd2 Qf6 28. f4 O-O-O 29. Rg2 Rxd6 " +
            "30. Rbg1 d4 31. Rg6 dxc3+ 32. Ke1 cxb2 33. Rxf6 b1=Q+ 34. Kf2 Rg8 35. Rxg8+",

            "1. e4 c5 2. Nf3 Nc6 3. d4 cxd4 4. Nxd4 g6 5. Nc3 Bg7 6. Be3 Nf6 7. Bc4 O-O 8. O-O Ne5 9. Bb3 Neg4 " + 
            "10. Bg5 h6 11. Bxf6 Nxf6 12. Qf3 e5 13. Ndb5 d6 14. Nd5 a6 15. Nbc3 Bg4 16. Qg3 Rb8 17. Nxf6+ Bxf6 18. Qxg4 b5 19. Ne2 Qc7 " + 
            "20. Rac1 Bg5 21. f4 Qc8 22. Qxc8 Rbxc8 23. fxg5 hxg5 24. Rxf7 Rxf7 25. Rf1 Rf8"
        };

        var fens = new[] {
            "8/5p2/5k2/3R1p1p/3P2pP/4R1P1/KP6/8 b - - 0 35",
            "8/8/4K3/4N3/5k2/8/4q3/8 w - - 16 73",
            "5k2/6p1/6Pp/pb5P/8/2p1p2r/p1P5/RR2K1r1 w - - 2 72",
            "r1bq1r2/pp2n3/4N1Pk/3pPp2/1b1n2Q1/2N5/PP3PP1/R1B1K2R b KQ - 0 15",
            "2k3R1/1p6/p1nrpR1p/2p5/5P2/4P3/4QK2/1q6 b - - 0 35",
            "5rk1/5r2/p2p2p1/1p2p1p1/4P3/1B6/PPP1N1PP/5RK1 w - - 2 26"
        };

        for (var i = 0; i < pgns.Length; i++) {
            var pgn = pgns[i];
            var expectedFen = fens[i];
            var board = new ChessBoard();
            var moves = GetMoves(pgn);
            var fen = string.Empty;
            for (var j = 0; j < moves.Length; j++) {
                var move = moves[j];
                Assert.IsTrue(board.Move(move), $"Move {j + 1} ({move}) should be valid");
                fen = board.ToFen();
            }

            Assert.AreEqual(expectedFen, fen, $"Game {i + 1} final position does not match expected FEN");
        }
    }
}