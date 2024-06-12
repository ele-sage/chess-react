namespace ChessAPI
{
// Chess.Directions.cs
public partial class Chess
{
    
    private static readonly ulong[,] CastleSpace = {{0x6000000000000000UL, 0x0E00000000000000UL},{0x0000000000000060UL, 0x000000000000000EUL}};

    private static ulong Self(ulong bitboard) => bitboard;
    private static ulong North(ulong bitboard) => bitboard >> Size;
    private static ulong South(ulong bitboard) => bitboard << Size;
    private static ulong East(ulong bitboard) => (bitboard << 1) & ~FileMasks[0];
    private static ulong West(ulong bitboard) => (bitboard >> 1) & ~FileMasks[Size - 1];
    private static ulong NorthEast(ulong bitboard) => (bitboard >> Size - 1) & ~FileMasks[0];
    private static ulong NorthWest(ulong bitboard) => (bitboard >> Size + 1) & ~FileMasks[Size - 1];
    private static ulong SouthEast(ulong bitboard) => (bitboard << Size + 1) & ~FileMasks[0];
    private static ulong SouthWest(ulong bitboard) => (bitboard << Size - 1) & ~FileMasks[Size - 1];

    private static readonly Func<ulong, ulong>[] RookDirections = [North, South, East, West];
    private static readonly Func<ulong, ulong>[] BishopDirections = [NorthEast, SouthWest, NorthWest, SouthEast];
    private static readonly Func<ulong, ulong>[] QueenDirections = [.. BishopDirections, .. RookDirections];
    private static readonly Func<ulong, ulong>[] PawnDirection = [North, South];
    private static readonly Func<ulong, ulong>[,] PawnAttack = {{NorthEast, NorthWest}, {SouthWest, SouthEast}};

    private static readonly ulong[] KnightMasks = [0x7F7F7F7F7F7F7F7FUL, 0xFEFEFEFEFEFEFEFEUL, 0x3F3F3F3F3F3F3F3FUL, 0xFCFCFCFCFCFCFCFCUL];
    private static ulong KnightMoves(ulong bitboard) => (bitboard >> 17) & KnightMasks[0] | (bitboard >> 15) & KnightMasks[1] | (bitboard << 15) & KnightMasks[0] | (bitboard << 17) & KnightMasks[1]
                                                        | (bitboard >> 10) & KnightMasks[2] | (bitboard << 6) & KnightMasks[2] | (bitboard >> 6) & KnightMasks[3] | (bitboard << 10) & KnightMasks[3];
}
}