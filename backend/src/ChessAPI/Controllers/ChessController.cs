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
                GameResponse response = chess.GetLegalMoves();
                Console.WriteLine(response.Fen);
                return Ok(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest(e.Message);
            }
        }

        [HttpGet("bot")]
        public ActionResult GetBot([FromQuery] string fen = "")
        {
            try
            {
                Chess chess = new(fen);
                GameResponse response = chess.GetLegalMovesAfterBot();
                return Ok(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest(e.Message);
            }
        }
    }
}