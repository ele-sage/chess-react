using System.Numerics;

namespace ChessAPI
{
// Chess.Check.cs
public partial class Chess
{
    private ulong CheckMask(int color, ulong bitboard, Func<ulong, ulong> direction)
    {
        char king = color == 0 ? 'K' : 'k';
        ulong mask = bitboard;

        if (direction(bitboard) == mask) return mask;
        do {
            mask |= bitboard;
            bitboard = direction(bitboard);
        } while ((_bitboards[king] & bitboard) == 0);
        return mask;
    }

    private void SetCheckBy(int color, ulong bitboard, Func<ulong, ulong> direction)
    {
        while (bitboard != 0)
        {
            ulong bit = bitboard & ~(bitboard - 1);
            int index = BitOperations.TrailingZeroCount(bit);
            ulong bitboardPiece = 1UL << index;

            if (_checkBy[color].ContainsKey(bitboardPiece))
                _checkBy[color][bitboardPiece] |= CheckMask(color, bitboardPiece, direction);
            else
                _checkBy[color].Add(bitboardPiece, CheckMask(color, bitboardPiece, direction));
            bitboard &= bitboard - 1;
        }
    }

    private bool IsCheck(int color)
    {
        _checkBy[color].Clear();
        char[] pieces = color == 0 ? ['K','n','b','r','q'] : ['k','N','B','R','Q'];
    
        ulong knightMoves = KnightMoves(_bitboards[pieces[0]]) & _bitboards[pieces[1]];
        SetCheckBy(color, knightMoves, Self);

        ulong pawnMoves = (PawnAttack[color,0](_bitboards[pieces[0]]) | PawnAttack[color,1](_bitboards[pieces[0]])) & _bitboards[Pieces[color ^ 1,0]];
        SetCheckBy(color, pawnMoves, Self);

        for (int i = 0; i < 8; i++)
        {
            ulong direction = QueenDirections[i](_bitboards[pieces[0]]);
            char bishop_rook = i < 4 ? pieces[2] : pieces[3];
            while (direction > 0)
            {
                SetCheckBy(color, direction & _bitboards[pieces[4]], QueenDirections[i + ((i % 2 == 0) ? 1 : -1)]);
                SetCheckBy(color, direction & _bitboards[bishop_rook], QueenDirections[i + ((i % 2 == 0) ? 1 : -1)]);

                direction &= _emptyBitboard;
                direction = QueenDirections[i](direction);
            }
        }
        return _checkBy[color].Count != 0;
    }
}
}