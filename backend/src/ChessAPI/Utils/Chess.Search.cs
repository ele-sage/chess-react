using System.Numerics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{


    private Move[] GetMoves(ulong from, ulong moves, char piece)
    {
        Move[] movesArray = new Move[BitOperations.PopCount(moves)];
        int i = 0;
        while (moves != 0)
        {
            ulong bit = moves & ~(moves - 1);
            movesArray[i++] = new Move(piece, from, bit);
            moves &= moves - 1;
        }
        return movesArray;
    }
    private Move[] GetAllPossibleMoves(char turn)
    {
        int color = turn == 'w' ? 0 : 1;

        SetFullBitboard(color);
        SetFullBitboard(color ^ 1);
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        int[] kingPos = BitboardToCoord(turn == 'w' ? _bitboards['K'] : _bitboards['k']);
        _kingPos[color, 0] = kingPos[0];
        _kingPos[color, 1] = kingPos[1];
        _pinnedToKing[color] = PinnedToKing(color, RookDirections) | PinnedToKing(color, BishopDirections);
        // Print all _pinnedToKing
        // PrintBitBoard(_pinnedToKing[color]);
        SetCoverage(color ^ 1);

        IsCheck(color);
        List<Move> moves = [];
        List<Move> attacks = [];

        foreach (char piece in PiecesString[color])
        {
            char pieceKey = char.ToLower(piece);
            ulong piecesMask = _bitboards[piece];
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
                    moves.AddRange(GetMoves(pieceBitboard, movesAttacks[0], piece));
                    attacks.AddRange(GetMoves(pieceBitboard, movesAttacks[1], piece));
                }
            }
        }

        return [.. attacks, .. moves];
    }
}

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

    public Move(Move move)
    {
        Piece = move.Piece;
        From = move.From;
        To = move.To;
        PrevPiece = move.PrevPiece;
        IsPromotion = move.IsPromotion;
        CastleMask = move.CastleMask;
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