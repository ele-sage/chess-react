
namespace ChessAPI
{
// Chess.Perft.cs
public partial class Chess
{
    // Count all positions to a specific depth
    public long Perft(int depth)
    {
        if (depth == 0) return 1;

        long nodes = 0;
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] moves = [.. attacks, .. otherMoves];

        foreach (var move in moves)
        {
            // Save board state
            ulong enPassantMask = _enPassantMask;
            ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
            bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
            int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
            int color = _turn == 'w' ? 0 : 1;
            ulong pinnedToKing = _pinnedToKing[color];
            
            ApplyMove(move);
            long childNodes = Perft(depth - 1);
            nodes += childNodes;
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
        }
        return nodes;
    }
    
    // Run Perft with detailed results for first moves (helpful for debugging)
    public void PerftDivide(int depth)
    {
        string directoryPath = "./Utils/perftCompare";
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        File.WriteAllText($"{directoryPath}/myPerft.txt", "");
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] moves = [.. attacks, .. otherMoves];
        long totalNodes = 0;
        foreach (var move in moves)
        {
            // Save state
            ulong enPassantMask = _enPassantMask;
            ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
            bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
            int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
            int color = _turn == 'w' ? 0 : 1;
            ulong pinnedToKing = _pinnedToKing[color];
            
            ApplyMove(move);
            long nodes = Perft(depth - 1);
            totalNodes += nodes;
            
            // Write move and node count to ./perftCompare/myPerft.txt
            // Console.WriteLine($"{BitboardToSquare(move.From)}{BitboardToSquare(move.To)}: {nodes}");
            // Could not find a part of the path '/app/src/ChessAPI/perftCompare/myPerft.txt'.    
            File.AppendAllText($"{directoryPath}/myPerft.txt", $"{BitboardToSquare(move.From)}{BitboardToSquare(move.To)}: {nodes}\n");
            
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
        }
        Console.WriteLine($"Total: {totalNodes}");
    }

    public void VerifyMoveGenerator()
    {
        // Test case format: FEN position, depth, expected node count
        var testCases = new (string fen, int depth, long expected)[]
        {
            // Starting position
            ("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 1, 20),
            ("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 2, 400),
            ("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 3, 8902),
            ("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 4, 197281),


            // Position 1
            ("rnbqkbnr/pppppppp/8/8/8/3P4/PPP1PPPP/RNBQKBNR b KQkq - 0 1", 4, 328511),
            ("rnbqkbnr/pppp1ppp/4p3/8/8/3P4/PPP1PPPP/RNBQKBNR w KQkq - 0 2", 3, 21624),
            ("rnbqkbnr/pppp1ppp/4p3/8/8/3P4/PPPKPPPP/RNBQ1BNR b kq - 0 2", 2, 647),

            
            // Position 2 (test castling and en passant)
            ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48),
            ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2039),
            ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97862),
            ("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 4, 4085603),
            
            // Position 3 (test pinned pieces)
            ("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 1, 14),
            ("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 2, 191),
            
            // Position 4 (test en passant captures)
            ("rnbqkb1r/pp1p1ppp/2p5/4P3/2B5/8/PPP1NnPP/RNBQK2R w KQkq - 0 6", 1, 42),
            
            // Position 5 (test promotion)
            // ("n1n5/PPPk4/8/8/8/8/4Kppp/5N1N b - - 0 1", 1, 24)
        };
        
        foreach (var (fen, depth, expected) in testCases)
        {
            Chess chess = new(fen);
            long actual = chess.Perft(depth);
            
            Console.WriteLine($"Position: {fen}");
            Console.WriteLine($"Depth: {depth}, Expected: {expected}, Actual: {actual}");
            Console.WriteLine(actual == expected ? "PASS" : "FAIL");
            Console.WriteLine();
            
            // If it fails, run divide to help locate the problem
            if (actual != expected && depth > 1)
            {
                Console.WriteLine("Detailed move breakdown:");
                chess.PerftDivide(depth);
            }
        }
    }
}
}
