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
    private static readonly char[,] Pieces = {{'P', 'N', 'B', 'R', 'Q', 'K'}, {'p', 'n', 'b', 'r', 'q', 'k'}};
    private static readonly ulong[] FileMasks = [
        0x0101010101010101UL, 0x0202020202020202UL, 0x0404040404040404UL, 0x0808080808080808UL,
        0x1010101010101010UL, 0x2020202020202020UL, 0x4040404040404040UL, 0x8080808080808080UL
    ];
    private static readonly ulong[] RankMasks = [
        0xFF00000000000000UL, 0x00FF000000000000UL, 0x0000FF0000000000UL, 0x000000FF00000000UL,
        0x00000000FF000000UL, 0x0000000000FF0000UL, 0x000000000000FF00UL, 0x00000000000000FFUL
    ];

    private void SetFullBitboard(int color)
    {
        _fullBitboard[color] = _bitboards[Pieces[color,0]] | _bitboards[Pieces[color,1]] | _bitboards[Pieces[color,2]] | _bitboards[Pieces[color,3]] | _bitboards[Pieces[color,4]] | _bitboards[Pieces[color,5]];
    }

    private Dictionary<char, ulong> _bitboards;
    private ulong[]         _fullBitboard = [0UL, 0UL];
    private ulong[]         _pinnedToKing = [0UL, 0UL];
    private ulong[]         _pieceCoverage = [0UL, 0UL];
    private ulong[]         _pieceAttack = [0UL, 0UL];
    private Dictionary<ulong, ulong>[] _checkBy = [[], []];
    private int[,]          _kingPos = {{0,0}, {0,0}};
    private ulong           _emptyBitboard;
    private ulong           _enPassantMask;
    private int             _possibleMove = 0;
    private string          _fen;
    private char            _turn;
    private bool[,]         _castle = {{false,false}, {false,false}};
    private int             _halfmove;
    private int             _fullmove;
    private readonly Dictionary<char, Func<ulong, int, bool, int, ulong>> _moveGenerators;

    public Chess(string fen = Fen)
    {
        _bitboards = new Dictionary<char, ulong>
        {
            {'P', 0UL}, {'N', 0UL}, {'B', 0UL}, {'R', 0UL}, {'Q', 0UL}, {'K', 0UL},
            {'p', 0UL}, {'n', 0UL}, {'b', 0UL}, {'r', 0UL}, {'q', 0UL}, {'k', 0UL}
        };
        _moveGenerators = new Dictionary<char, Func<ulong, int, bool, int, ulong>>
        {
            {'p', GeneratePawnMoves},
            {'n', GenerateKnightMoves},
            {'b', GenerateBishopMoves},
            {'r', GenerateRookMoves},
            {'q', GenerateQueenMoves},
            {'k', GenerateKingMoves}
        };
        _fen = fen;
        _turn = 'w';
        _halfmove = 0;
        _fullmove = 0;
        InitializeBoard();
        _fullBitboard[0] = _bitboards['P'] | _bitboards['N'] | _bitboards['B'] | _bitboards['R'] | _bitboards['Q'] | _bitboards['K'];
        _fullBitboard[1] = _bitboards['p'] | _bitboards['n'] | _bitboards['b'] | _bitboards['r'] | _bitboards['q'] | _bitboards['k'];
        _emptyBitboard = ~(_fullBitboard[0] | _fullBitboard[1]);
        SetKingPos();
        InitCoverage();
        seenPositions = [];

    }

    // public Chess(Chess other)
    // {
    //     _bitboards = new Dictionary<char, ulong>(other._bitboards);
    //     _fullBitboard = (ulong[])other._fullBitboard.Clone();
    //     _pinnedToKing = (ulong[])other._pinnedToKing.Clone();
    //     _pieceCoverage = (ulong[])other._pieceCoverage.Clone();
    //     _pieceAttack = (ulong[])other._pieceAttack.Clone();
    //     _checkBy = new Dictionary<ulong, ulong>[2];
    //     for (int i = 0; i < 2; i++)
    //     {
    //         _checkBy[i] = new Dictionary<ulong, ulong>(other._checkBy[i]);
    //     }
    //     _kingPos = (int[,])other._kingPos.Clone();
    //     _emptyBitboard = other._emptyBitboard;
    //     _enPassantMask = other._enPassantMask;
    //     _possibleMove = other._possibleMove;
    //     _fen = other._fen;
    //     _turn = other._turn;
    //     _castle = (bool[,])other._castle.Clone();
    //     _halfmove = other._halfmove;
    //     _fullmove = other._fullmove;
    //     _moveGenerators = new Dictionary<char, Func<ulong, int, bool, int, ulong>>(other._moveGenerators);
    // }

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
        if (_fen[i] == '-')
            i++;
        else
        {
            if (_fen[i] == 'K')
            {
                i++;
                _castle [0,0] = true;
            }
            if (_fen[i] == 'Q')
            {
                i++;
                _castle [0,1] = true;
            }
            if (_fen[i] == 'k')
            {
                i++;
                _castle [1,0] = true;
            }
            if (_fen[i] == 'q')
            {
                i++;
                _castle [1,1] = true;
            }
        }

        // Possible En Passant Target Mask
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _enPassantMask = FileRankToMask(_fen[j..i]);

        // Halfmove Clock
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _halfmove = int.Parse(_fen.Substring(j, i - j));

        // Fullmove Number
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _fullmove = int.Parse(_fen.Substring(j, i - j));
    }

    private ulong PinnedToKing(int color, Func<ulong, ulong>[] directions)
    {
        char[]  pieceThreat = [(directions == RookDirections ? 'R' : 'B'), 'Q'];
        ulong   pinnedPiece = 0UL;
        ulong   pinnedMask = 0UL;
        
        if (color == 0)
            for (int i = 0; i < pieceThreat.Length; i++)
                pieceThreat[i] = Char.ToLower(pieceThreat[i]);

        for (int i = 0; i < 4; i++)
        {
            ulong direction = directions[i](color == 0 ? _bitboards['K'] : _bitboards['k']);

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

    private void SetKingCoverage()
    {
        ulong whiteKingAdjancent = North(_bitboards[PieceSymbols[5]]) | South(_bitboards[PieceSymbols[5]]) | East(_bitboards[PieceSymbols[5]]) | West(_bitboards[PieceSymbols[5]]) | NorthEast(_bitboards[PieceSymbols[5]]) | NorthWest(_bitboards[PieceSymbols[5]]) | SouthEast(_bitboards[PieceSymbols[5]]) | SouthWest(_bitboards[PieceSymbols[5]]);
        ulong blackKingAdjancent = North(_bitboards[PieceSymbols[11]]) | South(_bitboards[PieceSymbols[11]]) | East(_bitboards[PieceSymbols[11]]) | West(_bitboards[PieceSymbols[11]]) | NorthEast(_bitboards[PieceSymbols[11]]) | NorthWest(_bitboards[PieceSymbols[11]]) | SouthEast(_bitboards[PieceSymbols[11]]) | SouthWest(_bitboards[PieceSymbols[11]]);

        _pieceCoverage[0] |= whiteKingAdjancent & ~_fullBitboard[1];
        _pieceCoverage[1] |= blackKingAdjancent & ~_fullBitboard[0];
        _pieceAttack[0] |= whiteKingAdjancent & _fullBitboard[1] & ~_pieceCoverage[1];
        _pieceAttack[1] |= blackKingAdjancent & _fullBitboard[0] & ~_pieceCoverage[0];
    }

    private void SetCoverage(int color, bool kingCoverage = true)
    {
        char[]  pieces = color == 0 ? ['P', 'N', 'B', 'R', 'Q', 'K'] : ['p', 'n', 'b', 'r', 'q', 'k'];
        _pieceCoverage[color] = 0UL;
        
        for (int i = 0; i < pieces.Length - 1; i++)
            _pieceCoverage[color] |= _moveGenerators[char.ToLower(pieces[i])](_bitboards[pieces[i]], color, true, 0);
        if (kingCoverage)
            SetKingCoverage();
    }

    private void InitCoverage()
    {
        _pieceCoverage[0] = 0UL;
        _pieceCoverage[1] = 0UL;

        _pinnedToKing[0] = PinnedToKing(0, RookDirections) | PinnedToKing(0, BishopDirections);
        _pinnedToKing[1] = PinnedToKing(0, RookDirections) | PinnedToKing(0, BishopDirections);
        SetCoverage(0, false);
        SetCoverage(1, false);
        SetKingCoverage();
        IsCheck(0);
        IsCheck(1);
    }

    private void SetKingPos()
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