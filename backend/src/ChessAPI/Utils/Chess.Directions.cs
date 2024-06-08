namespace ChessAPI
{
public partial class Chess
{
    private static ulong North(ulong bitboard) => bitboard >> Size;
    private static ulong South(ulong bitboard) => bitboard << Size;
    private static ulong East(ulong bitboard) => (bitboard << 1) & ~FileMasks[0];
    private static ulong West(ulong bitboard) => (bitboard >> 1) & ~FileMasks[Size - 1];
    private static ulong NorthEast(ulong bitboard) => (bitboard >> Size - 1) & ~FileMasks[0];
    private static ulong NorthWest(ulong bitboard) => (bitboard >> Size + 1) & ~FileMasks[Size - 1];
    private static ulong SouthEast(ulong bitboard) => (bitboard << Size + 1) & ~FileMasks[0];
    private static ulong SouthWest(ulong bitboard) => (bitboard << Size - 1) & ~FileMasks[Size - 1];

    private static readonly Func<ulong, ulong>[] RookDirections = 
    {
        bitboard => North(bitboard),
        bitboard => South(bitboard),
        bitboard => East(bitboard),
        bitboard => West(bitboard)
    };
    private static readonly Func<ulong, ulong>[] BishopDirections = 
    {
        bitboard => NorthEast(bitboard),
        bitboard => SouthWest(bitboard),
        bitboard => NorthWest(bitboard),
        bitboard => SouthEast(bitboard)
    };
    private static readonly Func<ulong, ulong>[] QueenDirections = RookDirections.Concat(BishopDirections).ToArray();

    // Knight moves
    private static ulong KnightNE(ulong bitboard) => (bitboard >> 17) & 0x7F7F7F7F7F7F7F7F;
    private static ulong KnightNW(ulong bitboard) => (bitboard >> 15) & 0xFEFEFEFEFEFEFEFE;
    private static ulong KnightSE(ulong bitboard) => (bitboard << 15) & 0x7F7F7F7F7F7F7F7F;
    private static ulong KnightSW(ulong bitboard) => (bitboard << 17) & 0xFEFEFEFEFEFEFEFE;
    private static ulong KnightEN(ulong bitboard) => (bitboard >> 10) & 0x3F3F3F3F3F3F3F3F;
    private static ulong KnightES(ulong bitboard) => (bitboard << 6) & 0x3F3F3F3F3F3F3F3F;
    private static ulong KnightWN(ulong bitboard) => (bitboard >> 6) & 0xFCFCFCFCFCFCFCFC;
    private static ulong KnightWS(ulong bitboard) => (bitboard << 10) & 0xFCFCFCFCFCFCFCFC;
}
}