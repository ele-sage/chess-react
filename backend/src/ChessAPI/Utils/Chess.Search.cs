using System.Diagnostics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{
    private Move IterativeDeepening()
    {
        _maxDepth = 6;
        _timeLimitMillis = 1000;

        Stopwatch stopwatch = new();
        stopwatch.Start();
        bool isMaximizingPlayer = _turn == 'w';

        for (int depth = 3; depth <= _maxDepth; depth++)
        {
            _currentDepth = depth;
            AlphaBeta(depth, int.MinValue, int.MaxValue, isMaximizingPlayer, stopwatch);

            if (stopwatch.ElapsedMilliseconds >= _timeLimitMillis)
                break;
            _bestMove = _currentBestMove;
            _bestScore = _currentBestScore;
        }
        if (isMaximizingPlayer)
        {
            if (_currentBestScore > _bestScore)
                _bestMove = _currentBestMove;
        }
        else
        {
            if (_currentBestScore < _bestScore)
                _bestMove = _currentBestMove;
        }
        stopwatch.Stop();
        Console.WriteLine($"Depth: {_currentDepth}");
        Console.WriteLine($"Possible moves: {_possibleMove}");
        return _bestMove;
    }

    private static List<Move> GetMoves(ulong from, ulong moves, char piece)
    {
        List<Move> moveList = [];

        while (moves != 0)
        {
            ulong bit = moves & ~(moves - 1);
            moveList.Add(new Move(piece, from, bit));
            moves &= moves - 1;
        }
        return moveList;
    }

    private Move[] GetAllPossibleMoves(char turn)
    {
        int color = turn == 'w' ? 0 : 1;
        List<Move> moves = [];
        List<Move> attacks = [];

        SetFullBitboard(color);
        SetFullBitboard(color ^ 1);
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        int[] kingPos = BitboardToCoord(turn == 'w' ? _bitboards['K'] : _bitboards['k']);
        _kingPos[color, 0] = kingPos[0];
        _kingPos[color, 1] = kingPos[1];
        _pinnedToKing[color] = PinnedToKing(color, RookDirections) | PinnedToKing(color, BishopDirections);
        SetCoverage(color ^ 1);
        IsCheck(color);

        for (int i = 0; i < 6; i++)
        {
            char pieceKey = char.ToLower(Pieces[color, i]);
            ulong piecesMask = _bitboards[Pieces[color, i]];

            while (piecesMask != 0)
            {
                ulong pieceBitboard = piecesMask & ~(piecesMask - 1);
                piecesMask &= piecesMask - 1;

                if (_checkBy[color].Count == 2 && pieceKey != 'k')
                {
                    continue;
                }
                else
                {
                    int constraint = AxisConstraint(pieceBitboard, color);
                    ulong[] movesAttacks = _moveGenerators[pieceKey](pieceBitboard, color, false, constraint);

                    if (_checkBy[color].Count == 1 && pieceKey != 'k')
                    {
                        movesAttacks[0] &= _checkBy[color].ElementAt(0).Value;
                        movesAttacks[1] &= _checkBy[color].ElementAt(0).Value;
                    }
                    moves.AddRange(GetMoves(pieceBitboard, movesAttacks[0], Pieces[color, i]));
                    attacks.AddRange(GetMoves(pieceBitboard, movesAttacks[1], Pieces[color, i]));

                    // moves.InsertRange(0, GetMoves(pieceBitboard, movesAttacks[0] & ~_pieceCoverage[color ^ 1], Pieces[color, i]));
                    // moves.AddRange(GetMoves(pieceBitboard, movesAttacks[0] & _pieceCoverage[color ^ 1], Pieces[color, i]));
                    // attacks.InsertRange(0, GetMoves(pieceBitboard, movesAttacks[1] & ~_pieceCoverage[color ^ 1], Pieces[color, i]));
                    // attacks.AddRange(GetMoves(pieceBitboard, movesAttacks[1] & _pieceCoverage[color ^ 1], Pieces[color, i]));

                    // moves.InsertRange(0, GetMoves(pieceBitboard, movesAttacks[1] & _pieceCoverage[color ^ 1], Pieces[color, i]));
                    // moves.AddRange(GetMoves(pieceBitboard, movesAttacks[0] & _pieceCoverage[color ^ 1], Pieces[color, i]));
                    // attacks.InsertRange(0, GetMoves(pieceBitboard, movesAttacks[1] & ~_pieceCoverage[color ^ 1], Pieces[color, i]));
                    // attacks.AddRange(GetMoves(pieceBitboard, movesAttacks[0] & ~_pieceCoverage[color ^ 1], Pieces[color, i]));
                }
            }
        }
        return [.. attacks, .. moves];
    }
}
// A simple move representation
public class Move
{
    public char Piece { get; }
    public ulong From { get; }
    public ulong To { get; }
    public char PrevPiece { get; set; }
    public bool IsPromotion { get; set; }
    public ulong CastleMask { get; set; }
    public Move(char piece, ulong from, ulong to)
    {
        Piece = piece;
        From = from;
        To = to;
        PrevPiece = '-';
        IsPromotion = false;
        CastleMask = 0UL;
    }

    public static string GetBitBoardString(ulong bitboard)
    {
        string board = "";
        for (int i = 0; i < 64; i++)
        {
            if (i % 8 == 0) board += $"\n{8 - i / 8} ";
            board += $"{(bitboard >> i) & 1UL} ";
        }
        return board + "\n  a b c d e f g h\n";
    }

    public static string GetFromToBoardString(ulong from, ulong to)
    {
        string board = "";
        for (int i = 0; i < 64; i++)
        {
            if (i % 8 == 0) board += $"\n{8 - i / 8} ";
            if (((from >> i) & 1UL) == 1)
                board += "F ";
            else if (((to >> i) & 1UL) == 1)
                board += "T ";
            else
                board += ". ";
        }
        return board + "\n  a b c d e f g h\n";
    }

    public override string ToString()
    {
        return $"Piece: {Piece} {GetFromToBoardString(From, To)}";
    }
}
}