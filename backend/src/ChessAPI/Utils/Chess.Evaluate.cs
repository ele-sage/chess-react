using System.Numerics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{
    private int CountMaterial(char piece)
    {
        return BitOperations.PopCount(_bitboards[piece]);
    }

    private int EvaluateBoard()
    {
        int whiteScore = CountMaterial('P') + CountMaterial('N') + CountMaterial('B') + CountMaterial('R') + CountMaterial('Q');
        int blackScore = CountMaterial('p') + CountMaterial('n') + CountMaterial('b') + CountMaterial('r') + CountMaterial('q');
        return whiteScore - blackScore;
    }

    private void GetPossibleMove()
    {
        Console.WriteLine(_possibleMove);
    }

    private int AlphaBeta(int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0)
        {
            _possibleMove++;
            return EvaluateBoard();
        }

        var moves = GetAllPossibleMoves(_turn);
        ulong emptyBitboard = _emptyBitboard;
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[] castle = [_castle[_turn == 'w' ? 0 : 1, 0], _castle[_turn == 'w' ? 0 : 1, 1]];

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;

            foreach (var move in moves)
            {
                ApplyMove(move);
                // if (depth - 2 == 0 && !IsUniqueBoardPosition())
                // {
                //     UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                //     continue;
                // }
                int eval = AlphaBeta(depth - 1, alpha, beta, false);
                UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break; // Beta cut-off
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;

            foreach (var move in moves)
            {
                ApplyMove(move);
                // if (depth - 2 == 0 && !IsUniqueBoardPosition())
                // {
                //     UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                //     continue;
                // }
                int eval = AlphaBeta(depth - 1, alpha, beta, true);
                UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break; // Alpha cut-off
                }
            }
            return minEval;
        }
    }

    public int Minimax(int depth, bool maximizingPlayer)
    {
        if (depth == 0)
        {
            _possibleMove++;
            return EvaluateBoard();
        }

        var moves = GetAllPossibleMoves(_turn);
        ulong emptyBitboard = _emptyBitboard;
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[] castle = [_castle[_turn == 'w' ? 0 : 1, 0], _castle[_turn == 'w' ? 0 : 1, 1]];

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            
            foreach (var move in moves)
            {
                ApplyMove(move);
                if (depth - 2 == 0 && !IsUniqueBoardPosition())
                {
                    UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                    continue;
                }
                int eval = Minimax(depth - 1, false);
                UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in moves)
            {
                ApplyMove(move);
                if (depth - 2 == 0 && !IsUniqueBoardPosition())
                {
                    UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                    continue;
                }
                int eval = Minimax(depth - 1, true);
                UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }
    }

    private void ApplyMove(Move move)
    {
        int color = _turn == 'w' ? 0 : 1;

        _bitboards[move.Piece] &= ~move.From;
        _bitboards[move.Piece] |= move.To;
        _enPassantMask = 0UL;
        _halfmove++;
        if ((move.To & _fullBitboard[color ^ 1]) != 0)
        {
            for (int j = 0; j < 6; j++)
            {
                if ((move.To & _bitboards[Pieces[color ^ 1,j]]) != 0)
                {
                    move.PrevPiece = Pieces[color ^ 1,j];
                    _bitboards[Pieces[color ^ 1,j]] &= ~move.To;
                    _halfmove = 0;
                    break;
                }
            }
        }
        if (move.Piece == 'P' || move.Piece == 'p')
        {
            _halfmove = 0;
            if (Math.Abs(BitOperations.TrailingZeroCount(move.To) - BitOperations.TrailingZeroCount(move.From)) == 16)
            {
                if (move.To > move.From)
                    _enPassantMask = move.To >> 8;
                else
                    _enPassantMask = move.To << 8;
            }
        }
        if (move.Piece == 'K' || move.Piece == 'k')
        {
            _castle[color,0] = false;
            _castle[color,1] = false;
        }
        else if (move.Piece == 'R' || move.Piece == 'r')
        {
            if (BitOperations.TrailingZeroCount(move.From) % 8 == 0)
                _castle[color,1] = false;
            else if (BitOperations.TrailingZeroCount(move.From) % 8 == 7)
                _castle[color,0] = false;
        }
        
        _turn = _turn == 'w' ? 'b' : 'w';
    }

    private void UndoMove(Move move, ulong emptyBitboard, ulong enPassantMask, ulong[] fullBitboard, bool[] castle)
    {
        _turn = _turn == 'w' ? 'b' : 'w';
        int color = _turn == 'w' ? 0 : 1;

        _bitboards[move.Piece] &= ~move.To;
        _bitboards[move.Piece] |= move.From;
        if (move.PrevPiece != '-')
            _bitboards[move.PrevPiece] |= move.To;
        _fullBitboard[0] = fullBitboard[0];
        _fullBitboard[1] = fullBitboard[1];
        _emptyBitboard = emptyBitboard;
        _enPassantMask = enPassantMask;
        _castle[color, 0] = castle[0];
        _castle[color, 1] = castle[1];
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
        _kingPos[color,0] = kingPos[0];
        _kingPos[color,1] = kingPos[1];
        _pinnedToKing[color] = PinnedToKing(color, RookDirections) | PinnedToKing(color, BishopDirections);
        SetCoverage(color ^ 1);
        IsCheck(color);


        for (int i = 0; i < 6; i++)
        {
            char pieceKey = char.ToLower(Pieces[color,i]);
            ulong piecesMask = _bitboards[Pieces[color,i]];

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
                    allMoves.AddRange(GetMoves(pieceBitboard, movesBitboards, Pieces[color,i]));
            }
        }
        return allMoves;
    }
}

// A simple move representation
public class Move(char piece, ulong from, ulong to)
{
    public char Piece { get; } = piece;
    public ulong From { get; } = from;
    public ulong To { get; } = to;
    public char PrevPiece { get; set; } = '-';
}
}

// private int AlphaBetaParallel(int depth, bool maximizingPlayer)
// {
//     int bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;
//     int alpha = int.MinValue;
//     int beta = int.MaxValue;
//     var moves = GetAllPossibleMoves(_turn);
//     ulong emptyBitboard = _emptyBitboard;
//     ulong enPassantMask = _enPassantMask;
//     ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
//     bool[] castle = [_castle[_turn == 'w' ? 0 : 1, 0], _castle[_turn == 'w' ? 0 : 1, 1]];
//     object lockObject = new();

//     Parallel.ForEach(moves, move =>{
//         Chess copy = new(this);

//         copy.ApplyMove(move);
//         int eval = copy.AlphaBeta(depth - 1, alpha, beta, !maximizingPlayer);
//         copy.UndoMove(move, emptyBitboard, enPassantMask, fullBitboard, castle);

//         lock (lockObject)
//         {
//             if (maximizingPlayer)
//             {
//                 bestEval = Math.Max(bestEval, eval);
//                 alpha = Math.Max(alpha, eval);
//             }
//             else
//             {
//                 bestEval = Math.Min(bestEval, eval);
//                 beta = Math.Min(beta, eval);
//             }
//         }
//     });
//     return bestEval;
// }