import React from 'react';
import Piece from './Piece'
import '../css/squares'



const Square = ({ square, piece, onClick, isSelected, isLegalMove }) => {
  const tileColor = isSelected && piece !== ' ' ? 'selected' : ((square.charCodeAt(0) + square.charCodeAt(1)) % 2 === 0) ? "dark" : "light";
  
  const isTarget = isLegalMove && piece !== ' ';
  const className = isTarget ? "target" : tileColor;

  const selected = isSelected && piece !== ' ' ? 'selected' : '';

  const markCol = square[1] === '1' ? <div className={`mark-row ${selected}`}>{square[0]}</div> : null;
  const markRow = square[0] === 'h' ? <div className={`mark-col ${selected}`}>{square[1]}</div> : null;

  return (
    <div className={`square ${className}`} onClick={() => onClick(square)}>
      <Piece piece={piece} isLegalMove={isLegalMove} isTarget={isTarget ? "legal-move-enemy " + tileColor : ''} />
      {markCol}
      {markRow}
    </div>
  );
}

export default Square;

// const Square = React.memo(({ square, piece, onClick, isSelected, isLegalMove }) => {
//   const tileColor = ((square.charCodeAt(0) + square.charCodeAt(1)) % 2 === 0) ? "dark" : "light";
//   const isTarget = isLegalMove && piece !== ' ';
//   const className = isTarget ? "target" : tileColor;
//   console.log(square);
//   return (
//     <div className={`square ${className} ${isSelected && piece !== ' ' ? 'selected' : ''}`} onClick={() => onClick(square)}>
//       <Piece piece={piece} isLegalMove={isLegalMove} isTarget={isTarget ? "legal-move-enemy " + tileColor : ''} />
//     </div>
//   );
// }, (prevProps, nextProps) => {
//   return (prevProps.piece === nextProps.piece &&
//     prevProps.isSelected === nextProps.isSelected &&
//     prevProps.isLegalMove === nextProps.isLegalMove &&
//     prevProps.onClick === nextProps.onClick);
//   });