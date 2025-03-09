using System.Numerics;

namespace ChessAPI
{
// Chess.Move.cs

public partial class Chess
{
    private void ApplyMove(Move move)
    {
        int color = _turn == 'w' ? 0 : 1;

        _bitboards[move.Piece] &= ~move.From;
        _bitboards[move.Piece] |= move.To;

        if ((move.To & _enPassantMask) != 0 && (move.Piece == 'P' || move.Piece == 'p'))
        {
            move.PrevPiece = Pieces[color ^ 1, 0];
            _bitboards[Pieces[color ^ 1, 0]] &= ~(color == 0 ? move.To << 8 : move.To >> 8);           
        }
        else if ((move.To & _fullBitboard[color ^ 1]) != 0)
        {
            for (int j = 0; j < 6; j++)
            {
                if ((move.To & _bitboards[Pieces[color ^ 1, j]]) != 0)
                {
                    if (_castle[color ^ 1, 0] && (move.To & RookPositions[color ^ 1, 0]) != 0)
                        _castle[color ^ 1, 0] = false;
                    else if (_castle[color ^ 1, 1] && (move.To & RookPositions[color ^ 1, 1]) != 0)
                        _castle[color ^ 1, 1] = false;

                    move.PrevPiece = Pieces[color ^ 1, j];
                    _bitboards[Pieces[color ^ 1, j]] &= ~move.To;
                    break;
                }
            }
        }
        _enPassantMask = 0UL;
        if (move.Piece == 'P' || move.Piece == 'p')
        {
            if (Math.Abs(BitOperations.TrailingZeroCount(move.To) - BitOperations.TrailingZeroCount(move.From)) == 16)
            {
                if (move.To > move.From)
                    _enPassantMask = move.To >> 8;
                else
                    _enPassantMask = move.To << 8;
            }
            else if ((move.To & (RankMasks[0] | RankMasks[7])) != 0)
            {
                move.IsPromotion = true;
                _bitboards[move.Piece] &= ~move.To;
                _bitboards[Pieces[color, 4]] |= move.To;
            }
        }
        else if (move.Piece == 'K' || move.Piece == 'k')
        {
            if (Math.Abs(BitOperations.TrailingZeroCount(move.To) - BitOperations.TrailingZeroCount(move.From)) == 2)
            {
                if (move.To > move.From)
                {
                    _bitboards[Pieces[color, 3]] &= ~RookPositions[color, 0];
                    _bitboards[Pieces[color, 3]] |= RookCastlePositions[color, 0];
                }
                else
                {
                    _bitboards[Pieces[color, 3]] &= ~RookPositions[color, 1];
                    _bitboards[Pieces[color, 3]] |= RookCastlePositions[color, 1];
                }
            }
            SetKingPos();
            _castle[color, 0] = false;
            _castle[color, 1] = false;
        }
        else if (move.Piece == 'R' || move.Piece == 'r')
        {
            if (BitOperations.TrailingZeroCount(move.From) % 8 == 0)
                _castle[color, 1] = false;
            else if (BitOperations.TrailingZeroCount(move.From) % 8 == 7)
                _castle[color, 0] = false;
        }
        _turn = _turn == 'w' ? 'b' : 'w';
    }

    private void UndoMove(Move move, ulong enPassantMask, ulong[] fullBitboard, bool[,] castle, int[,] kingPos, ulong pinnedToKing)
    {
        _turn = _turn == 'w' ? 'b' : 'w';
        int color = _turn == 'w' ? 0 : 1;

        _bitboards[move.Piece] &= ~move.To;
        _bitboards[move.Piece] |= move.From;

        if ((move.To & enPassantMask) != 0)
        {
            _bitboards[Pieces[color ^ 1, 0]] |= color == 0 ? move.To << 8 : move.To >> 8;
        }
        else if (move.PrevPiece != '-')
        {
            _bitboards[move.PrevPiece] |= move.To;
        }
        else if (move.Piece == 'K' || move.Piece == 'k')
        {
            if (Math.Abs(BitOperations.TrailingZeroCount(move.To) - BitOperations.TrailingZeroCount(move.From)) == 2)
            {
                if (move.To > move.From)
                {
                    _bitboards[Pieces[color, 3]] &= ~RookCastlePositions[color, 0];
                    _bitboards[Pieces[color, 3]] |= RookPositions[color, 0];
                }
                else
                {
                    _bitboards[Pieces[color, 3]] &= ~RookCastlePositions[color, 1];
                    _bitboards[Pieces[color, 3]] |= RookPositions[color, 1];
                }
            }
        }
        if (move.IsPromotion)
        {
            _bitboards[Pieces[color, 4]] &= ~move.To;
        }
        _castle[0, 0] = castle[0,0];
        _castle[0, 1] = castle[0,1];
        _castle[1, 0] = castle[1,0];
        _castle[1, 1] = castle[1,1];
        _enPassantMask = enPassantMask;
        _fullBitboard[0] = fullBitboard[0];
        _fullBitboard[1] = fullBitboard[1];
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        _kingPos[0, 0] = kingPos[0,0];
        _kingPos[0, 1] = kingPos[0,1];
        _kingPos[1, 0] = kingPos[1,0];
        _kingPos[1, 1] = kingPos[1,1];
        _pinnedToKing[color] = pinnedToKing;
    }
}

public class Move
{
    public int Score { get; set; }
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

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        Move move = (Move)obj;
        return Piece == move.Piece && From == move.From && To == move.To;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Piece, From, To);
    }
}
}