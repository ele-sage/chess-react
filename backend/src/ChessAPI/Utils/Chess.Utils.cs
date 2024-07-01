using System.Numerics;
using System.Diagnostics;

namespace ChessAPI
{
// Chess.Utils.cs
public partial class Chess
{
    private void PrintAllFieldsToFile(string path)
    {
        using (StreamWriter sw = new(path))
        {
            sw.WriteLine("Board:");
            sw.WriteLine(GetBoard());
            foreach (var bitboard in _bitboards)
                sw.WriteLine($"{bitboard.Key}: {GetBitBoard(bitboard.Value)}");
            sw.WriteLine("\nFull Bitboards:");
            sw.WriteLine($"White: {GetBitBoard(_fullBitboard[0])}\nBlack: {GetBitBoard(_fullBitboard[1])}");
            sw.WriteLine("\nPinned to King:");
            sw.WriteLine($"White: {GetBitBoard(_pinnedToKing[0])}\nBlack: {GetBitBoard(_pinnedToKing[1])}");
            sw.WriteLine("\nPiece Coverage:");
            sw.WriteLine($"White: {GetBitBoard(_pieceCoverage[0])}\nBlack: {GetBitBoard(_pieceCoverage[1])}");
            sw.WriteLine("\nPiece Attack:");
            sw.WriteLine($"White: {GetBitBoard(_pieceAttack[0])}\nBlack: {GetBitBoard(_pieceAttack[1])}");
            sw.WriteLine("\nCheck By:");
            foreach (var check in _checkBy)
            {
                foreach (var checkBy in check)
                    sw.WriteLine($"{checkBy.Key}: {checkBy.Value}");
            }
            sw.WriteLine("\nKing Position:");
            sw.WriteLine($"White: {_kingPos[0,0]}, {_kingPos[0,1]}\nBlack: {_kingPos[1,0]}, {_kingPos[1,1]}");
            sw.WriteLine("\nEmpty Bitboard:");
            sw.WriteLine(_emptyBitboard);
            sw.WriteLine("\nEn Passant Mask:");
            sw.WriteLine(_enPassantMask);
            sw.WriteLine("\nCastle:");
            sw.WriteLine($"White: {_castle[0,0]}, {_castle[0,1]}\nBlack: {_castle[1,0]}, {_castle[1,1]}");
        }
    }

    private void SetFullBitboard(int color)
    {
        _fullBitboard[color] = _bitboards[Pieces[color,0]] | _bitboards[Pieces[color,1]] | _bitboards[Pieces[color,2]] | _bitboards[Pieces[color,3]] | _bitboards[Pieces[color,4]] | _bitboards[Pieces[color,5]];
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
        if (index != 0)
            fen += index.ToString();
        string castle = "";
        if (_castle[0,0]) castle += "K";
        if (_castle[0,1]) castle += "Q";
        if (_castle[1,0]) castle += "k";
        if (_castle[1,1]) castle += "q";
        if (castle.Length == 0) castle = "-";
        string enPassant = _enPassantMask == 0 ? "-" : BitboardToSquare(_enPassantMask);
        _fen = fen + " " + _turn + " " + castle + " " + enPassant + " " + _halfmove.ToString() + " " + _fullmove.ToString();
        // Console.WriteLine(_fen);
        return _fen;
    }

    public void PrintBoard(string something = "")
    {
        string board = something;
        if (board == "")
            Console.WriteLine($"------------------ {_turn}");
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
            if (board != "")
                board += row + "\n";
            else
                Console.WriteLine(row);
        }
        board += "  a b c d e f g h\n";
        Console.WriteLine(board);
    }

    public string GetBoard()
    {
        string board = "";
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
            board += row + "\n";
        }
        board += "  a b c d e f g h\n";
        return board;
    }

    public static string GetBitBoard(ulong bitboard)
    {
        string board = "\n";
        for (int rank = 0; rank < Size; rank++)
        {
            string row = $"{Size - rank} ";
            for (int file = 0; file < Size; file++)
            {
                ulong squareMask = 1UL << (rank * Size + file);
                char piece = (bitboard & squareMask) != 0 ? '1' : '0';
                row += piece + " ";
            }
            board += row + "\n";
        }
        board += "  a b c d e f g h\n";
        return board;
    }

    public GameResponse GetLegalMoves()
    {
        List<string> legalMoves = [];
        bool checkmate = false;
        bool stalemate = false;
        List<string> checkBy = [];
        Move[] moves = GetAllPossibleMoves(_turn);
        if (moves.Length == 0 || moves[0].Piece == '-' || moves[0].Piece == '+')
        {
            if (IsCheck(_turn == 'w' ? 0 : 1))
                checkmate = true;
            else
                stalemate = true;
        }
        else
        {
            foreach (var check in _checkBy[_turn == 'w' ? 0 : 1])
            {
                // Console.WriteLine(BitboardToSquare(check.Key));
                checkBy.Add(BitboardToSquare(check.Key));
            }
            foreach (Move move in moves)
            {
                string moveSerialized = $"{BitboardToSquare(move.From)} {BitboardToSquare(move.To)} {move.Piece}";
                // Console.WriteLine(moveSerialized);
                legalMoves.Add(moveSerialized);
            }
        }

        return new GameResponse(legalMoves, GetFenFromBitboard(), checkmate, stalemate, checkBy);
    }

    public GameResponse GetLegalMovesAfterBot()
    {
        int color = _turn == 'w' ? 0 : 1;

        GameResponse response = GetLegalMoves();
        if (response.Checkmate || response.Stalemate)
            return response;
        
        // execution time
        Stopwatch sw = new();
        sw.Start();
        Move bestMove = IterativeDeepening();

        sw.Stop();
        Console.WriteLine($"Execution Time: {sw.ElapsedMilliseconds}ms");
        if (bestMove.Piece == 'P' || bestMove.Piece == 'p' || (bestMove.To & (_fullBitboard[color ^ 1] | _enPassantMask)) != 0)
            _halfmove = 0;
        else
            _halfmove++;
        if (_turn == 'b')
            _fullmove++;
        Console.WriteLine(bestMove);
        ApplyMove(bestMove);
        return GetLegalMoves();
    }
}
public record GameResponse(
    List<string> LegalMoves, 
    string Fen,
    bool Checkmate,
    bool Stalemate,
    List<string> CheckBy);
}