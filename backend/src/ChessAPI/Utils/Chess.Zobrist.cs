using System.Numerics;

namespace ChessAPI
{
public class TranspositionTable
{
    private readonly Dictionary<ulong, (int depth, int value, int flag)> table;
    public const int EXACT = 0, LOWERBOUND = 1, UPPERBOUND = 2;

    public TranspositionTable()
    {
        table = [];
    }

    public bool TryGetValue(ulong key, out (int depth, int value, int flag) entry)
    {
        return table.TryGetValue(key, out entry);
    }

    public void Store(ulong key, int depth, int value, int flag)
    {
        table[key] = (depth, value, flag);
    }
}

public partial class Chess
{
    private static readonly ulong[,] ZobristTable = new ulong[64, 12];
    private static readonly ulong BlackToMoveHash;
    private static readonly Dictionary<char, int> PieceToIndex = new()
    {
        {'P', 0}, {'N', 1}, {'B', 2}, {'R', 3}, {'Q', 4}, {'K', 5},
        {'p', 6}, {'n', 7}, {'b', 8}, {'r', 9}, {'q', 10}, {'k', 11}
    };
    private TranspositionTable _transpositionTable = new();
    static Chess()
    {
        Random rand = new();
        for (int i = 0; i < 64; i++)
        {
            for (int j = 0; j < 12; j++)
            {
                ZobristTable[i, j] = (ulong)rand.Next();
            }
        }
        BlackToMoveHash = (ulong)rand.Next();
    }
    private ulong ComputeHash()
    {
        ulong hash = 0UL;
        foreach (var kvp in _bitboards)
        {
            char piece = kvp.Key;
            ulong bitboard = kvp.Value;
            int pieceIndex = PieceToIndex[piece];
            while (bitboard != 0)
            {
                ulong piecePos = bitboard & ~(bitboard - 1);
                int square = BitOperations.TrailingZeroCount(piecePos);
                hash ^= ZobristTable[square, pieceIndex];
                bitboard &= bitboard - 1;
            }
        }
        if (_turn == 'b')
            hash ^= BlackToMoveHash;
        return hash;
    }
}
}