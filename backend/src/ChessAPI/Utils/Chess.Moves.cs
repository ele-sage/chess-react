using System.Numerics;

namespace ChessAPI
{
// Chess.Moves.cs
public partial class Chess
{
    private ulong[] GeneratePawnMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        ulong moves = 0UL;
        ulong capture = 0UL;
        if (isCoverage)
        {
            ulong diagonal = PawnAttack[color,0](bitboard) | PawnAttack[color,1](bitboard);
            _pieceAttack[color] |=  diagonal & (_fullBitboard[color ^ 1] | _enPassantMask);
            return [diagonal & ~_fullBitboard[color ^ 1], 0UL];
        }
        if (constraint < 2)
        {
            ulong diagonal = PawnAttack[color,0](bitboard) | PawnAttack[color,1](bitboard);

            moves = PawnDirection[color](bitboard) & _emptyBitboard;
            moves |= PawnDirection[color](moves) & _emptyBitboard & RankMasks[3 + color];
            if (constraint == 0)
                capture |= diagonal & (_fullBitboard[color ^ 1] | _enPassantMask);
        }
        // else if (constraint == 5 && (PawnAttack[color,0](bitboard) & _fullBitboard[color ^ 1]) != 0)
        //     capture |= PawnAttack[color,0](bitboard) & _fullBitboard[color ^ 1];
        // else if (constraint == 7 && (PawnAttack[color,1](bitboard) & _fullBitboard[color ^ 1]) != 0)
        //     capture |= PawnAttack[color,1](bitboard) & _fullBitboard[color ^ 1];

        return [moves, capture];
    }

    private ulong[] GenerateKnightMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        if (constraint != 0)
            return [0UL, 0UL];

        ulong knightMoves = KnightMoves(bitboard);

        if (isCoverage)
        {
            _pieceAttack[color] |= knightMoves & _fullBitboard[color ^ 1];
            return [knightMoves & ~_fullBitboard[color ^ 1], 0UL];
        }
        return [knightMoves & _emptyBitboard, knightMoves & _fullBitboard[color ^ 1]];
    }

    private ulong[] IterDir(ulong bitboard, int color, bool isCoverage, int start, int finish)
    {
        ulong moves = 0UL;
        ulong capture = 0UL;

        for (int i = start; i < finish; i++)
        {
            ulong direction = QueenDirections[i](bitboard);
            if (isCoverage)
            {
                while (direction > 0)
                {
                    moves |= direction & _fullBitboard[color];
                    _pieceAttack[color] |= direction & _fullBitboard[color ^ 1];
                    direction &= _emptyBitboard;
                    moves |= direction;
                    direction = QueenDirections[i](direction);
                }
            }
            else
            {
                while ((direction & _emptyBitboard) != 0)
                {
                    direction &= _emptyBitboard;
                    moves |= direction;
                    direction = QueenDirections[i](direction);
                }
                if ((direction & _fullBitboard[color ^ 1]) != 0)
                    capture |= direction & _fullBitboard[color ^ 1];
            }
        }
        return [moves, capture];
    }

    private ulong[] GenerateBishopMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint < 4)
                return [0UL, 0UL];
            constraint -= 4;
            return IterDir(bitboard, color, isCoverage, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, color, isCoverage, 0, 4);

    }

    private ulong[] GenerateRookMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint > 4)
                return [0UL, 0UL];
            constraint += 4;
            return IterDir(bitboard, color, isCoverage, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, color, isCoverage, 4, 8);
    }

    private ulong[] GenerateQueenMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint < 4)
                constraint += 4;
            else if (constraint > 4)
                constraint -= 4;
            return IterDir(bitboard, color, isCoverage, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, color, isCoverage, 0, 8);
    }

    private ulong[] GenerateKingMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        ulong kingMoves = North(bitboard) | South(bitboard) | East(bitboard) | West(bitboard) | NorthEast(bitboard) | NorthWest(bitboard) | SouthEast(bitboard) | SouthWest(bitboard);
        // kingMoves &= (_emptyBitboard | _fullBitboard[color ^ 1]) & ~_pieceCoverage[color ^ 1];
        ulong moves = kingMoves & _emptyBitboard & ~_pieceCoverage[color ^ 1];
        ulong capture = kingMoves & _fullBitboard[color ^ 1] & ~_pieceCoverage[color ^ 1];

        if (_castle[color, 0] && ((CastleSpace[color, 0] & _emptyBitboard & ~_pieceCoverage[color ^ 1]) == CastleSpace[color, 0]))
            moves |= bitboard << 2;
        if (_castle[color, 1] && ((CastleSpace[color, 1] & _emptyBitboard & ~_pieceCoverage[color ^ 1]) == CastleSpace[color, 1]))
            moves |= bitboard >> 2;
        return [moves, capture];
    }

    private int AxisConstraint(ulong pieceBitboard, int color)
    {
        if ((pieceBitboard & _pinnedToKing[color]) != 0)
        {
            if ((pieceBitboard & (color == 0 ? _bitboards['N'] : _bitboards['n'])) != 0)
                return 1;

            if ((FileMasks[_kingPos[color,0]] & pieceBitboard) != 0)
                return 1;
            else if ((RankMasks[_kingPos[color,1]] & pieceBitboard) != 0)
                return 3;
            else 
            {
                int[] piecePos = BitboardToCoord(pieceBitboard);

                if ((piecePos[0] - piecePos[1]) == (_kingPos[color,0] - _kingPos[color,1]))
                    return 5;
                if ((piecePos[0] + piecePos[1]) == (_kingPos[color,0] + _kingPos[color,1]))
                    return 7;
            }
        }
        return 0;
    }

    private ulong CheckMask(int color, ulong bitboard, Func<ulong, ulong> direction)
    {
        char king = color == 0 ? 'K' : 'k';
        ulong mask = bitboard;

        if (direction(bitboard) == mask) return mask;
        do {
            mask |= bitboard;
            bitboard = direction(bitboard);
            // PrintBitBoard(bitboard);
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
        if (_checkBy[color].Count != 0)
        {
            _castle[color,0] = false;
            _castle[color,1] = false;
        }
        return _checkBy[color].Count != 0;
    }
}
}