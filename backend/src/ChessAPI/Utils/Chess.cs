using System;
using System.Collections.Generic;
using System.Linq;


//   noWe         nort         noEa
//           +7    +8    +9
//               \  |  /
//   west    -1 <-  0 -> +1    east
//               /  |  \
//           -9    -8    -7
//   soWe         sout         soEa

namespace ChessAPI
{
// Chess.cs
public partial class Chess
{
    private const int Size = 8;
    private const string Fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private static readonly char[] PieceSymbols = ['P', 'N', 'B', 'R', 'Q', 'K', 'p', 'n', 'b', 'r', 'q', 'k'];

    private static readonly ulong[] FileMasks = [
        0x0101010101010101UL, 0x0202020202020202UL, 0x0404040404040404UL, 0x0808080808080808UL,
        0x1010101010101010UL, 0x2020202020202020UL, 0x4040404040404040UL, 0x8080808080808080UL
    ];
    private static readonly ulong[] RankMasks = [
        0xFF00000000000000UL, 0x00FF000000000000UL, 0x0000FF0000000000UL, 0x000000FF00000000UL,
        0x00000000FF000000UL, 0x0000000000FF0000UL, 0x000000000000FF00UL, 0x00000000000000FFUL
    ];

    private Dictionary<char, ulong> _bitboards;
    private ulong[]         _fullBitboard;
    private ulong[]         _pinnedToKing;
    private ulong[]         _pieceCoverage;
    private int[,]          _kingPos;
    private ulong           _emptyBitboard;
    private ulong           _enPassantMask;

    private bool            _coverageSet;
    private readonly string _fen;
    private char            _turn;
    private string          _castle;
    private string          _enPassant;
    private int             _halfmove; // The halfmove clock specifies a decimal number of half moves with respect to the 50 move draw rule. It is reset to zero after a capture or a pawn move and incremented otherwise.
    private int             _fullmove;

    private readonly Dictionary<char, Func<ulong, bool, int, ulong>> _moveGenerators;

    public Chess(string fen = Fen)
    {
        _bitboards = new Dictionary<char, ulong>
        {
            {'P', 0UL}, {'N', 0UL}, {'B', 0UL}, {'R', 0UL}, {'Q', 0UL}, {'K', 0UL},
            {'p', 0UL}, {'n', 0UL}, {'b', 0UL}, {'r', 0UL}, {'q', 0UL}, {'k', 0UL}
        };
        _fullBitboard = new ulong[2];
        _pieceCoverage = new ulong[2];
        _pinnedToKing = new ulong[2];
        _kingPos = new int [2,2];
        _coverageSet = false;
        _fen = fen;
        _turn = 'w';
        _castle = "";
        _enPassant = "";
        _halfmove = 0;
        _fullmove = 0;
        InitializeBoard();
        _fullBitboard[0] = _bitboards['P'] | _bitboards['N'] | _bitboards['B'] | _bitboards['R'] | _bitboards['Q'] | _bitboards['K'];
        _fullBitboard[1] = _bitboards['p'] | _bitboards['n'] | _bitboards['b'] | _bitboards['r'] | _bitboards['q'] | _bitboards['k'];
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);

        _moveGenerators = new Dictionary<char, Func<ulong, bool, int, ulong>>
        {
            {'p', GeneratePawnMoves},
            {'n', GenerateKnightMoves},
            {'b', GenerateBishopMoves},
            {'r', GenerateRookMoves},
            {'q', GenerateQueenMoves},
            {'k', GenerateKingMoves}
        };
        _pinnedToKing[0] = PinnedToKing('w', RookDirections) | PinnedToKing('w', BishopDirections);
        _pinnedToKing[1] = PinnedToKing('b', RookDirections) | PinnedToKing('b', BishopDirections);
        setKingPos();
        _pieceCoverage[0] = GetCoverage(true);
        _pieceCoverage[1] = GetCoverage(false);
        _coverageSet = true;
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
                _bitboards[_fen[i]] |= 1UL << index++;
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

    private ulong PinnedToKing(char c, Func<ulong, ulong>[] directions)
    {
        int     color = c == 'w' ?  0 : 1;
        char[]  pieceThreat = [(directions == RookDirections ? 'R' : 'B'), 'Q'];
        ulong   pinnedPiece = 0UL;
        ulong   pinnedMask = 0UL;
        
        if (color == 'w')
            for (int i = 0; i < pieceThreat.Length; i++)
                pieceThreat[i] = Char.ToLower(pieceThreat[i]);

        for (int i = 0; i < 4; i++)
        {
            ulong direction = directions[i](color == 'w' ? _bitboards['K'] : _bitboards['k']);

            while ((direction & ~_fullBitboard[color ^ 1]) != 0)
            {
                if ((direction & _fullBitboard[color]) != 0)
                {
                    pinnedPiece = direction;
                    direction = directions[i](direction);
                    while ((direction & ~_fullBitboard[color]) != 0)
                    {
                        if ((direction & (_bitboards[pieceThreat[0]] | _bitboards[pieceThreat[1]])) != 0)
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

    private ulong GetCoverage(bool isWhite)
    {
        ulong   enemyMaskCoverage = 0UL;
        char[]  pieces = isWhite ? ['P', 'N', 'B', 'R', 'Q', 'K'] : ['p', 'n', 'b', 'r', 'q', 'k'];
        
        for (int i = 0; i < pieces.Length; i++)
            if (_moveGenerators.TryGetValue(char.ToLower(pieces[i]), out var generateMoves))
                enemyMaskCoverage |= generateMoves(_bitboards[pieces[i]], isWhite, 0);

        return enemyMaskCoverage;
    }

    private void setKingPos()
    {
        int[] whiteKingPos = BitboardToCoord(_bitboards['K']);
        int[] blackKingPos = BitboardToCoord(_bitboards['k']);
        _kingPos[0,0] = whiteKingPos[0];
        _kingPos[0,1] = whiteKingPos[1];
        _kingPos[1,0] = blackKingPos[0];
        _kingPos[1,1] = blackKingPos[1];
    }
}
}