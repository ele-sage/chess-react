using System.Collections.Concurrent;
using System.Numerics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{
    public Move GetBestMove(int maxDepth)
    {
        bool isMaximizingPlayer = _turn == 'w';
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        int bestScore = isMaximizingPlayer ? int.MinValue : int.MaxValue;
        Move bestMove = new('-', 0UL, 0UL);
        List<Move> allMoves = GetAllPossibleMoves(_turn);
        int color = _turn == 'w' ? 0 : 1;
        if (allMoves.Count == 0)
        {
            if (_checkBy[color].Count == 0)
                return new('+', 0UL, 0UL); // Stalemate
            else
                return new('-', 0UL, 0UL); // Checkmate
        }
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[,] castle = { { _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] } };
        int[,] kingPos = { { _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] } };
        ulong pinnedToKing = _pinnedToKing[color];

        PrintAllFieldsToFile("original");
        foreach (Move move in allMoves)
        {
            ApplyMove(move);
            int score = AlphaBeta(maxDepth - 1, alpha, beta, !isMaximizingPlayer);
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing, true);
            if (isMaximizingPlayer && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                alpha = Math.Max(alpha, bestScore);
            }
            else if (!isMaximizingPlayer && score < bestScore)
            {
                bestScore = score;
                bestMove = move;
                beta = Math.Min(beta, bestScore);
            }

            if (alpha >= beta)
                break;
        }
        PrintAllFieldsToFile("final");
        Console.WriteLine($"Possible moves: {_possibleMove}");
        return bestMove;
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

    private List<Move> GetAllPossibleMoves(char turn)
    {
        int color = turn == 'w' ? 0 : 1;
        List<Move> allMoves = [];

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
                ulong movesBitboards;
                if (_checkBy[color].Count == 2 && pieceKey != 'k')
                    movesBitboards = 0UL;
                else
                {
                    int constraint = AxisConstraint(pieceBitboard, color);
                    movesBitboards = _moveGenerators[pieceKey](pieceBitboard, color, false, constraint);

                    if (_checkBy[color].Count == 1 && pieceKey != 'k')
                        movesBitboards &= _checkBy[color].ElementAt(0).Value;
                }
                if (movesBitboards > 0)
                {
                    allMoves.AddRange(GetMoves(pieceBitboard, movesBitboards, Pieces[color, i]));
                }
            }
        }
        return allMoves;
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