namespace ChessAPI
{
// Chess.cs
public partial class Chess
{
    private Dictionary<char, ulong> _bitboards = new()
    {
        {'P', 0UL}, {'N', 0UL}, {'B', 0UL}, {'R', 0UL}, {'Q', 0UL}, {'K', 0UL},
        {'p', 0UL}, {'n', 0UL}, {'b', 0UL}, {'r', 0UL}, {'q', 0UL}, {'k', 0UL}
    };

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
        while (i < _fen.Length && _fen[i] != ' ')
            i++;

        // Possible En Passant Target Mask
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _enPassantMask = FileRankToMask(_fen[j..i]);

        // Halfmove Clock
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _halfmove = int.Parse(_fen[j..i]);

        // Fullmove Number
        j = ++i;
        while (++i < _fen.Length && _fen[i] != ' ');
        _fullmove = int.Parse(_fen[j..i]);
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
        _pieceCoverage[color] = 0UL;
        _pieceAttack[color] = 0UL;

        for (int i = 0; i < 5; i++)
            _pieceCoverage[color] |= _moveGenerators[char.ToLower(Pieces[color, i])](_bitboards[Pieces[color, i]], color, true, 0);
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


    // public Move GetBestMoveParallel(int maxDepth)
    // {
    //     List<Move> allMoves = GetAllPossibleMoves(_turn);
    //     if (allMoves.Count == 0)
    //     {
    //         int color = _turn == 'w' ? 0 : 1;
    //         if (_checkBy[color].Count == 0)
    //             return new('+', 0UL, 0UL); // Stalemate
    //         else
    //             return new('-', 0UL, 0UL); // Checkmate
    //     }
    //     int alpha = int.MinValue;
    //     int beta = int.MaxValue;
    //     Move bestMove = new('-', 0UL, 0UL);
    //     int bestScore = _turn == 'w' ? int.MinValue : int.MaxValue;
    //     int[] scores = new int[allMoves.Count];
    //     Chess[] boards = new Chess[allMoves.Count];

    //     for (int i = 0; i < allMoves.Count; i++)
    //         boards[i] = new Chess(this);
    //     Parallel.For(0, allMoves.Count, i =>
    //     {
    //         boards[i].ApplyMove(allMoves[i]);
    //         scores[i] = boards[i].NegaMax(maxDepth - 1, alpha, beta);
    //     });
    //     int totalPossibleMoves = _possibleMove;
    //     for (int i = 0; i < allMoves.Count; i++)
    //     {
    //         totalPossibleMoves += boards[i]._possibleMove;
    //         if (_turn == 'w' && scores[i] > bestScore)
    //         {
    //             bestScore = scores[i];
    //             bestMove = allMoves[i];
    //             alpha = Math.Max(alpha, bestScore);
    //         }
    //         else if (_turn == 'b' && scores[i] < bestScore)
    //         {
    //             bestScore = scores[i];
    //             bestMove = allMoves[i];
    //             beta = Math.Min(beta, bestScore);
    //         }
    //     }
    //     Console.WriteLine($"Total possible moves: {totalPossibleMoves}");
    //     return bestMove;
    // }

