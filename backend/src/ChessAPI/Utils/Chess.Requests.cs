using System.Numerics;
using System.Diagnostics;

namespace ChessAPI
{
// Chess.Requests.cs
public partial class Chess
{

    public GameResponse GetLegalMoves()
    {
        List<string> legalMoves = [];
        bool checkmate = false;
        bool stalemate = false;
        List<string> checkBy = [];
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] moves = [.. attacks, .. otherMoves];
        if (moves.Length == 0 || moves[0].Piece == '-' || moves[0].Piece == '+')
        {
            if (IsCheck(_turn == 'w' ? 0 : 1))
                checkmate = true;
            else
                stalemate = true;
        }
        else
        {
            foreach (var check in _checkBy[_turn == 'w' ? 0 : 1])
            {
                // Console.WriteLine(BitboardToSquare(check.Key));
                checkBy.Add(BitboardToSquare(check.Key));
            }
            foreach (Move move in moves)
            {
                string piece = (char.IsUpper(move.Piece) ? "w" : "b") + move.Piece.ToString().ToUpper();
                string moveSerialized = $"{BitboardToSquare(move.From)} {BitboardToSquare(move.To)} {piece}";
                // Console.WriteLine(moveSerialized);
                legalMoves.Add(moveSerialized);
            }
        }

        return new GameResponse(legalMoves, GetFenFromBitboard(), checkmate, stalemate, checkBy);
    }

    public BotResponse GetLegalMovesAfterBot()
    {
        int color = _turn == 'w' ? 0 : 1;

        GameResponse response = GetLegalMoves();

        if (response.Checkmate || response.Stalemate)
            return new BotResponse("", response.LegalMoves, response.Fen, response.Checkmate, response.Stalemate, response.CheckBy);
        
        // execution time
        Stopwatch sw = new();
        sw.Start();
        Move bestMove = IterativeDeepening(20);
        // var (bestMove, searchCompleted) = GetBestMove(8);

        sw.Stop();
        Console.WriteLine($"Execution Time: {sw.ElapsedMilliseconds}ms");
        if (bestMove.Piece == 'P' || bestMove.Piece == 'p' || (bestMove.To & (_fullBitboard[color ^ 1] | _enPassantMask)) != 0)
            _halfmove = 0;
        else
            _halfmove++;
        if (_turn == 'b')
            _fullmove++;

        ApplyMove(bestMove);
        response = GetLegalMoves();
        return new BotResponse($"{BitboardToSquare(bestMove.From)}{BitboardToSquare(bestMove.To)}", response.LegalMoves, response.Fen, response.Checkmate, response.Stalemate, response.CheckBy);
    }

    public GameResponse MakeMove(string moveStr)
    {
        Console.WriteLine(moveStr);
        // Parse the move string (e.g., "e2e4")
        if (moveStr.Length < 4)
            throw new ArgumentException("Invalid move format");

        string fromSquare = moveStr[..2];
        string toSquare = moveStr.Substring(2, 2);
        string promotion = moveStr.Length > 4 ? moveStr.Substring(4, 1) : "";

        // Get all legal moves
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] legalMoves = [.. attacks, .. otherMoves];

        // Find the matching move
        Move? matchingMove = null;
        foreach (Move move in legalMoves)
        {
            int fromIndex = BitOperations.TrailingZeroCount(move.From);
            int toIndex = BitOperations.TrailingZeroCount(move.To);
            
            string moveFromSquare = $"{(char)('a' + fromIndex % 8)}{8 - fromIndex / 8}";
            string moveToSquare = $"{(char)('a' + toIndex % 8)}{8 - toIndex / 8}";
            
            if (moveFromSquare == fromSquare && moveToSquare == toSquare)
            {
                if (move.IsPromotion && !string.IsNullOrEmpty(promotion))
                {
                    // Handle promotion logic if needed
                    // For now, just match the move
                    matchingMove = move;
                    break;
                }
                else if (!move.IsPromotion)
                {
                    matchingMove = move;
                    break;
                }
            }
        }

        if (matchingMove == null)
            throw new ArgumentException("Illegal move");

        // Apply the move
        ApplyMove(matchingMove);
        
        // Return the new state
        return GetLegalMoves();
    }
}

public record GameResponse(
    List<string> LegalMoves, 
    string Fen,
    bool Checkmate,
    bool Stalemate,
    List<string> CheckBy
);

public record BotResponse(
    string Move,
    List<string> LegalMoves, 
    string Fen,
    bool Checkmate,
    bool Stalemate,
    List<string> CheckBy
);
}

