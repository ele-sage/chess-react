using System.Numerics;

namespace ChessAPI
{
// Chess.MoveGen.cs
public partial class Chess
{
    private bool IsEnPassantPinned(ulong fromSquare, int color)
    {
        // Determine the captured pawn's position
        ulong capturedPawnSquare = color == 0 ? _enPassantMask << 8 : _enPassantMask >> 8;
        bool areAdjacent = (East(fromSquare) & capturedPawnSquare) != 0 || (West(fromSquare) & capturedPawnSquare) != 0;
        ulong enemyRookQueen = _bitboards[color == 0 ? 'r' : 'R'] | _bitboards[color == 0 ? 'q' : 'Q'];

        //  1. if the pawn (fromSquare) is adjacent to the captured pawn
        //  2. if king is on the same rank as the pawn that can capture en passant
        //  3. if there is a enemy rook or queen on that same rank
        if(!areAdjacent || (RankMasks[_kingPos[color,1]] & fromSquare) == 0 || (enemyRookQueen & RankMasks[_kingPos[color,1]]) == 0)
            return false;

        // Create a temporary full bitboard without the pawn doing the en passant capture and the captured pawn
        ulong[] tempFullBitboard = new ulong[2];
        _fullBitboard.CopyTo(tempFullBitboard, 0);
        
        if (color == 0) {
            tempFullBitboard[0] &= ~fromSquare;
            tempFullBitboard[1] &= ~capturedPawnSquare;
        } else {
            tempFullBitboard[1] &= ~fromSquare;
            tempFullBitboard[0] &= ~capturedPawnSquare;
        }

        ulong ray = _bitboards[color == 0 ? 'K' : 'k'];
        Func<ulong, ulong> direction = fromSquare > ray ? East : West;

        while ((ray = direction(ray)) != 0) {
            if ((ray & tempFullBitboard[color]) != 0) return false; // Blocked by friendly piece
            if ((ray & tempFullBitboard[color ^ 1]) != 0) {
                // Check if it's a rook or queen
                if ((ray & enemyRookQueen) != 0) {
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    private ulong[] GeneratePawnMoves(ulong bitboard, int color, bool isCoverage, int constraint)
    {
        ulong moves = 0UL;
        ulong capture = 0UL;
        bool isEnPassantValid = !isCoverage && _enPassantMask != 0 && _turn == (color == 0 ? 'w' : 'b') && !IsEnPassantPinned(bitboard, color);
        if (isCoverage)
        {
            ulong diagonal = PawnAttack[color,0](bitboard) | PawnAttack[color,1](bitboard);
            _pieceAttack[color] |=  diagonal & (_fullBitboard[color ^ 1] | (isEnPassantValid ? _enPassantMask : 0UL));
            return [diagonal & ~_fullBitboard[color ^ 1], 0UL];
        }
        if (constraint < 2)
        {
            ulong diagonal = PawnAttack[color,0](bitboard) | PawnAttack[color,1](bitboard);

            moves = PawnDirection[color](bitboard) & _emptyBitboard;
            moves |= PawnDirection[color](moves) & _emptyBitboard & RankMasks[3 + color];
            if (constraint == 0)
                capture |= diagonal & (_fullBitboard[color ^ 1] | (isEnPassantValid ? _enPassantMask : 0UL));
        }
        else if (constraint == 5 && (PawnAttack[color,0](bitboard) & _fullBitboard[color ^ 1]) != 0)
            capture |= PawnAttack[color,0](bitboard) & _fullBitboard[color ^ 1];
        else if (constraint == 7 && (PawnAttack[color,1](bitboard) & _fullBitboard[color ^ 1]) != 0)
            capture |= PawnAttack[color,1](bitboard) & _fullBitboard[color ^ 1];

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
                    // If the direction is blocked by a piece and it's the the enemy king
                    direction &= _emptyBitboard | _bitboards[Pieces[color ^ 1,5]];
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
        ulong moves = kingMoves & _emptyBitboard & ~_pieceCoverage[color ^ 1];
        ulong capture = kingMoves & _fullBitboard[color ^ 1] & ~_pieceCoverage[color ^ 1];

        bool isCheck = _checkBy[_turn == 'w' ? 0 : 1].Count != 0;
        if (_castle[color, 0] && !isCheck && ((CastleSpace[color, 0] & _emptyBitboard & ~_pieceCoverage[color ^ 1]) == CastleSpace[color, 0]))
            moves |= bitboard << 2;
        if (_castle[color, 1] && !isCheck && ((CastleSpace[color, 1] & _emptyBitboard & ~_pieceCoverage[color ^ 1]) == CastleSpace[color, 1]))
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
}
}