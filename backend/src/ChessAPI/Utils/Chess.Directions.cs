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

    private static readonly Func<ulong, ulong>[] RookDirections = [ North, South, East, West ];
    private static readonly Func<ulong, ulong>[] BishopDirections = [ NorthEast, SouthWest, NorthWest, SouthEast ];
    private static readonly Func<ulong, ulong>[] QueenDirections = [.. BishopDirections, .. RookDirections];

    private static readonly Func<char, char>[] Cap = [ char.ToUpper, char.ToLower ];

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