using System.Numerics;
using System.Diagnostics;

namespace ChessAPI
{
// Chess.Eval.cs
public partial class Chess
{
    private int EvaluatePieceCoverage()
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
                    int factor = color == c ? 1 : -1;
                    // Add material value
                    score += factor * PieceValues[i];
                    
                    // Add position bonus
                    int tableIndex = color == 0 ? pieceIndex : 63 - pieceIndex;
                    score += factor * PieceSquareTables[i][tableIndex] / 10;
                    
                    bitboard &= bitboard - 1;
                }
            }
        }
        // factor piece coverage and attacks
        score += BitOperations.PopCount(_pieceCoverage[c]) - BitOperations.PopCount(_pieceCoverage[c ^ 1]);
        score += BitOperations.PopCount(_pieceAttack[c]) - BitOperations.PopCount(_pieceAttack[c ^ 1]);
        return score;
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
                    int factor = color == c ? 1 : -1;
                    // Add material value
                    score += factor * PieceValues[i];
                    
                    // Add position bonus
                    int tableIndex = color == 0 ? pieceIndex : 63 - pieceIndex;
                    score += factor * PieceSquareTables[i][tableIndex] / 10;
                    
                    bitboard &= bitboard - 1;
                }
            }
        }
        return score;
    }

    private int EvaluatePeSTO()
    {
        int mgScore = 0;
        int egScore = 0;
        int gamePhase = 0;
        
        // Calculate material score and piece positioning
        for (int color = 0; color < 2; color++)
        {
            for (int pieceType = 0; pieceType < 6; pieceType++)
            {
                char piece = Pieces[color, pieceType];
                ulong bitboard = _bitboards[piece];
                int tableIndex = color * 6 + pieceType; // Index for mg_table/eg_table
                
                while (bitboard != 0)
                {
                    ulong piecePos = bitboard & ~(bitboard - 1);
                    bitboard &= bitboard - 1;
                    
                    int square = BitOperations.TrailingZeroCount(piecePos);
                    
                    // Add material and position scores
                    if (color == 0) // White
                    {
                        mgScore += PieceValuesMG[pieceType] + mg_table[tableIndex, square];
                        egScore += PieceValuesEG[pieceType] + eg_table[tableIndex, square];
                    }
                    else // Black
                    {
                        mgScore -= PieceValuesMG[pieceType] + mg_table[tableIndex, square];
                        egScore -= PieceValuesEG[pieceType] + eg_table[tableIndex, square];
                    }
                    
                    // Accumulate phase
                    gamePhase += PhaseValues[pieceType];
                }
            }
        }
        
        // Adjust score from the perspective of the side to move
        int perspective = _turn == 'w' ? 1 : -1;
        
        // Calculate interpolation factor based on game phase
        gamePhase = Math.Min(gamePhase, TotalPhase);
        int phase = (gamePhase * 256 + (TotalPhase / 2)) / TotalPhase;
        
        // Interpolate between middlegame and endgame scores
        int score = ((mgScore * phase) + (egScore * (256 - phase))) / 256;
        
        // Return score from the perspective of the side to move
        return score * perspective;
    }
}
}