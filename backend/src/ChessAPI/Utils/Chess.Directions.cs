namespace ChessAPI
{
// Chess.Directions.cs
public partial class Chess
{
    private const int Size = 8;
    private const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private static readonly char[] PieceSymbols = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'];
    private static readonly char[,] Pieces = {{'P', 'N', 'B', 'R', 'Q', 'K'}, {'p', 'n', 'b', 'r', 'q', 'k'}};
    private static readonly string[] PiecesString = ["PNBRQK", "pnbrqk"];
    private static readonly ulong[] FileMasks = [
        0x0101010101010101UL, 0x0202020202020202UL, 0x0404040404040404UL, 0x0808080808080808UL,
        0x1010101010101010UL, 0x2020202020202020UL, 0x4040404040404040UL, 0x8080808080808080UL
    ];
    private static readonly ulong[] RankMasks = [
        0xFF00000000000000UL, 0x00FF000000000000UL, 0x0000FF0000000000UL, 0x000000FF00000000UL,
        0x00000000FF000000UL, 0x0000000000FF0000UL, 0x000000000000FF00UL, 0x00000000000000FFUL
    ];

    private static readonly ulong[,] CastleSpace = {{0x6000000000000000UL, 0x0E00000000000000UL},{0x0000000000000060UL, 0x000000000000000EUL}};
    private static readonly ulong[,] RookPositions = {{0x8000000000000000UL, 0x0100000000000000UL},{0x0000000000000080UL, 0x0000000000000001UL}};
    private static readonly ulong[,] RookCastlePositions = {{0x2000000000000000UL, 0x0800000000000000UL},{0x0000000000000020UL, 0x0000000000000008UL}};
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
    private static readonly int[] PieceValues = [100, 320, 330, 500, 900, 20000]; // P, N, B, R, Q, K
    private static readonly int[][] PieceSquareTables = [
        // Pawn
        [
            9000,9000,9000,9000,9000,9000,9000,9000,
             200, 200, 200, 200, 200, 200, 200, 200,
             100, 100, 100, 100, 100, 100, 100, 100,
              40,  40,  90, 100, 100,  90,  40,  40,
              20,  20,  20, 100, 150,  20,  20,  20,
               2,   4,   0,  15,   4,   0,   4,   2,
             -10, -10, -10, -20, -35, -10, -10, -10,
               0,   0,   0,   0,   0,   0,   0,   0
        ],
        // Knight
        [
            -20, -80, -60, -60, -60, -60, -80, -20,
            -80, -40,   0,   0,   0,   0, -40, -80,
            -60,   0,  20,  30,  30,  20,   0, -60, 
            -60,  10,  30,  40,  40,  30,  10, -60,
            -60,   0,  30,  40,  40,  30,   0, -60, 
            -60,  10,  20,  30,  30,  30,   1, -60,
            -80, -40,   0,  10,  10,   0,  -4, -80,
            -20, -80, -60, -60, -60, -60, -80, -20
        ],
        // Bishop
        [
            -40, -20, -20, -20, -20, -20, -20, -40,
            -20,   0,   0,   0,   0,   0,   0, -20,
            -20,   0,  10,  20,  20,  10,   0, -20,
            -20,  10,  10,  20,  20,  10,  10, -20,
            -20,   0,  20,  20,  20,  20,   0, -20,
            -20,  20,  20,  20,  20,  20,  20, -20,
            -20,  10,   0,   0,   0,   0,  10, -20,
            -40, -20, -20, -20, -20, -20, -20, -40 
        ],
        // Rook
        [
              0,  0,  0,  0,  0,  0,  0,   0,
             10, 20, 20, 20, 20, 20, 20,  10,
            -10,  0,  0,  0,  0,  0,  0, -10,
            -10,  0,  0,  0,  0,  0,  0, -10,
            -10,  0,  0,  0,  0,  0,  0, -10,
            -10,  0,  0,  0,  0,  0,  0, -10, 
            -10,  0,  0,  0,  0,  0,  0, -10,
            -30, 30, 40, 10, 10,  0,  0, -30
        ],
        // Queen
        [
            -40, -20, -20, -10, -10, -20, -20, -40,
            -20,   0,   0,   0,   0,   0,   0, -20,
            -20,   0,  10,  10,  10,  10,   0, -20,
            -10,   0,  10,  10,  10,  10,   0, -10,
              0,   0,  10,  10,  10,  10,   0, -10,
            -20,  10,  10,  10,  10,  10,   0, -20,
            -20,   0,  10,   0,   0,   0,   0, -20,
            -40, -20, -20, -10, -10, -20, -20, -40 
        ],
        // King
        [
            -60, -80, -80, -2, -20, -80, -80, -60,
            -60, -80, -80, -2, -20, -80, -80, -60,
            -60, -80, -80, -2, -20, -80, -80, -60,
            -60, -80, -80, -2, -20, -80, -80, -60,
            -40, -60, -60, -8, -80, -60, -60, -40,
            -20, -40, -40, -40,-40, -40, -40, -20,
             40,  40,   0,   0,  0,   0,  40,  40,
             40,  60,  20,   0,  0,  20,  60,  40
        ]
    ];
}
}