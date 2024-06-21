import React from 'react';
import Square from './Square';
import "../css/styles.css";

const SIZE = 8;

const Board = ({ board, movePiece }) => {
  const squares = [];
  for (let i = 0; i < SIZE; i++) {
    for (let j = 0; j < SIZE; j++) {
      squares.push(String.fromCharCode(97 + j) + (SIZE - i));
    }
  }

  return (
    <div className="board">
      {squares.map((square, index) => (
        <Square key={index} index={square} piece={board.get(square)} movePiece={movePiece} />
      ))}
    </div>
  );
};

export default Board;
