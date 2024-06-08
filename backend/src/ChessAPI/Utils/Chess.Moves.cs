namespace ChessAPI
{
public partial class Chess
{
    private Dictionary<string, List<string>> _whiteMoves = new();
    private Dictionary<string, List<string>> _blackMoves = new();

    private ulong GeneratePawnMoves(ulong bitboard, bool isWhite, int constraint)
    {
        ulong moves = 0UL;
        ulong singleStep = 0UL;

        if (!_coverageSet)
        {
            ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);
            return (isWhite ? (NorthEast(bitboard) | NorthWest(bitboard)) & ~enemyMask : (SouthEast(bitboard) | SouthWest(bitboard)) & ~enemyMask);
        }
        if (constraint == 0 || constraint == 1)
        {
            moves |= singleStep = (isWhite ? North(bitboard) & _emptyBitboard : South(bitboard) & _emptyBitboard);
            moves |= (isWhite ? North(singleStep) & _emptyBitboard & RankMasks[3] : South(singleStep) & _emptyBitboard & RankMasks[4]);
            if (constraint == 0)
                moves |= (isWhite ? (NorthEast(bitboard) | NorthWest(bitboard)) & (_blackBitboard | _enPassantMask) : (SouthEast(bitboard) | SouthWest(bitboard)) & (_whiteBitboard | _enPassantMask));
        }

        return moves;
    }

    private ulong GenerateKnightMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (!_coverageSet)
            isWhite = !isWhite;
        ulong moves = 0UL;
        ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);

        if (constraint != 0)
            return moves;

        ulong[] knightMoves = {
            KnightNE(bitboard), KnightNW(bitboard), KnightSE(bitboard), KnightSW(bitboard),
            KnightEN(bitboard), KnightES(bitboard), KnightWN(bitboard), KnightWS(bitboard)
        };

        foreach (var move in knightMoves)
            if ((move & (_emptyBitboard | enemyMask)) != 0)
                moves |= move;

        return moves;
    }

    private ulong IterDir(ulong bitboard, bool isWhite, int start, int finish)
    {
        if (!_coverageSet)
            isWhite = !isWhite;
        ulong moves = 0UL;
        ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);

        for (int i = start; i < finish; i++)
        {
            ulong direction = QueenDirections[i](bitboard);

            while ((direction & _emptyBitboard) != 0)
            {
                direction &= _emptyBitboard;
                moves |= direction;
                direction = QueenDirections[i](direction);
            }
            if ((direction & enemyMask) != 0)
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
        ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);
        ulong enemyMaskCoverage = (_coverageSet ? (isWhite ? _blackCoverage : _whiteCoverage) : 0UL);

        ulong[] kingMoves = {
            North(bitboard), South(bitboard), East(bitboard), West(bitboard),
            NorthEast(bitboard), NorthWest(bitboard), SouthEast(bitboard), SouthWest(bitboard)
        };

        foreach (var move in kingMoves)
            if ((move & (_emptyBitboard | enemyMask) & ~enemyMaskCoverage) != 0)
                moves |= move;
        return moves;
    }

    private int AxisConstraint(ulong pieceBitboard, bool isWhite)
    {
        if ((pieceBitboard & (isWhite ? _whitePinned : _blackPinned)) != 0)
        {
            if ((pieceBitboard & (isWhite ? _bitboards[Piece['N']] : _bitboards[Piece['n']])) != 0)
                return 1;
            int[] kingPos = (isWhite ? _whiteKingPos : _blackKingPos);

            PrintBitBoard(pieceBitboard);
            if ((FileMasks[kingPos[0]] & pieceBitboard) != 0)
                return 1;
            else if ((RankMasks[kingPos[1]] & pieceBitboard) != 0)
                return 3;
            else 
            {
                int[] piecePos = BitboardToCoord(pieceBitboard);

                if ((piecePos[0] - piecePos[1]) == (kingPos[0] - kingPos[1]))
                    return 5;
                if ((piecePos[0] + piecePos[1]) == (kingPos[0] + kingPos[1]))
                    return 7;
            }
        }
        return 0;
    }

    public void GetAllMovesForPiece(char piece)
    {
        bool isWhite = char.IsUpper(piece);
        char pieceKey = char.ToLower(piece);

        if (_moveGenerators.TryGetValue(pieceKey, out var generateMoves))
        {
            ulong piecesMask = _bitboards[Piece[piece]];
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