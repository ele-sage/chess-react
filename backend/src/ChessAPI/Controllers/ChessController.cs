using Microsoft.AspNetCore.Mvc;

namespace ChessAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessController : ControllerBase
    {
        [HttpGet("legalmoves")]
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

        [HttpGet("bot")]
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