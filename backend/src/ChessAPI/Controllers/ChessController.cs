using Microsoft.AspNetCore.Mvc;

namespace ChessAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessController : ControllerBase
    {
        // Based on the query string, chess engine will simply return the fen and legal moves
        [HttpGet]
        public ActionResult GetLegalMoves([FromQuery] string fen = "")
        {
            try
            {
                Chess chess = new(fen);
                List<string> legalMoves = chess.GetPossibleMoves();
                string newFen = chess.GetFenFromBitboard();
                return Ok(new { fen = newFen, legalMoves });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Based on the query string, chess engine will make a move and return the new fen and legal moves
        [HttpGet("bot/")]
        public ActionResult GetBot([FromQuery] string fen = "")
        {
            try
            {
                Chess chess = new(fen);
                List<string> legalMoves = chess.DoTurn();
                string newFen = chess.GetFenFromBitboard();
                return Ok(new { fen = newFen, legalMoves });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
// 4k2r/r3bppp/p1np4/1p1NpP2/2p1P3/6N1/PPKR2PP/4QB1R w k - 4 23