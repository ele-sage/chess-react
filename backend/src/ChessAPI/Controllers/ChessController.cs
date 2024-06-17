using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace ChessAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessController : ControllerBase
    {
        private static bool IsValidFen(string fen)
        {
            string fenPattern = @"\s*^(((?:[rnbqkpRNBQKP1-8]+\/){7})[rnbqkpRNBQKP1-8]+)\s([b|w])\s([K|Q|k|q|-]{1,4})\s(-|[a-h][1-8])\s(\d+\s\d+)$";
            // validate fen with regex
            if (!Regex.IsMatch(fen, fenPattern))
            {
                return false;
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
                        return false;
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
                    return false;
                }
                i++;
            }
            if (kings[0] != 1 || kings[1] != 1)
            {
                return false;
            }
            return true;
        }
        [HttpGet]
        public ActionResult Get([FromQuery] string fen = "")
        {
            if (!IsValidFen(fen))
            {
                return BadRequest("Invalid FEN");
            }
            Chess chess = new(fen);
            List<string> legalMoves = chess.DoTurn();
            string newFen = chess.GetFenFromBitboard();
            return Ok(new { fen = newFen, legalMoves });
        }
    }
}
// 4k2r/r3bppp/p1np4/1p1NpP2/2p1P3/6N1/PPKR2PP/4QB1R w k - 4 23