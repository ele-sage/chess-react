using System.Numerics;

namespace ChessAPI
{
// Chess.Utils.cs
public partial class Chess
{
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

    private static string BitboardToSquare(ulong bitboard)
    {
        int index = BitOperations.TrailingZeroCount(bitboard);
        int rank = Size - (index / Size);
        char file = (char)('a' + (index % Size));
        return $"{file}{rank}";
    }

    private static List<string> BitboardToSquares(ulong bitboard)
    {
        List<string> squares = new();
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
            if (i % Size == 0) Console.WriteLine();
            Console.Write($"{(bitboard >> i) & 1UL} ");
        }
        Console.WriteLine();
    }

    public void GetFenFromBitboard()
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
        foreach (char piece in PieceSymbols)
            GetAllMovesForPiece(piece);

        Console.WriteLine("\nWhite Pieces:");
        foreach (var moves in _whiteMoves)
        {
            string key = moves.Key;
            List<string> value = moves.Value;
            Console.WriteLine($"{key}: [{string.Join(", ", value)}]");
        }
        Console.WriteLine("\nBlack Pieces:");
        foreach (var moves in _blackMoves)
        {
            string key = moves.Key;
            List<string> value = moves.Value;
            Console.WriteLine($"{key}: [{string.Join(", ", value)}]");
        }
        Console.Write("\nWhite king check? ");
        Console.WriteLine(IsCheck(_bitboards['K'], true));
        Console.Write("\nBlack king check?");
        Console.WriteLine(IsCheck(_bitboards['k'], false));
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
}
}