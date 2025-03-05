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
                Console.WriteLine(response.LegalMoves.Count);
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

    [Route("api/[controller]")]
    [ApiController]
    public class PerftController : ControllerBase
    {
        [HttpGet("perft")]
        public ActionResult GetPerft([FromQuery] string fen = "", [FromQuery] int depth = 1)
        {
            try
            {
                Chess chess = new(fen);
                long nodes = chess.Perft(depth);
                return Ok(nodes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest(e.Message);
            }
        }

        [HttpGet("perftdivide")]
        public ActionResult GetPerftDivide([FromQuery] string fen = "", [FromQuery] int depth = 1)
        {
            try
            {
                Chess chess = new(fen);
                chess.PerftDivide(depth);
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest(e.Message);
            }
        }

        [HttpGet("verify")]
        public ActionResult VerifyMoveGenerator([FromQuery] string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        {
            try
            {
                Chess chess = new(fen);
                chess.VerifyMoveGenerator();
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest(e.Message);
            }
        }
    }
}