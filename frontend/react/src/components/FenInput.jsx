import React, { useRef, useEffect } from 'react';
import "../css/input.css";

const BASE_FEN = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

const FenInput = ({ fen, onManualFenChange }) => {
  const inputRef = useRef(fen);

  useEffect(() => {
    inputRef.current.value = fen;
  }, [fen]);
  
  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      onManualFenChange(inputRef.current.value);
    }
  };
  return (
    <div className="container-fen">
      <input className="input-fen" ref={inputRef} type="text" defaultValue={fen} onKeyPress={handleKeyPress} />
      <button className="button-fen" onClick={() => onManualFenChange(inputRef.current.value)}>Set FEN</button>
      <button className="button-fen" onClick={() => onManualFenChange(BASE_FEN)}>New Game</button>
    </div>
  );
};
export default FenInput;