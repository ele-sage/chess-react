using System.Numerics;

namespace ChessAPI
{
public class TranspositionTable
{
    private readonly (ulong key, int depth, int value, int flag, Move? bestMove)[] _table;
    private readonly ulong _sizeMask;  // Changed from int to ulong
    public const int EXACT = 0, LOWERBOUND = 1, UPPERBOUND = 2;
    public int Age { get; set; } = 0;

    public TranspositionTable(int sizeMB = 64)
    {
        // Calculate size as power of 2 for efficient modulo operation
        int entries = (sizeMB * 1024 * 1024) / 32; // 32 bytes per entry approx
        int size = 1;
        while (size < entries) size *= 2;
        _sizeMask = (ulong)size - 1;  // Cast to ulong
        _table = new (ulong, int, int, int, Move?)[size];
    }

    public bool TryGetValue(ulong key, out (int depth, int value, int flag, Move? bestMove) entry)
    {
        int index = (int)(key & _sizeMask);
        if (_table[index].key == key)
        {
            entry = (_table[index].depth, _table[index].value, _table[index].flag, _table[index].bestMove);
            return true;
        }
        entry = default;
        return false;
    }

    public void Store(ulong key, int depth, int value, int flag, Move? bestMove = null)
    {
        int index = (int)(key & _sizeMask);
        
        // Replace if empty or if new position is searched deeper
        if (_table[index].key == 0 || depth >= _table[index].depth)
        {
            _table[index] = (key, depth, value, flag, bestMove);
        }
    }
    
    public void Clear()
    {
        Array.Clear(_table, 0, _table.Length);
        Age++;
    }
}
public partial class Chess
{
    private static readonly ulong[,] ZobristTable = new ulong[64, 12];
    private static readonly ulong BlackToMoveHash;
    private static readonly ulong WhiteKingsideCastleHash;
    private static readonly ulong WhiteQueensideCastleHash;
    private static readonly ulong BlackKingsideCastleHash;
    private static readonly ulong BlackQueensideCastleHash;
    private static readonly ulong[] EnPassantHash = new ulong[8];
    
    private static readonly Dictionary<char, int> PieceToIndex = new()
    {
        {'P', 0}, {'N', 1}, {'B', 2}, {'R', 3}, {'Q', 4}, {'K', 5},
        {'p', 6}, {'n', 7}, {'b', 8}, {'r', 9}, {'q', 10}, {'k', 11}
    };
    private TranspositionTable _transpositionTable = new();
    
    // Fix initialization
    static Chess()
    {
        Random rand = new();
        for (int i = 0; i < 64; i++)
        {
            for (int j = 0; j < 12; j++)
            {
                // Create full 64-bit random values with proper casting
                ulong r1 = (ulong)(uint)rand.Next();
                ulong r2 = (ulong)(uint)rand.Next();
                ZobristTable[i, j] = (r1 << 32) | r2;
            }
        }
        
        // Initialize side to move hash with proper casting
        ulong b1 = (ulong)(uint)rand.Next();
        ulong b2 = (ulong)(uint)rand.Next();
        BlackToMoveHash = (b1 << 32) | b2;
        
        // Initialize castling rights hashes with proper casting
        WhiteKingsideCastleHash = ((ulong)(uint)rand.Next() << 32) | (ulong)(uint)rand.Next();
        WhiteQueensideCastleHash = ((ulong)(uint)rand.Next() << 32) | (ulong)(uint)rand.Next();
        BlackKingsideCastleHash = ((ulong)(uint)rand.Next() << 32) | (ulong)(uint)rand.Next();
        BlackQueensideCastleHash = ((ulong)(uint)rand.Next() << 32) | (ulong)(uint)rand.Next();
        
        // Initialize en passant hashes with proper casting
        for (int file = 0; file < 8; file++)
        {
            EnPassantHash[file] = ((ulong)(uint)rand.Next() << 32) | (ulong)(uint)rand.Next();
        }
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
            
        // Add castling rights to hash
        if (_castle[0, 0]) hash ^= WhiteKingsideCastleHash;
        if (_castle[0, 1]) hash ^= WhiteQueensideCastleHash;
        if (_castle[1, 0]) hash ^= BlackKingsideCastleHash;
        if (_castle[1, 1]) hash ^= BlackQueensideCastleHash;
        
        // Add en passant possibility to hash
        if (_enPassantMask != 0)
        {
            int square = BitOperations.TrailingZeroCount(_enPassantMask);
            hash ^= EnPassantHash[square % 8]; // Just need the file
        }
        
        return hash;
    }
}
}