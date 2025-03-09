using System.Numerics;

namespace ChessAPI
{
// Chess.Search.cs
public partial class Chess
{   
    private int QuiescenceSearch(int alpha, int beta, int depth = 0)
    {
        _possibleMove++;
        
        // First get a static evaluation as a baseline score
        int standPat = _evaluate();
        
        // Check for immediate checkmates
        int color = _turn == 'w' ? 0 : 1;
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        
        // If in check, we must consider all moves, not just captures
        if (_checkBy[color].Count > 0)
        {
            // If no moves available while in check, it's checkmate
            if (attacks.Length == 0 && otherMoves.Length == 0)
            {
                return -10000; // Return a very negative score (checkmate against us)
            }
            
            // If in check, we need to consider all legal moves, not just captures
            attacks = attacks.Concat(otherMoves).ToArray();
        }
        else
        {
            // Not in check, we can apply stand-pat and consider only captures
            if (standPat >= beta)
                return beta;
            if (alpha < standPat)
                alpha = standPat;
        }
        
        // Prevent too deep quiescence searches
        if (depth >= 4 || _stopwatch.ElapsedMilliseconds > _timeLimit)
            return standPat;
        
        if (attacks.Length == 0)
            return standPat;
        
        // Save board state
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
        int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
        ulong pinnedToKing = _pinnedToKing[color];
        
        foreach (Move move in attacks)
        {
            ApplyMove(move);
            int score = -QuiescenceSearch(-beta, -alpha, depth + 1);
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
            
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }
        
        return alpha;
    }

    private int AlphaBeta(int depth, int alpha, int beta)
    {
        _possibleMove++;
        
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
        


        if (depth <= 0 || _stopwatch.ElapsedMilliseconds > _timeLimit)
            return QuiescenceSearch(alpha, beta);
        
        int color = _turn == 'w' ? 0 : 1;
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] moves = [.. attacks, .. otherMoves];
        
        // Check for cutoff conditions
        if (moves.Length == 0)
        {
            int score;
            if (_checkBy[color].Count == 0)
                score = 0; // Stalemate
            else
                score = -10000 + depth; // Checkmate
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
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;
        int color = _turn == 'w' ? 0 : 1;
        
        // Try to get the previous best move from the transposition table
        Move? previousBestMove = null;
        ulong hash = ComputeHash();
        if (_transpositionTable.TryGetValue(hash, out var entry))
        {
            previousBestMove = entry.bestMove;
        }
        
        // Get and order all legal moves
        var (attacks, otherMoves) = GetAllPossibleMoves(_turn);
        Move[] moves = [.. attacks, .. otherMoves];
        
        // Move ordering: try the previous best move first if available
        if (previousBestMove != null)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].Equals(previousBestMove))
                {
                    // Swap with the first position
                    if (i > 0)
                    {
                        (moves[i], moves[0]) = (moves[0], moves[i]);
                    }
                    break;
                }
            }
        }

        // Rest of your function...
        ulong enPassantMask = _enPassantMask;
        ulong[] fullBitboard = [_fullBitboard[0], _fullBitboard[1]];
        bool[,] castle = {{ _castle[0, 0], _castle[0, 1] }, { _castle[1, 0], _castle[1, 1] }};
        int[,] kingPos = {{ _kingPos[0, 0], _kingPos[0, 1] }, { _kingPos[1, 0], _kingPos[1, 1] }};
        ulong pinnedToKing = _pinnedToKing[color];
        
        int bestScore = int.MinValue + 1;
        
        foreach (Move move in moves)
        {
            if (_stopwatch.ElapsedMilliseconds > _timeLimit)
                return (bestMove, false);

            ApplyMove(move);
            int score = -AlphaBeta(depth - 1, -beta, -alpha);
            UndoMove(move, enPassantMask, fullBitboard, castle, kingPos, pinnedToKing);
            move.Score = score;
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                
                if (score > alpha)
                    alpha = score;
            }
        }
        
        // Store result in the transposition table
        _transpositionTable.Store(hash, depth, bestScore, TranspositionTable.EXACT, bestMove);
        
        Console.WriteLine($"Depth {depth}: {_possibleMove} positions, score: {bestScore}");
        return (bestMove ?? new Move('-', 0UL, 0UL), true);
    }

    private Move IterativeDeepening(int maxDepth)
    {
        Move? bestMove = null;
        _stopwatch.Reset();
        _stopwatch.Start();

        for (int depth = 1; depth <= maxDepth; depth++)
        {
            Console.WriteLine($"Searching depth {depth}...");

            if (_stopwatch.ElapsedMilliseconds > _timeLimit * 0.8) // Use 80% of time limit as cutoff
                break;
                
            var (currentBestMove, searchCompleted) = GetBestMove(depth);
            
            if (searchCompleted && currentBestMove != null && currentBestMove.Piece != '-')
            {
                bestMove = currentBestMove;
                if (bestMove.Score <= -9000 || bestMove.Score >= 9000)
                    break;
                Console.WriteLine($"Best move so far: {bestMove}");
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

    private (Move[], Move[]) GetAllPossibleMoves(char turn)
    {
        int color = turn == 'w' ? 0 : 1;

        SetFullBitboard(color);
        SetFullBitboard(color ^ 1);
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        int[] kingPos = BitboardToCoord(turn == 'w' ? _bitboards['K'] : _bitboards['k']);
        _kingPos[color, 0] = kingPos[0];
        _kingPos[color, 1] = kingPos[1];
        _pinnedToKing[color] = PinnedToKing(color, RookDirections) | PinnedToKing(color, BishopDirections);
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
        // Order attacks by value
        // attacks = [.. OrderAttacksByValue(attacks, color)];

        return ([.. attacks], [.. moves]);
    }

    private List<Move> OrderAttacksByValue(List<Move> attacks, int attackerColor)
    {
        // Sort attacks by MVV-LVA order
        return [.. attacks.OrderByDescending(attack => {
            // Get the captured piece and its value
            int victimValue = GetCapturedPieceValue(attack.To, attackerColor ^ 1);
            
            // Get the value of the attacking piece
            int attackerValue = 0;
            char attackingPiece = attack.Piece;
            if (char.IsUpper(attackingPiece)) // White piece
                attackerValue = PieceValues["PNBRQK".IndexOf(attackingPiece)];
            else // Black piece
                attackerValue = PieceValues["pnbrqk".IndexOf(char.ToLower(attackingPiece))];
            
            // MVV-LVA formula: 10 * victim value - attacker value
            // This prioritizes capturing higher value pieces with lower value pieces
            return victimValue * 10 - attackerValue;
        })];
    }

    private int GetCapturedPieceValue(ulong targetSquare, int victimColor)
    {
        for (int i = 0; i < 6; i++)
        {
            if ((_bitboards[Pieces[victimColor, i]] & targetSquare) != 0)
                return PieceValues[i];
        }
        
        // En passant capture (value of a pawn)
        if ((_enPassantMask & targetSquare) != 0)
            return PieceValues[0]; // Pawn value
        
        return 0; // No piece captured (shouldn't happen for attacks)
    }

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
}
}