using Microsoft.AspNetCore.Mvc;
using System;  
using System.Configuration;

namespace ChessAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChessController : ControllerBase
    {
        // [HttpGet]
        // public IActionResult Get()
        // {
        //     Chess chess = new();
        //     chess.DoTurn();
        //     return Ok(chess.GetFenFromBitboard());
        // }

        [HttpGet]
        public ActionResult Get([FromQuery] string fen = "")
        {
            Chess chess = new(fen);
            chess.DoTurn();
            return Ok(chess.GetFenFromBitboard());
        }
    }
}
