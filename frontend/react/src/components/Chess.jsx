import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import Board from './Board';
import ChessGame from '../utils/chess';
import "../css/styles.css";

const Chess = () => {
  const chessGame = useMemo(() => new ChessGame(), []);
  const [board, setBoard] = useState(chessGame.getBoard());
  const [alertMessage, setAlertMessage] = useState('');

  const movePiece = useCallback((from, to) => {
    try {
      if (chessGame.movePiece(from, to)) {
        setBoard(new Map(chessGame.getBoard()));
        chessGame.setLegalMoves();
      }
    } catch (e) {
      setAlertMessage(e.message);
    }
    if (chessGame.getIsCheckmate()) {
      setAlertMessage('Checkmate!');
    } else if (chessGame.getIsCheck()) {
      setAlertMessage('Check!');
    } else if (chessGame.getIsStalemate()) {
      setAlertMessage('Stalemate!');
    }
  }, [chessGame]);

  useEffect(() => {
    if (alertMessage) {
      const timer = setTimeout(() => {
        setAlertMessage('');
      }, 3000);
      return () => clearTimeout(timer); // Clear timeout if component unmounts or alertMessage changes
    }
  }, [alertMessage]);


  return (
    <DndProvider backend={HTML5Backend}>
      <div className="chessboard">
        {alertMessage && (
          <div className="alert">
            {alertMessage}
          </div>
        )}
        <Board board={board} movePiece={movePiece} />
      </div>
    </DndProvider>
  );
};

export default Chess;
