import React from 'react';
import Square from './Square';

const Board = ({ board, onSquareClick, selectedSquare, legalMoves}) => {

  return (
    <div className="chessboard">
      {Array.from(board.entries()).map(([square, piece]) => (
        <Square 
          key={square} 
          square={square} 
          piece={piece} 
          onClick={() => onSquareClick(square)} 
          isSelected={selectedSquare === square}
          isLegalMove={legalMoves ? legalMoves.includes(square) : false}
        />
      ))}
    </div>
  );
};

export default Board;