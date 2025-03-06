using System.Numerics;
using System.Diagnostics;

namespace ChessAPI
{
// Chess.Evaluate.cs
public partial class Chess
{
    private int PeSTO_Eval()
    {
        int mg_score = 0;
        int eg_score = 0;
        int gamePhase = 0;
        int c = _turn == 'w' ? 0 : 1;
        
        // Calculate material and position scores for both middlegame and endgame
        for (int color = 0; color < 2; color++)
        {
            for (int i = 0; i < 6; i++)
            {
                char piece = Pieces[color, i];
                ulong bitboard = _bitboards[piece];
                
                while (bitboard != 0)
                {
                    ulong piecePos = bitboard & ~(bitboard - 1);
                    int pieceIndex = BitOperations.TrailingZeroCount(piecePos);
                    
                    // Add phase increment for this piece (except pawns and kings)
                    int pieceType = i + color * 6;
                    gamePhase += ChessConstants.gamephaseInc[pieceType];
                    
                    // Add position scores for middlegame and endgame
                    int tableIndex = color == 0 ? pieceIndex : 63 - pieceIndex;
                    int factor = color == c ? 1 : -1;
                    
                    mg_score += factor * ChessConstants.mg_table[pieceType, tableIndex];
                    eg_score += factor * ChessConstants.eg_table[pieceType, tableIndex];
                    
                    bitboard &= bitboard - 1;
                }
            }
        }
        
        // Determine the phase of the game (0 = middlegame, 24 = endgame)
        const int totalPhase = 24; // Maximum possible game phase value
        gamePhase = Math.Min(gamePhase, totalPhase);
        
        // Interpolate between middlegame and endgame scores
        int phase = (gamePhase * 256 + (totalPhase / 2)) / totalPhase;
        int finalScore = ((mg_score * (256 - phase)) + (eg_score * phase)) / 256;
        
        return finalScore;
    }

    private int Evaluate()
    {
        int score = 0;
        int c = _turn == 'w' ? 0 : 1;
        
        // Material evaluation
        for (int color = 0; color < 2; color++)
        {
            for (int i = 0; i < 6; i++)
            {
                char piece = Pieces[color, i];
                ulong bitboard = _bitboards[piece];
                
                while (bitboard != 0)
                {
                    ulong piecePos = bitboard & ~(bitboard - 1);
                    int pieceIndex = BitOperations.TrailingZeroCount(piecePos);
                    
                    // Add material value
                    score += (color == c ? 1 : -1) * PieceValues[i];
                    
                    // Add position bonus
                    int tableIndex = color == 0 ? pieceIndex : 63 - pieceIndex;
                    score += (color == c ? 1 : -1) * PieceSquareTables[i][tableIndex] / 10;
                    
                    bitboard &= bitboard - 1;
                }
            }
        }
        
        return score;
    }

    private int AlphaBeta(int depth, int alpha, int beta)
    {
        _possibleMove++;
        if (depth == 0)
            return Evaluate();
            // return PeSTO_Eval();
        
        ulong hash = ComputeHash();
        if (_transpositionTable.TryGetValue(hash, out var entry) && entry.depth >= depth)
        {
            if (entry.flag == TranspositionTable.EXACT)
                return entry.value;
            else if (entry.flag == TranspositionTable.LOWERBOUND && entry.value > alpha)
                alpha = entry.value;
            else if (entry.flag == TranspositionTable.UPPERBOUND && entry.value < beta)
                beta = entry.value;
            
            if (alpha >= beta)
                return entry.value;
        }
        
        int color = _turn == 'w' ? 0 : 1;
        Move[] moves = GetAllPossibleMoves(_turn);
        
        if (moves.Length == 0)
        {
            int score;
            if (_checkBy[color].Count == 0) // Stalemate
                score = 0;
            else // Checkmate - adjust based on depth so immediate checkmates are valued higher
                score = -10000 + depth; 

            _transpositionTable.Store(hash, depth, score, TranspositionTable.EXACT);
            return score;
        }
        
        // Save board state
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
        int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
        ulong pinnedToKing = _pinnedToKing[color];
        
        int bestScore = int.MinValue;
        Move? bestMove = null;
        int alphaOrig = alpha;
        
        foreach (Move move in moves)
        {
            ApplyMove(move);
            int score = -AlphaBeta(depth - 1, -beta, -alpha);
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                
                if (score > alpha)
                    alpha = score;
                    
                if (alpha >= beta)
                    break; // Beta cutoff
            }
        }

        int flag;
        if (bestScore <= alphaOrig)
            flag = TranspositionTable.UPPERBOUND;
        else if (bestScore >= beta)
            flag = TranspositionTable.LOWERBOUND;
        else
            flag = TranspositionTable.EXACT;
            

        _transpositionTable.Store(hash, depth, bestScore, flag, bestMove);
        
        return bestScore;
    }
    private (Move?, bool) GetBestMove(int depth)
    {
        Move? bestMove = null;
        int alpha = int.MinValue + 1; // Avoid using exact int.MinValue
        int beta = int.MaxValue;
        int color = _turn == 'w' ? 0 : 1;
        Move[] moves = GetAllPossibleMoves(_turn);

        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
        int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
        ulong pinnedToKing = _pinnedToKing[color];
        
        int bestScore = int.MinValue + 1;
        ulong hash = ComputeHash();
        
        foreach (Move move in moves)
        {
            if (_stopwatch.ElapsedMilliseconds > _timeLimit)
                return (bestMove, false);

            ApplyMove(move);
            int score = -AlphaBeta(depth - 1, -beta, -alpha);
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                
                if (score > alpha)
                    alpha = score;
            }
        }
        _transpositionTable.Store(hash, depth, bestScore, TranspositionTable.EXACT, bestMove);
    
        Console.WriteLine($"Total number of moves considered: {_possibleMove}");
        Console.WriteLine($"Best move score: {bestScore}");
        Console.WriteLine($"Best move: {bestMove}");
        return (bestMove ?? new Move('-', 0, 0), true);
    }

    private Move IterativeDeepening(int maxDepth)
    {
        Move? bestMove = null;
        _stopwatch.Reset();
        _stopwatch.Start();
        
        for (int depth = 4; depth <= maxDepth; depth++)
        {
            Console.WriteLine($"Searching depth {depth}...");

            if (_stopwatch.ElapsedMilliseconds > _timeLimit * 0.8) // Use 80% of time limit as cutoff
                break;
                
            var (currentBestMove, searchCompleted) = GetBestMove(depth);
            
            if (searchCompleted && currentBestMove != null && currentBestMove.Piece != '-')
            {
                bestMove = currentBestMove;
            }
            else if (!searchCompleted)
            {
                // Use previous best move
                break;
            }
        }
        
        _stopwatch.Stop();
        return bestMove ?? new Move('-', 0, 0);
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
}