import "../css/styles.css";
import React, { useState, useMemo, useEffect } from 'react';
import ChessGame from '../utils/chess';
import Board from './Board';
import FenInput from './FenInput';
import BotToggle from "./BotToggle";
import { useLocalStorageState } from './utils';

const BASE_FEN = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

const Chess = () => {
  const [fen, setFen] = useLocalStorageState('FEN', BASE_FEN);
  const chessInstance = useMemo(() => new ChessGame(fen, false), []);
  const [board, setBoard] = useState(chessInstance.getBoard());
  const [selectedSquare, setSelectedSquare] = useState(null);
  const [alertMessage, setAlertMessage] = useState('');
  const [history, setHistory] = useState([chessInstance.getFen()]);
  const [currentMoveIndex, setCurrentMoveIndex] = useState(0);

  const fetchLegalMoves = async (mode = "legalMoves") => {
    setAlertMessage(await chessInstance.setLegalMoves(mode));
    setBoard(new Map(chessInstance.getBoard()));
    setFen(chessInstance.getFen());
  };

  useEffect(() => { 
    fetchLegalMoves();
    setHistory([chessInstance.getFen()]);
    setCurrentMoveIndex(0);
  }, []);

  const movePiece = async (from, to) => {
    try {
      if (chessInstance.movePiece(from, to)) {
        await fetchLegalMoves();
        if (chessInstance.isAgainstComputer()) {
          await fetchLegalMoves("bot");
        }
        const newFen = chessInstance.getFen();
        if (history[currentMoveIndex] !== newFen) {
          const newHistory = history.slice(0, currentMoveIndex + 1);
          newHistory.push(newFen);
          setHistory(newHistory);
          setCurrentMoveIndex(newHistory.length - 1);
        }
      }
    } catch (e) {
      setAlertMessage(e.message);
    }
  };

  const handleSquareClick = (square) => {
    console.log(chessInstance.getAllLegalMoves());
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

  const onManualFenChange = async (newFen) => {
    try {
      chessInstance.updateGameState(newFen);
      await fetchLegalMoves();

      if (history[currentMoveIndex] !== newFen) {
        const newHistory = history.slice(0, currentMoveIndex + 1);
        newHistory.push(newFen);
        setHistory(newHistory);
        setCurrentMoveIndex(newHistory.length - 1);
      }
    } catch (e) {
      setAlertMessage(e.message);
    }
  };

  const legalMoves = selectedSquare ? chessInstance.getLegalMoves(selectedSquare) : null;

  const goToPreviousMove = async () => {
    if (currentMoveIndex > 0) {
      setCurrentMoveIndex(currentMoveIndex - 1);
      const previousFen = history[currentMoveIndex - 1];
      chessInstance.updateGameState(previousFen);
      await fetchLegalMoves();
      setSelectedSquare(null);
    }
  };

  const goToNextMove = async () => {
    if (currentMoveIndex < history.length - 1) {
      setCurrentMoveIndex(currentMoveIndex + 1);
      const nextFen = history[currentMoveIndex + 1];
      chessInstance.updateGameState(nextFen);
      await fetchLegalMoves();
      setSelectedSquare(null);
    }
  };

  return (
    <div className="container-chessboard">
      {alertMessage && (<div className="alert">{alertMessage}</div>)}
      <div className="container-toggle">
        <button className="button-fen prev" onClick={goToPreviousMove} disabled={currentMoveIndex === 0}>Previous</button>
        <button className="button-fen next" onClick={goToNextMove} disabled={currentMoveIndex === history.length - 1}>Next</button>
        <BotToggle chessInstance={chessInstance} />
      </div>
      <Board board={board} onSquareClick={handleSquareClick} selectedSquare={selectedSquare} legalMoves={legalMoves} />
      <FenInput fen={fen} onManualFenChange={onManualFenChange} />
    </div>
  );
};

export default Chess;
