using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace ChessAPI
{
public class Chess
{
    
    private const ulong FULL = 0xFFFFFFFFFFFFFFFFUL;
    private const int Size = 8;
    private const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private static readonly Dictionary<char, int> Piece = new()
    {
        {'P', 0}, {'N', 1}, {'B', 2}, {'R', 3}, {'Q', 4}, {'K', 5},
        {'p', 6}, {'n', 7}, {'b', 8}, {'r', 9}, {'q', 10}, {'k', 11}
    };

    private static readonly ulong[] FileMasks = [
        0x0101010101010101UL, 0x0202020202020202UL, 0x0404040404040404UL, 0x0808080808080808UL,
        0x1010101010101010UL, 0x2020202020202020UL, 0x4040404040404040UL, 0x8080808080808080UL
    ];
    private static readonly ulong[] RankMasks = [
        0xFF00000000000000UL, 0x00FF000000000000UL, 0x0000FF0000000000UL, 0x000000FF00000000UL,
        0x00000000FF000000UL, 0x0000000000FF0000UL, 0x000000000000FF00UL, 0x00000000000000FFUL
    ];

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

    private ulong[] _bitboards;
    private ulong _whiteBitboard;
    private ulong _blackBitboard;
    private ulong _whitePinned;
    private ulong _blackPinned;
    private ulong _emptyBitboard;
    private ulong _enPassantMask;

    private int[] _whiteKingPos;
    private int[] _blackKingPos;

    private Dictionary<string, List<string>> _whiteMoves = new();
    private Dictionary<string, List<string>> _blackMoves = new();

    private readonly string _fen;
    private char _turn;
    private string _castle;
    private string _enPassant;
    private int _halfmove; // The halfmove clock specifies a decimal number of half moves with respect to the 50 move draw rule. It is reset to zero after a capture or a pawn move and incremented otherwise.
    private int _fullmove;

    private readonly Dictionary<char, Func<ulong, bool, int, IEnumerable<ulong>>> _moveGenerators;

    public Chess(string fen = Fen)
    {
        _bitboards = new ulong[12];
        _fen = fen;
        _turn = 'w';
        _castle = "";
        _enPassant = "";
        _halfmove = 0;
        _fullmove = 0;
        InitializeBoard();
        _whiteBitboard = _bitboards[Piece['P']] | _bitboards[Piece['N']] | _bitboards[Piece['B']] | _bitboards[Piece['R']] | _bitboards[Piece['Q']] | _bitboards[Piece['K']];
        _blackBitboard = _bitboards[Piece['p']] | _bitboards[Piece['n']] | _bitboards[Piece['b']] | _bitboards[Piece['r']] | _bitboards[Piece['q']] | _bitboards[Piece['k']];
        _emptyBitboard = ~(_whiteBitboard | _blackBitboard);

        _moveGenerators = new Dictionary<char, Func<ulong, bool, int, IEnumerable<ulong>>>
        {
            {'p', GeneratePawnMoves},
            {'n', GenerateKnightMoves},
            {'b', GenerateBishopMoves},
            {'r', GenerateRookMoves},
            {'q', GenerateQueenMoves},
            {'k', GenerateKingMoves}
        };
        _whitePinned = PinnedToKing('w', RookDirections) | PinnedToKing('w', BishopDirections);
        _blackPinned = PinnedToKing('b', RookDirections) | PinnedToKing('b', BishopDirections);
        _whiteKingPos = BitboardToCoord(_bitboards[Piece['K']]);
        _blackKingPos = BitboardToCoord(_bitboards[Piece['k']]);
    }

    public static ulong FileRankToMask(string fileRank)
    {
        int i = 0;
        ulong mask = 0x0UL;
        while (i + 1 < fileRank.Length)
        {
            int fileValue = fileRank[i] - 'a';

            if (int.TryParse(fileRank[i + 1].ToString(), out int rankValue) && 
                fileValue >= 0 && fileValue <= 8 && rankValue >= 0 && rankValue <= 8)
                mask |= 1UL << ((Size - rankValue) * Size + fileValue);
            else
                return 0x0UL;
            i+=2;
        }
        if (i != fileRank.Length) return 0x0UL;
        return mask;
    }

    private int[] BitboardToCoord(ulong bitboard)
    {
        int index = BitOperations.TrailingZeroCount(bitboard);

        return [index % 8, 7 - (index / 8)];
    }

    private string BitboardToSquare(ulong bitboard)
    {
        int[] coord = BitboardToCoord(bitboard);
        char fileChar = (char)('a' + coord[0]);
        return $"{fileChar}{coord[1] + 1}";
    }

    private void InitializeBoard()
    {
        int index = 0, i = 0, j = 0;

        // Bitboards representation
        while (i < _fen.Length && _fen[i] != ' ')
        {
            if (int.TryParse(_fen[i].ToString(), out int num))
                index += num;
            else if (_fen[i] != '/')
                _bitboards[Piece[_fen[i]]] |= 1UL << index++;
            i++;
        }

        // Active Color
        _turn = _fen[++i];

        // Castling Rights
        i += 2;
        while (i < _fen.Length && _fen[i] != ' ')
            _castle += _fen[i++];

        // Possible En Passant Target Mask
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _enPassant = _fen.Substring(j, i - j);
        _enPassantMask = FileRankToMask(_enPassant);

        // Halfmove Clock
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _halfmove = int.Parse(_fen.Substring(j, i - j));

        // Fullmove Number
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _fullmove = int.Parse(_fen.Substring(j, i - j));
    }

    private ulong PinnedToKing(ulong color, Func<ulong, ulong>[] directions)
    {
        char[]  pieceThreat = {(directions == RookDirections ? 'R' : 'B'), 'Q'};
        ulong   pinnedPiece = 0UL;
        ulong   pinnedMask = 0UL;
        ulong   bitboard = (color == 'w' ? _bitboards[Piece['K']] : _bitboards[Piece['k']]);
        ulong   enemyMask = (color == 'w' ? _blackBitboard : _whiteBitboard);
        ulong   friendMask = (color == 'w' ?  _whiteBitboard : _blackBitboard);
        
        if (color == 'w')
            for (int i = 0; i < pieceThreat.Length; i++)
                pieceThreat[i] = Char.ToLower(pieceThreat[i]);

        for (int i = 0; i < 4; i++)
        {
            ulong direction = directions[i](bitboard);

            while ((direction & ~enemyMask) != 0)
            {
                if ((direction & friendMask) != 0)
                {
                    pinnedPiece = direction;
                    direction = directions[i](direction);
                    while ((direction & ~friendMask) != 0)
                    {
                        if ((direction & (_bitboards[Piece[pieceThreat[0]]] | _bitboards[Piece[pieceThreat[1]]])) != 0)
                        {
                            pinnedMask |= pinnedPiece;
                            break;
                        }
                        direction &= _emptyBitboard;
                        direction = directions[i](direction);
                    }
                    break;
                }
                direction &= _emptyBitboard;
                direction = directions[i](direction);
            }
        }

        return pinnedMask;
    }

    public static void PrintBitBoard(ulong bitboard)
    {
        for (int i = 0; i < 64; i++)
        {
            if (i % Size == 0) Console.WriteLine();
            Console.Write($"{(bitboard >> i) & 1UL} ");
        }
        Console.WriteLine();
    }

    private IEnumerable<ulong> GeneratePawnMoves(ulong bitboard, bool isWhite, int constraint)
    {
        List<ulong> moves = new List<ulong>();

        ulong attacks  = 0UL;
        ulong singleStep = 0UL;

        if (constraint == 0 || constraint == 1)
        {
            singleStep = (isWhite ? North(bitboard) & _emptyBitboard : South(bitboard) & _emptyBitboard);
            attacks |= isWhite ? (NorthEast(bitboard) | NorthWest(bitboard)) & (_blackBitboard | _enPassantMask) : (SouthEast(bitboard) & SouthWest(bitboard)) & (_whiteBitboard | _enPassantMask);
        }
        ulong doubleStep = isWhite ? North(singleStep) & _emptyBitboard & RankMasks[3] : South(singleStep) & _emptyBitboard & RankMasks[4];

        if (singleStep != 0) moves.Add(singleStep);
        if (doubleStep != 0) moves.Add(doubleStep);
        if (attacks != 0) moves.Add(attacks);

        return moves;
    }

    private IEnumerable<ulong> GenerateKnightMoves(ulong bitboard, bool isWhite, int constraint)
    {
        List<ulong> moves = new List<ulong>();
        
        if (constraint != 0)
            return moves;
        ulong[] knightMoves = {
            KnightNE(bitboard), KnightNW(bitboard), KnightSE(bitboard), KnightSW(bitboard),
            KnightEN(bitboard), KnightES(bitboard), KnightWN(bitboard), KnightWS(bitboard)
        };

        foreach (var move in knightMoves)
            if ((move & (_emptyBitboard | (isWhite ? _blackBitboard : _whiteBitboard))) != 0)
                moves.Add(move);

        return moves;
    }

    private IEnumerable<ulong> IterDir(ulong bitboard, bool isWhite, int start, int finish)
    {
        List<ulong> moves = new List<ulong>();
        ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);

        for (int i = start; i < finish; i++)
        {
            ulong direction = QueenDirections[i](bitboard);

            while ((direction & _emptyBitboard) != 0)
            {
                direction &= _emptyBitboard;
                moves.Add(direction);
                direction = QueenDirections[i](direction);
            }
            if ((direction & enemyMask) != 0)
                moves.Add(direction);
        }

        return moves;
    }

    private IEnumerable<ulong> GenerateBishopMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint < 4)
                return Enumerable.Empty<ulong>().ToList();
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, isWhite, 4, 8);
    }

    private IEnumerable<ulong> GenerateRookMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
        {
            if (constraint > 4)
                return Enumerable.Empty<ulong>().ToList();
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        }
        return IterDir(bitboard, isWhite, 0, 4);
    }

    private IEnumerable<ulong> GenerateQueenMoves(ulong bitboard, bool isWhite, int constraint)
    {
        if (constraint != 0)
            return IterDir(bitboard, isWhite, constraint - 1, constraint + 1);
        return IterDir(bitboard, isWhite, 0, 8);
    }

    private IEnumerable<ulong> GenerateKingMoves(ulong bitboard, bool isWhite, int constraint)
    {
        List<ulong> moves = new List<ulong>();
        ulong enemyMask = (isWhite ? _blackBitboard : _whiteBitboard);

        ulong[] kingMoves = {
            North(bitboard), South(bitboard), East(bitboard), West(bitboard),
            NorthEast(bitboard), NorthWest(bitboard), SouthEast(bitboard), SouthWest(bitboard)
        };

        foreach (var move in kingMoves)
            if ((move & (_emptyBitboard | enemyMask)) != 0)
                moves.Add(move);
        return moves;
    }

    private int axisConstraint(ulong pieceBitboard, bool isWhite)
    {
        if ((pieceBitboard & (isWhite ? _whitePinned : _blackPinned)) != 0)
        {
            if ((pieceBitboard & (isWhite ? _bitboards[Piece['N']] : _bitboards[Piece['n']])) != 0)
                return 1;
            int[] kingPos = (isWhite ? _whiteKingPos : _blackKingPos);
            Console.WriteLine(kingPos[0]);
            Console.WriteLine(kingPos[1]);

            if ((FileMasks[kingPos[0]] & pieceBitboard) != 0)
                return 1;
            else if ((RankMasks[kingPos[1]] & pieceBitboard) != 0)
                return 3;
            else 
            {
                int[] piecePos = BitboardToCoord(pieceBitboard);

                if ((piecePos[0] - piecePos[1]) == (kingPos[0] - kingPos[1]))
                    return 5;
                if ((piecePos[0] - piecePos[1]) == (kingPos[0] + kingPos[1]))
                    return 7;
            }
        }
        return 0;
    }

    public void GetAllMovesForPiece(char piece)
    {
        bool isWhite = char.IsUpper(piece);
        char pieceKey = char.ToLower(piece);

        // Console.WriteLine($"{piece}|{isWhite}");
        if (_moveGenerators.TryGetValue(pieceKey, out var generateMoves))
        {
            ulong piecesMask = _bitboards[Piece[piece]];
            while (piecesMask != 0)
            {
                ulong pieceBitboard = piecesMask & ~(piecesMask - 1);
                piecesMask &= piecesMask - 1;
                int constraint = axisConstraint(pieceBitboard, isWhite);

                string piecePosition = piece.ToString() + ", " + BitboardToSquare(pieceBitboard);

                IEnumerable<ulong> movesBitboards = generateMoves(pieceBitboard, isWhite, constraint);

                List<string> moves = new List<string>();
                foreach (var moveBitboard in movesBitboards)
                    moves.Add(BitboardToSquare(moveBitboard));
                
                if (isWhite)
                {
                    if (!_whiteMoves.TryAdd(piecePosition, moves))
                        _whiteMoves[piecePosition] = moves;
                }
                else
                {
                    if (!_blackMoves.TryAdd(piecePosition, moves))
                        _blackMoves[piecePosition] = moves;
                }
            }
        }
    }

    public void getFenFromBitboard()
    {
        string fen = "";
        char[] pieceSymbols = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'];
        int index = 0;

        for (int i = 0; i < 64; i++)
        {
            if (i > 0 && i % 8 == 0)
            {
                if (index != 0)
                    fen += index.ToString();
                fen += "/";
                index = 0;
            }
            char piece = '.';
            ulong squareMask = 1UL << i;
            for (int j = 0; j < _bitboards.Length; j++)
            {
                if ((_bitboards[j] & squareMask) != 0)
                {
                    piece = pieceSymbols[j];
                    break;
                }
            }
            if (piece != '.')
            {
                if (index != 0)
                    fen += index.ToString();
                fen += piece.ToString();
                index = 0;
            }
            else
                index++;
        }
        fen += ((_turn == 'w') ? " b " : " w ") + _castle + " " + _enPassant + " " + _halfmove.ToString() + " " + ((_turn == 'w') ? _fullmove : _fullmove + 1).ToString();

        Console.WriteLine();
        Console.WriteLine(_fen);
        Console.WriteLine(fen);
    }

    public void DoRandomMove()
    {
        Random rnd = new Random();
        Dictionary<string, List<string>> moves = new(((_turn == 'w') ? _whiteMoves : _blackMoves));


        List<string> possibleMoves = new(moves.ElementAt(rnd.Next(moves.Count)).Value);
        Console.WriteLine(moves.ElementAt(rnd.Next(moves.Count)).Value.ToString());

        // rnd.Next(moves.Count)
    }
    
    public void PrintAllAccessibleSquares()
    {
        char[] pieceSymbols = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'];
        
        foreach (char piece in pieceSymbols)
            GetAllMovesForPiece(piece);

        Console.WriteLine("\nWhite Pieces:");
        foreach (var white in _whiteMoves)
            Console.WriteLine($"{white.Key}: {string.Join(", ", white.Value)}");
        Console.WriteLine("\nBlack Pieces:");
        foreach (var black in _blackMoves)
            Console.WriteLine($"{black.Key}: {string.Join(", ", black.Value)}");
    }
    
    public void PrintBoard()
    {
        char[] pieceSymbols = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'];

        for (int rank = 0; rank < Size; rank++)
        {
            string row = $"{Size - rank} ";
            for (int file = 0; file < Size; file++)
            {
                ulong squareMask = 1UL << (rank * Size + file);
                char piece = '.';

                for (int i = 0; i < _bitboards.Length; i++)
                {
                    if ((_bitboards[i] & squareMask) != 0)
                    {
                        piece = pieceSymbols[i];
                        break;
                    }
                }
                row += piece + " ";
            }
            Console.WriteLine(row);
        }
        Console.WriteLine("  a b c d e f g h");
    }

    public static void Main(string[] args)
    {
        Chess chess = new Chess("rn2kbnr/1p3ppp/p2p4/2pKpPB1/4P3/2N1Q1N1/PP1R2PP/4qB1R w kq - 11 17");
        chess.PrintBoard();
        chess.PrintAllAccessibleSquares();
        // chess.getFenFromBitboard();
        // chess.DoRandomMove();
    }
}
}

// rn2kbnr/1p3ppp/p2p4/2pKPPq1/N2p4/8/PP2b1PP/R1B2BNR b kq - 0 13
// rn2kbnr/1p3ppp/p2p4/2pK1Pq1/N2pP3/8/PP2b1PP/R1B2BNR w kq - 2 13
// rn2kbnr/pp3ppp/3p4/2p1pP2/2K1P1b1/8/PPq1Q1PP/RNB2BNR w kq - 0 8
// rn2kbnr/pp3ppp/3p4/2p1pP2/2K1P1b1/2N5/PPq1Q1PP/R1B2BNR b kq - 1 8
// rn2kbnr/1p3ppp/p2p4/2p1pP2/2K1P3/2NQ4/PPq1b1PP/R1B2BNR w kq - 2 10
// rn2kbnr/1p3ppp/p2p4/2pKpP2/4P3/2Nq4/PP2b1PP/R1B2BNR w kq - 0 11 check mate