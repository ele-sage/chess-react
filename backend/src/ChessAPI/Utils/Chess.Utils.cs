using System.Numerics;

namespace ChessAPI
{
// Chess.Utils.cs
public partial class Chess
{

    private HashSet<(ulong[], ulong[])> seenPositions;

    private bool IsUniqueBoardPosition()
    {
        ulong[] bitboardWhite = [_bitboards['P'], _bitboards['N'], _bitboards['B'], _bitboards['R'], _bitboards['Q'], _bitboards['K']];
        ulong[] bitboardBlack = [_bitboards['p'], _bitboards['n'], _bitboards['b'], _bitboards['r'], _bitboards['q'], _bitboards['k']];
        var currentPosition = (bitboardWhite, bitboardBlack);

        if (seenPositions.Contains(currentPosition) == false)
        {
            seenPositions.Add(currentPosition);
            return true;
        }
        return false;
    }

    private static ulong FileRankToMask(string fileRank)
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

    private static string BitboardToSquare(ulong bitboard)
    {
        int index = BitOperations.TrailingZeroCount(bitboard);
        int rank = Size - (index / Size);
        char file = (char)('a' + (index % Size));
        return $"{file}{rank}";
    }

    private static List<string> BitboardToSquares(ulong bitboard)
    {
        List<string> squares = [];
        while (bitboard != 0)
        {
            ulong bit = bitboard & ~(bitboard - 1);
            squares.Add(BitboardToSquare(bit));
            bitboard &= bitboard - 1;
        }
        return squares;
    }

    public static ulong FileRankToBitboard(string fileRank)
    {
        int file = fileRank[0] - 'a';
        int rank = fileRank[1] - '1';

        int bitPosition = rank * Size + file;
        return 1UL << bitPosition;
    }

    public static void PrintBitBoard(ulong bitboard)
    {
        for (int i = 0; i < 64; i++)
        {
            if (i % Size == 0) Console.Write($"\n{Size - i / Size} ");
            Console.Write($"{(bitboard >> i) & 1UL} ");
        }
        Console.WriteLine("\n  a b c d e f g h\n");
    }

    public string GetFenFromBitboard()
    {
        string fen = "";
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
            for (int j = 0; j < PieceSymbols.Length; j++)
            {
                if ((_bitboards[PieceSymbols[j]] & squareMask) != 0)
                {
                    piece = PieceSymbols[j];
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
        string castle = "";
        if (_castle[0,0]) castle += "K";
        if (_castle[0,1]) castle += "Q";
        if (_castle[1,0]) castle += "k";
        if (_castle[1,1]) castle += "q";
        if (castle.Length == 0) castle = "-";
        string enPassant = _enPassantMask == 0 ? "-" : BitboardToSquare(_enPassantMask);
        _fen = fen + " " + _turn + " " + castle + " " + enPassant + " " + _halfmove.ToString() + " " + ((_turn == 'w') ? _fullmove : _fullmove++).ToString();
        return _fen;
    }

    private ulong GetRandomMove(ulong moves)
    {
        List<ulong> indexes = [];
        Random rnd = new();
        
        while (moves != 0)
        {
            ulong bit = moves & ~(moves - 1);
            indexes.Add(bit);
            moves &= moves - 1;
        }
        return indexes[rnd.Next(indexes.Count)];
    }

    public void DoRandomMove(int color)
    {
        Random rnd = new();
        int random = rnd.Next(_movesBitboard[color].Count);
        int index = 1;
            
        KeyValuePair<ulong, ulong> keyValue = _movesBitboard[color].ElementAt(random);

        while (keyValue.Value == 0 && index < _movesBitboard[color].Count)
        {
            keyValue = _movesBitboard[color].ElementAt((random + index) % _movesBitboard[color].Count);
            index++;
        }
        if (keyValue.Value == 0)
            return;
        // SetNewBitBoardState(keyValue.Key, GetRandomMove(keyValue.Value), color);
    }

    public Dictionary<string, List<string>> DoTurn()
    {
        int color = _turn == 'w' ? 0 : 1;
        char[]  pieces = color == 0 ? ['P', 'N', 'B', 'R', 'Q', 'K'] : ['p', 'n', 'b', 'r', 'q', 'k'];
        char[]  enemyPieces = color == 0 ? ['p', 'n', 'b', 'r', 'q', 'k'] : ['P', 'N', 'B', 'R', 'Q', 'K'];
        Dictionary<string, List<string>> legalMoves = [];

        Console.WriteLine("before your moves");
        foreach (char piece in pieces)
            GetAllMovesForPiece(piece);
        DoRandomMove(color);
        
        SetCoverage(color);
        foreach (char piece in enemyPieces)
            GetAllMovesForPiece(piece);

        foreach (var moves in _movesBitboard[color ^ 1])
        {
            string piecePosition = BitboardToSquare(moves.Key);
            List<string> moveSet = BitboardToSquares(moves.Value);
            if(!legalMoves.TryAdd(piecePosition, moveSet))
                legalMoves[piecePosition] = moveSet;
        }
        return legalMoves;
    }

    public void PrintAllAccessibleSquares()
    {
        int i = 0;
        foreach (var color in _checkBy)
        {
            if(IsCheck(i))
            {
                string c = i == 0 ? "White" : "Black";
                Console.Write($"\n{c} king is check by ");
                foreach (var moves in _checkBy[i])
                {
                    string pos = BitboardToSquare(moves.Key);
                    ulong piece = moves.Value;
                    Console.WriteLine($"{pos}: [{string.Join(", ", piece)}]");
                }
            }
            i++;
        }
        foreach (char piece in PieceSymbols)
            GetAllMovesForPiece(piece);

        i = 0;
        foreach (var color in _movesBitboard)
        {
            foreach (var moves in color)
            {
                string piecePosition = BitboardToSquare(moves.Key);
                List<string> moveSet = BitboardToSquares(moves.Value);
                if(!_moves[i].TryAdd(piecePosition, moveSet))
                    _moves[i][piecePosition] = moveSet;
            }
            i++;
        }
        foreach (var color in _moves)
        {
            foreach (var moves in color)
            {
                string key = moves.Key;
                List<string> value = moves.Value;
                Console.WriteLine($"{key}: [{string.Join(", ", value)}]");
            }
            Console.WriteLine();
        }
        Console.Write("\nWhite attack:");
        PrintBitBoard(_pieceAttack[0]);
        Console.Write("\nBlack attack:");
        PrintBitBoard(_pieceAttack[1]);
        Console.Write("\nWhite Coverage:");
        PrintBitBoard(_pieceCoverage[0]);
        Console.Write("\nBlack Coverage:");
        PrintBitBoard(_pieceCoverage[1]);
    }
    
    public void PrintBoard()
    {
        for (int rank = 0; rank < Size; rank++)
        {
            string row = $"{Size - rank} ";
            for (int file = 0; file < Size; file++)
            {
                ulong squareMask = 1UL << (rank * Size + file);
                char piece = '.';

                for (int i = 0; i < PieceSymbols.Length; i++)
                {
                    if ((_bitboards[PieceSymbols[i]] & squareMask) != 0)
                    {
                        piece = PieceSymbols[i];
                        break;
                    }
                }
                row += piece + " ";
            }
            Console.WriteLine(row);
        }
        Console.WriteLine("  a b c d e f g h");
    }

    public void GetAllMovesForPiece(char piece)
    {
        int color = char.IsUpper(piece) ? 0 : 1;
        char pieceKey = char.ToLower(piece);

        if (_moveGenerators.TryGetValue(pieceKey, out var generateMoves))
        {
            ulong piecesMask = _bitboards[piece];
            while (piecesMask != 0)
            {
                ulong pieceBitboard = piecesMask & ~(piecesMask - 1);
                piecesMask &= piecesMask - 1;
                
                if (_checkBy[color].Count == 2 && pieceKey != 'k')
                    if(!_movesBitboard[color].TryAdd(pieceBitboard, 0UL))
                        _movesBitboard[color][pieceBitboard] = 0UL;
                
                int constraint = AxisConstraint(pieceBitboard, color);
                ulong movesBitboards = generateMoves(pieceBitboard, color, false, constraint);

                if (_checkBy[color].Count == 1 && pieceKey != 'k')
                    movesBitboards &= _checkBy[color].ElementAt(0).Value;

                if(!_movesBitboard[color].TryAdd(pieceBitboard, movesBitboards))
                    _movesBitboard[color][pieceBitboard] = movesBitboards;
            }
        }
    }

    public void SetAllMoves(int color)
    {
        for (int i = 0; i < 6; i++)
        {
            char pieceKey = char.ToLower(Pieces[color ^ 1,i]);

            if (_moveGenerators.TryGetValue(pieceKey, out var generateMoves))
            {
                ulong piecesMask = _bitboards[Pieces[color ^ 1,i]];
                while (piecesMask != 0)
                {
                    ulong pieceBitboard = piecesMask & ~(piecesMask - 1);
                    piecesMask &= piecesMask - 1;
                    
                    if (_checkBy[color].Count == 2 && pieceKey != 'k')
                        if(!_movesBitboard[color].TryAdd(pieceBitboard, 0UL))
                            _movesBitboard[color][pieceBitboard] = 0UL;
                    
                    int constraint = AxisConstraint(pieceBitboard, color);
                    ulong movesBitboards = generateMoves(pieceBitboard, color, false, constraint);

                    if (_checkBy[color].Count == 1 && pieceKey != 'k')
                        movesBitboards &= _checkBy[color].ElementAt(0).Value;

                    if(!_movesBitboard[color].TryAdd(pieceBitboard, movesBitboards))
                        _movesBitboard[color][pieceBitboard] = movesBitboards;
                }
            }
        }
    }
}
}