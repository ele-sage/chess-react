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
public partial class Chess
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

    private ulong[] _bitboards;
    private ulong _whiteBitboard;
    private ulong _blackBitboard;
    private ulong _whitePinned;
    private ulong _blackPinned;
    private ulong _whiteCoverage;
    private ulong _blackCoverage;
    private ulong _emptyBitboard;
    private ulong _enPassantMask;

    private int[] _whiteKingPos;
    private int[] _blackKingPos;

    private bool _coverageSet;

    private readonly string _fen;
    private char            _turn;
    private string          _castle;
    private string          _enPassant;
    private int             _halfmove; // The halfmove clock specifies a decimal number of half moves with respect to the 50 move draw rule. It is reset to zero after a capture or a pawn move and incremented otherwise.
    private int             _fullmove;

    private readonly Dictionary<char, Func<ulong, bool, int, ulong>> _moveGenerators;

    public Chess(string fen = Fen)
    {
        _coverageSet = false;
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

        _moveGenerators = new Dictionary<char, Func<ulong, bool, int, ulong>>
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
        _whiteCoverage = GetCoverage(true);
        _blackCoverage = GetCoverage(false);
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

    private ulong GetCoverage(bool isWhite)
    {
        ulong   enemyMaskCoverage = 0UL;
        char[]  pieces = (isWhite ? ['P', 'N', 'B', 'R', 'Q', 'K'] : ['p', 'n', 'b', 'r', 'q', 'k']);
        
        for (int i = 0; i < pieces.Length; i++)
            if (_moveGenerators.TryGetValue(char.ToLower(pieces[i]), out var generateMoves))
                enemyMaskCoverage |= generateMoves(_bitboards[Piece[pieces[i]]], isWhite, 0);

        return enemyMaskCoverage;
    }
}
}