import React from 'react';
import { useDrop } from 'react-dnd';
import "../css/squares.css";
import Piece from './Piece';

const ItemTypes = {
  PIECE: 'piece',
};

const Square = ({ index, piece, movePiece }) => {
  const color = (index.charCodeAt(0) + parseInt(index[1])) % 2 === 0 ? 'dark' : 'light';
  const [{ isOver }, drop] = useDrop(() => ({
    accept: ItemTypes.PIECE,
    drop: (item) => movePiece(item.from, index),
    collect: (monitor) => ({
      isOver: !!monitor.isOver(),
    }),
  }), [index]);

  return (
    <div ref={drop} className={`square ${color} ${isOver ? 'hover' : ''}`}>
      {piece && <Piece piece={piece} position={index} />}
    </div>
  );
};

export default Square;
