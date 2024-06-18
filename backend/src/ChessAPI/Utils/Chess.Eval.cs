using System.Numerics;
using System.Diagnostics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{
    private int Evaluate()
    {
        int score = 0;
        // SetCoverage(_turn == 'w' ? 1 : 0);
        for (int i = 0; i < 6; i++)
        {
            char piece = Pieces[0, i];
            ulong bitboard = _bitboards[piece];
            while (bitboard != 0)
            {
                ulong piecePos = bitboard & ~(bitboard - 1);
                int pieceIndex = BitOperations.TrailingZeroCount(piecePos);
                int pieceValue = PieceValues[i];
                if ((_pieceCoverage[1] & (1UL << pieceIndex)) != 0)
                    pieceValue >>= 1;
                score += pieceValue + PieceSquareTables[i][pieceIndex];
                bitboard &= bitboard - 1;
            }
        }
        for (int i = 0; i < 6; i++)
        {
            char piece = Pieces[1, i];
            ulong bitboard = _bitboards[piece];
            while (bitboard != 0)
            {
                ulong piecePos = bitboard & ~(bitboard - 1);
                int pieceIndex = BitOperations.TrailingZeroCount(piecePos);
                int pieceValue = PieceValues[i];
                if ((_pieceCoverage[0] & (1UL << pieceIndex)) != 0)
                    pieceValue >>= 1;
                score -= pieceValue + PieceSquareTables[i][63 - pieceIndex];
                bitboard &= bitboard - 1;
            }
        }
        _possibleMove++;
        return score;
    }

    private int AlphaBeta(int depth, int alpha, int beta, bool isMaximizingPlayer, Stopwatch stopwatch)
    {
        if (depth == 0)
            return Evaluate();
        if (stopwatch.ElapsedMilliseconds >= _timeLimitMillis)
            return isMaximizingPlayer ? int.MinValue : int.MaxValue;
        ulong hash = ComputeHash();
        if (_transpositionTable.TryGetValue(hash, out var entry) && entry.depth >= depth)
        {
            if (entry.flag == TranspositionTable.EXACT)
                return entry.value;
            if (entry.flag == TranspositionTable.LOWERBOUND)
                alpha = Math.Max(alpha, entry.value);
            else
                beta = Math.Min(beta, entry.value);
            if (alpha >= beta)
                return entry.value;
        }
        int color = _turn == 'w' ? 0 : 1;
        List<Move> moves = GetAllPossibleMoves(_turn);
        if (moves.Count == 0)
        {
            if (_checkBy[color].Count == 0)
                return 0; // Stalemate
            else
                return isMaximizingPlayer ? int.MinValue : int.MaxValue; // Checkmate
        }
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = { _fullBitboard[0], _fullBitboard[1] };
        bool[,] castle = { { _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] } };
        int[,] kingPos = { { _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] } };
        ulong pinnedToKing = _pinnedToKing[color];
        if (isMaximizingPlayer)
        {
            int value = int.MinValue;
            foreach (Move move in moves)
            {
                ApplyMove(move);
                value = Math.Max(value, AlphaBeta(depth - 1, alpha, beta, false, stopwatch));
                alpha = Math.Max(alpha, value);
                UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
                if (alpha >= beta)
                    break;
            }
            _transpositionTable.Store(hash, depth, value, value <= alpha ? TranspositionTable.UPPERBOUND : TranspositionTable.EXACT);
            return value;
        }
        else
        {
            int value = int.MaxValue;
            foreach (Move move in moves)
            {
                ApplyMove(move);
                value = Math.Min(value, AlphaBeta(depth - 1, alpha, beta, true, stopwatch));
                beta = Math.Min(beta, value);
                UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
                if (alpha >= beta)
                    break;
            }
            _transpositionTable.Store(hash, depth, value, value >= beta ? TranspositionTable.LOWERBOUND : TranspositionTable.EXACT);
            return value;
        }
    }

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

    private void UndoMove(Move move, ulong enPassantMask, ulong[] fullBitboard, bool[,] castle, int[,] kingPos, ulong pinnedToKing, bool setMoves = false)
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
        _fullBitboard[color] = fullBitboard[color];
        _fullBitboard[color ^ 1] = fullBitboard[color ^ 1];
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        _kingPos[0, 0] = kingPos[0,0];
        _kingPos[0, 1] = kingPos[0,1];
        _kingPos[1, 0] = kingPos[1,0];
        _kingPos[1, 1] = kingPos[1,1];
        _pinnedToKing[color] = pinnedToKing;
        if (setMoves)
        {
            SetCoverage(color ^ 1);
            SetCoverage(color);
            IsCheck(color);
        }
    }
}
}