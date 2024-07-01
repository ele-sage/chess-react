import "../css/styles.css";
import "../css/toggle.css";
import React, { useState, useMemo, useEffect } from 'react';
import ChessGame from '../utils/chess';
import Board from './Board';
import FenInput from './FenInput';
import { useLocalStorageState } from './utils';



const PlayAgainstComputer = ({ toggleMode }) => {
  return (
    <label className="switch-bot">

      <input type="checkbox" className="input-bot" onChange={toggleMode} />
      <span className="slider-bot"></span>
    </label>
  );
};

const Chess = () => {
  const [fen, setFen] = useLocalStorageState('FEN', 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1');
  const chessInstance = useMemo(() => new ChessGame(fen, false), []);
  const [board, setBoard] = useState(chessInstance.getBoard());
  const [selectedSquare, setSelectedSquare] = useState(null);
  const [alertMessage, setAlertMessage] = useState('');

  useEffect(() => {
    setBoard(chessInstance.getBoard());
  }, []);

  const movePiece = async (from, to) => {
    try {
      if (chessInstance.movePiece(from, to)) {
        await chessInstance.setLegalMoves();
        setBoard(new Map(chessInstance.getBoard()));
        setFen(chessInstance.getFen());
        if (chessInstance.isAgainstComputer()) {
          await chessInstance.setLegalMoves("bot");
          setBoard(new Map(chessInstance.getBoard()));
          setFen(chessInstance.getFen());
        }
      }
    } catch (e) {
      setAlertMessage(e.message);
    }
  };

  const handleSquareClick = (square) => {
    if (selectedSquare) {
      if (selectedSquare === square) {
        setSelectedSquare(null);
      } else if (chessInstance.isSameColor(selectedSquare, square)) {
        setSelectedSquare(square);
      } else if (chessInstance.isLegalMove(selectedSquare, square)) {
        movePiece(selectedSquare, square);
        setSelectedSquare(null);
      } else {
        setSelectedSquare(null);
      }
    } else {
      if (chessInstance.isTurn(square)) {
        setSelectedSquare(square);
      }
    }
  };

  const onManualFenChange = (newFen) => {
    try {
      chessInstance.setFen(newFen);
      setBoard(new Map(chessInstance.getBoard()));
      setFen(chessInstance.getFen());
    } catch (e) {
      setAlertMessage(e.message);
    }
  }

  useEffect(() => {
    if (chessInstance.getIsCheckmate()) {
      setAlertMessage('Checkmate!');
    } else if (chessInstance.getIsStalemate()) {
      setAlertMessage('Stalemate!');
    } else if (chessInstance.getIsCheck()) {
      setAlertMessage('Check!');
    }
  }, [board]);

  useEffect(() => {
    if (alertMessage) {
      const timer = setTimeout(() => {
        setAlertMessage('');
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [alertMessage]);

  const legalMoves = selectedSquare ? chessInstance.getLegalMoves(selectedSquare) : null;

  return (
    <div className="container-chessboard">
      {alertMessage && (<div className="alert">{alertMessage}</div>)}
      <div className="container-toggle">
        <span className="span-bot">Play against computer</span>
        <PlayAgainstComputer toggleMode={() => chessInstance.toggleMode()} />
      </div>
      <Board board={board} onSquareClick={handleSquareClick} selectedSquare={selectedSquare} legalMoves={legalMoves} />
      <FenInput fen={fen} onManualFenChange={onManualFenChange} />
    </div>
  );
};

export default Chess;