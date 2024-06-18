using System.Text.RegularExpressions;

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
    private int             _timeLimitMillis = 1000;
    private int             _maxDepth = 5;
    private int             _currentDepth = 3;
    private Move            _currentBestMove = new('-', 0UL, 0UL);
    private int             _currentBestScore = 0;
    private readonly Dictionary<char, Func<ulong, int, bool, int, ulong[]>> _moveGenerators;
    private Move            _bestMove = new('-', 0UL, 0UL);
    private int             _bestScore = 0;
    public Chess(string fen = Fen)
    {
        IsValidFen(fen);
        _moveGenerators = new Dictionary<char, Func<ulong, int, bool, int, ulong[]>>
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
            _pieceCoverage[color] |= _moveGenerators[char.ToLower(Pieces[color, i])](_bitboards[Pieces[color, i]], color, true, 0)[0];

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

    private static void IsValidFen(string fen)
    {
        const string FEN_SYNTAX = "\nFEN Syntax ::=  `<Piece Placement> <Side to move> <Castling Rights> <En passant target square> <Halfmove clock> <Fullmove counter>`";
        const string PIECE_PLACEMENT = "\n<Piece Placement> ::= <rank8>'/'<rank7>'/'<rank6>'/'<rank5>'/'<rank4>'/'<rank3>'/'<rank2>'/'<rank1>\n<ranki>       ::= [<digit17>]<piece> {[<digit17>]<piece>} [<digit17>] | '8' +\n<piece>       ::= <white Piece> | <black Piece>\n<digit17>     ::= '1' | '2' | '3' | '4' | '5' | '6' | '7'\n<white Piece> ::= 'P' | 'N' | 'B' | 'R' | 'Q' | 'K'\n<black Piece> ::= 'p' | 'n' | 'b' | 'r' | 'q' | 'k'";
        const string SIDE_TO_MOVE = "\n<Side to move> ::= {'w' | 'b'}";
        const string CASTLING_RIGHTS = "\n<Castling Rights> ::= '-' | ['K'] ['Q'] ['k'] ['q'] (1..4)";
        const string EN_PASSANT = "\n<En passant target square> ::= '-' | <epsquare>\n<epsquare>   ::= <fileLetter> <eprank>\n<fileLetter> ::= 'a' | 'b' | 'c' | 'd' | 'e' | 'f' | 'g' | 'h'\n<eprank>     ::= '3' | '6'";
        const string HALFMOVE_CLOCK = "\n<Halfmove clock> ::= <digit> {<digit>}\n<digit>         ::= '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'";
        const string FULLMOVE_COUNTER = "\n<Fullmove counter> ::= <digit> {<digit>}\n<digit>           ::= '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9'";
        const string FEN_FORMAT = FEN_SYNTAX + PIECE_PLACEMENT + SIDE_TO_MOVE + CASTLING_RIGHTS + EN_PASSANT + HALFMOVE_CLOCK + FULLMOVE_COUNTER;
        const string FEN_EXAMPLE = "\nExample: 4k2r/r3bppp/p1np4/1p1NpP2/2p1P3/6N1/PPKR2PP/4QB1R w k - 4 23\n         rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1.";
        
        string fenPattern = @"\s*^(((?:[rnbqkpRNBQKP1-8]+\/){7})[rnbqkpRNBQKP1-8]+)\s([b|w])\s([K|Q|k|q]{1,4}|-)\s([a-h][3|6]|-)\s(\d+)\s(\d+)\s*$";

        // validate fen with regex
        if (!Regex.IsMatch(fen, fenPattern))
        {
            throw new ArgumentException("FEN does not match the standard format." + FEN_FORMAT + FEN_EXAMPLE);
        }

        int i = 0, lineLength = 0;
        int[] kings = [0, 0];
        while (i < fen.Length && fen[i] != ' ')
        {
            if (fen[i] == 'k') kings[0]++;
            else if (fen[i] == 'K') kings[1]++;
            if (fen[i] == '/')
            {
                if (lineLength != 8)
                {
                    throw new ArgumentException("Each rank must have exactly 8 squares." + "\nCurrent rank: " + fen[(i - lineLength)..i] + "\nSize: " + lineLength);
                }
                lineLength = 0;
            }
            else if (char.IsDigit(fen[i]))
            {
                lineLength += int.Parse(fen[i].ToString());
            }
            else if (char.IsLetter(fen[i]))
            {
                lineLength++;
            }
            else
            {
                throw new ArgumentException("Invalid character in FEN string." + "\nCharacter: " + fen[i]);
            }
            i++;
        }
        if (kings[0] != 1 || kings[1] != 1)
        {
            throw new ArgumentException("Each player must have exactly one king." + "\nWhite kings: " + kings[1] + "\nBlack kings: " + kings[0]);
        }
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

