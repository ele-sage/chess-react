import React, { useState } from "react";
import "../css/squares"
import ChessGame from "../utils/chess";
// import all pieces
import bb from "../assets/pieces/bb.png";
import bk from "../assets/pieces/bk.png";
import bn from "../assets/pieces/bn.png";
import bp from "../assets/pieces/bp.png";
import bq from "../assets/pieces/bq.png";
import br from "../assets/pieces/br.png";
import wb from "../assets/pieces/wb.png";
import wk from "../assets/pieces/wk.png";
import wn from "../assets/pieces/wn.png";
import wp from "../assets/pieces/wp.png";
import wq from "../assets/pieces/wq.png";
import wr from "../assets/pieces/wr.png";

const SIZE = 8;
const SERVER_API = "http://0.0.0.0:5000/api/Chess/";

function nextChar(c, iter = 1) {
  return String.fromCharCode(c.charCodeAt(0) + iter);
}

function prevChar(c, iter = 1) {
  return String.fromCharCode(c.charCodeAt(0) - iter);
}

const Square = ({index, piece}) => {
  const color = (index.charCodeAt(0) + parseInt(index[1])) % 2 === 0 ? 'dark' : 'light';
  let image = null;

  if (piece !== ' ') {
    switch (piece) {
      case 'b': image = <img src={bb} alt="black bishop" />; break;
      case 'k': image = <img src={bk} alt="black king" />; break;
      case 'n': image = <img src={bn} alt="black knight" />; break;
      case 'p': image = <img src={bp} alt="black pawn" />; break;
      case 'q': image = <img src={bq} alt="black queen" />; break;
      case 'r': image = <img src={br} alt="black rook" />; break;
      case 'B': image = <img src={wb} alt="white bishop" />; break;
      case 'K': image = <img src={wk} alt="white king" />; break;
      case 'N': image = <img src={wn} alt="white knight" />; break;
      case 'P': image = <img src={wp} alt="white pawn" />; break;
      case 'Q': image = <img src={wq} alt="white queen" />; break;
      case 'R': image = <img src={wr} alt="white rook" />; break;
      default: break;
    }
  }
  return (
    <div className={`square ${color}`}>
      {image}
    </div>
  );
}

// Chess board component
const Board = ({board, legalMoves}) => {
  const squares = [];
  for (let i = 0; i < SIZE; i++) {
    for (let j = 0; j < SIZE; j++) {
      squares.push(String.fromCharCode(97 + j) + (SIZE - i));
    }
  }

  return (
    <div className="board">
      {squares.map((square, index) => {
        return <Square key={index} index={square} piece={board.get(square)} />
      })}
    </div>
  );
}

const getLegalMoves = (fen) => {
  const legalMoves = new Map();
  fetch(SERVER_API + "?fen=" + fen)
    .then(response => response.json())
    .then(data => {
      if (data.legalMoves[0] === "Stalemate" || data.legalMoves[0] === "Checkmate") {
        legalMoves = data.legalMoves;
      } else {
        data.legalMoves.forEach(move => {
          let [from, to, piece] = move.split(' ');
          if (!legalMoves.has(from)) {
            legalMoves.set(from, []);
          }
          legalMoves.get(from).push(to);
        });
      }
    })
    .catch(error => {
      console.error('Error:', error);
    });
  console.log(legalMoves);
  return legalMoves;
}

const Chess = () => {
  const [chessGame, setChessGame] = useState(new ChessGame());
  const [legalMoves, setLegalMoves] = useState(getLegalMoves(chessGame.getFen()));



  return (
    <div className="chessboard">
      <Board board={chessGame.board} legalMoves={legalMoves} />
    </div>
  );
}

export default Chess;