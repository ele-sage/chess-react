namespace ChessAPI
{
// Chess.Moves.cs
public partial class Chess
{
    private Dictionary<string, List<string>> _whiteMoves = [];
    private Dictionary<string, List<string>> _blackMoves = [];

    private ulong GeneratePawnMoves(ulong bitboard, bool isWhite, int constraint)
    {
        ulong moves = 0UL;
        int color = isWhite ? 0 : 1;

        if (!_coverageSet)
            return isWhite ? (NorthEast(bitboard) | NorthWest(bitboard)) & ~_fullBitboard[color ^ 1] : (SouthEast(bitboard) | SouthWest(bitboard)) & ~_fullBitboard[color ^ 1];
        if (constraint == 0 || constraint == 1)
        {
            ulong singleStep;
            moves |= singleStep = isWhite ? North(bitboard) & _emptyBitboard : South(bitboard) & _emptyBitboard;
            moves |= isWhite ? North(singleStep) & _emptyBitboard & RankMasks[3] : South(singleStep) & _emptyBitboard & RankMasks[4];
            if (constraint == 0)
                moves |= isWhite ? (NorthEast(bitboard) | NorthWest(bitboard)) & (_fullBitboard[1] | _enPassantMask) : (SouthEast(bitboard) | SouthWest(bitboard)) & (_fullBitboard[0] | _enPassantMask);
        }

        return moves;
    }

    private ulong GenerateKnightMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (!_coverageSet)
            isWhite = !isWhite;
        ulong moves = 0UL;
        int   color = isWhite ?  0 : 1;

        if (constraint != 0)
            return moves;

        ulong[] knightMoves = [
            KnightNE(bitboard), KnightNW(bitboard), KnightSE(bitboard), KnightSW(bitboard),
            KnightEN(bitboard), KnightES(bitboard), KnightWN(bitboard), KnightWS(bitboard)
        ];

        foreach (var move in knightMoves)
            if ((move & (_emptyBitboard | _fullBitboard[color ^ 1])) != 0)
                moves |= move;

        return moves;
    }

    private ulong IterDir(ulong bitboard, bool isWhite, int start, int finish)
    {
        if (!_coverageSet)
            isWhite = !isWhite;
        ulong moves = 0UL;
        int   color = isWhite ?  0 : 1;

        for (int i = start; i < finish; i++)
        {
            ulong direction = QueenDirections[i](bitboard);

            while ((direction & _emptyBitboard) != 0)
            {
                direction &= _emptyBitboard;
                moves |= direction;
                direction = QueenDirections[i](direction);
            }
            if ((direction & _fullBitboard[color ^ 1]) != 0)
                moves |= direction;
        }

        return moves;
    }

    private ulong GenerateBishopMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint < 4)
                return 0UL;
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, isWhite, 4, 8);
    }

    private ulong GenerateRookMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint > 4)
                return 0UL;
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, isWhite, 0, 4);
    }

    private ulong GenerateQueenMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        return IterDir(bitboard, isWhite, 0, 8);
    }

    private ulong GenerateKingMoves(ulong bitboard, bool isWhite, int constraint)
    {
        ulong moves = 0UL;
        int   color = isWhite ?  0 : 1;
        ulong enemyMaskCoverage = _coverageSet ? _pieceCoverage[color ^ 1] : 0UL;

        ulong[] kingMoves = {
            North(bitboard), South(bitboard), East(bitboard), West(bitboard),
            NorthEast(bitboard), NorthWest(bitboard), SouthEast(bitboard), SouthWest(bitboard)
        };

        foreach (var move in kingMoves)
            if ((move & (_emptyBitboard | _fullBitboard[color ^ 1]) & ~enemyMaskCoverage) != 0)
                moves |= move;
        return moves;
    }

    private int AxisConstraint(ulong pieceBitboard, bool isWhite)
    {
        int color = isWhite ?  0 : 1;
        
        if ((pieceBitboard & _pinnedToKing[color]) != 0)
        {
            if ((pieceBitboard & (isWhite ? _bitboards['N'] : _bitboards['n'])) != 0)
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

    private bool IsCheck(ulong bitboard, bool isWhite)
    {
        int   color = isWhite ?  0 : 1;
        
        ulong[] knightMoves = [
            KnightNE(bitboard), KnightNW(bitboard), KnightSE(bitboard), KnightSW(bitboard),
            KnightEN(bitboard), KnightES(bitboard), KnightWN(bitboard), KnightWS(bitboard)
        ];
        foreach (var move in knightMoves)
        {
            Console.WriteLine(PieceSymbols[((color ^ 1) * 5) + 1]);
            if ((move & (_emptyBitboard | _fullBitboard[PieceSymbols[((color ^ 1) * 5) + 1]])) != 0)
                return true;
        }
        for (int i = 0; i < 8; i++)
        {
            ulong direction = QueenDirections[i](bitboard);

            while ((direction & _emptyBitboard) != 0)
            {
                direction &= _emptyBitboard;
                direction = QueenDirections[i](direction);
            }
            if ((direction & (_fullBitboard[PieceSymbols[((color ^ 1) * 5) + 4]] | _fullBitboard[PieceSymbols[((color ^ 1) * 5) + (i % 4) + 2]])) != 0)
                return true;
        }

        return false;
    }

    public void GetAllMovesForPiece(char piece)
    {
        bool isWhite = char.IsUpper(piece);
        char pieceKey = char.ToLower(piece);

        if (_moveGenerators.TryGetValue(pieceKey, out var generateMoves))
        {
            ulong piecesMask = _bitboards[piece];
            while (piecesMask != 0)
            {
                ulong pieceBitboard = piecesMask & ~(piecesMask - 1);
                piecesMask &= piecesMask - 1;
                int constraint = AxisConstraint(pieceBitboard, isWhite);

                string piecePosition = piece.ToString() + ", " + BitboardToSquare(pieceBitboard);
                ulong movesBitboards = generateMoves(pieceBitboard, isWhite, constraint);
                List<string> moves = BitboardToSquares(movesBitboards);

                if (isWhite)
                {
                    if (!_whiteMoves.TryAdd(piecePosition, moves))
                        _whiteMoves[piecePosition] = moves;
                }
                else
                {
                    if (!_blackMoves.TryAdd(piecePosition, moves))
                        _blackMoves[piecePosition] = moves;
                }
            }
        }
    }
}
}