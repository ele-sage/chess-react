import { Chessboard } from "react-chessboard";
import { useState, useMemo, useEffect } from "react";
import { Square, Piece } from "../types/chess";

interface ChessBoardProps {
  fen: string;
  legalMoves: string[];
  onMove: (move: string) => void;
  orientation: "white" | "black";
  disabled: boolean;
}

type CustomSquareStyles = {
  [square: string]: React.CSSProperties;
};

type BoardPosition = {
    [square in Square]?: Piece;
};

const ChessBoard = ({ fen, legalMoves, onMove, orientation, disabled }: ChessBoardProps) => {
  const [squareStyles, setSquareStyles] = useState<CustomSquareStyles>({});
  const [selectedSquare, setSelectedSquare] = useState<Square | null>(null);
  const [position, setPosition] = useState<BoardPosition>({});

  useEffect(() => {
    setSquareStyles({});
    setSelectedSquare(null);
  }, [fen]);

  const { freeSquares, attackSquares, draggablePieces } = useMemo(() => {
    const freeSquares: { [square: string]: Square[] } = {};
    const attackSquares: { [square: string]: Square[] } = {};
    const draggablePieces: { [square: string]: boolean } = {};

    legalMoves.forEach(move => {
      const [source, target] = move.split(" ");
      draggablePieces[source] = true;
      if (position[target as Square]) {
        if (!attackSquares[source]) {
          attackSquares[source] = [];
        }
        attackSquares[source].push(target as Square);
      } else {
        if (!freeSquares[source]) {
          freeSquares[source] = [];
        }
        freeSquares[source].push(target as Square);
      }
    });
    return { freeSquares, attackSquares, draggablePieces };
  }, [position, legalMoves]);

  const handleMovePiece = (sourceSquare: Square, targetSquare: Square) => {
    const piece = position[sourceSquare];
    const moveString = `${sourceSquare} ${targetSquare} ${piece}`;
    const isLegal = legalMoves.some(move => move === moveString);
    if (isLegal) {
      onMove(`${sourceSquare}${targetSquare}`);
      setSquareStyles({});
      setSelectedSquare(null);
    }
  }

  const handlePieceSelect = (sourceSquare: Square, piece: Piece | undefined) => {
    if (disabled) return;
    if (selectedSquare && (attackSquares[selectedSquare]?.includes(sourceSquare) || !piece)) {
      handleMovePiece(selectedSquare, sourceSquare);
      return;
    }
    if (sourceSquare === selectedSquare) {
      setSquareStyles({});
      setSelectedSquare(null);
      return;
    }
    const styles: CustomSquareStyles = {};
    styles[sourceSquare] = {
      backgroundColor: "rgba(255, 255, 0, 0.4)",
    };
    if (freeSquares[sourceSquare]) {
      freeSquares[sourceSquare].forEach(square => {
        styles[square] = {
          background: "radial-gradient(circle,rgba(22, 77, 0, 0.6) 36%, transparent 40%)",
        };
      });
    }
    if (attackSquares[sourceSquare]) {
      attackSquares[sourceSquare].forEach(square => {
        styles[square] = {
          background: "radial-gradient(circle,transparent 50%, rgba(77, 0, 0, 0.8) 70%)",
        };
      });
    }
    
    setSelectedSquare(sourceSquare);
    setSquareStyles(styles);
  }

  const onPieceDragBegin = (piece: Piece, sourceSquare: Square) => {
    handlePieceSelect(sourceSquare, piece);
  };
  
  const onDrop = (sourceSquare: Square, targetSquare: Square, piece: Piece) => {
    if (disabled) return false;
    
    const moveString = `${sourceSquare} ${targetSquare} ${piece}`;
    const isLegal = legalMoves.some(move => move === moveString);

    if (isLegal) {
      onMove(`${sourceSquare}${targetSquare}`);
      setSquareStyles({});
      setSelectedSquare(null);
      return true;
    }
    
    return false;
  };


  return (
    <div style={{ width: "700px", margin: "0 auto" }}>
      <Chessboard 
        position={fen}
        onPieceDrop={onDrop}
        onSquareClick={handlePieceSelect}
        onPieceDragBegin={onPieceDragBegin}
        isDraggablePiece={({ piece, sourceSquare }: { piece: Piece; sourceSquare: Square }) => draggablePieces[sourceSquare]}
        getPositionObject={setPosition}
        boardOrientation={orientation}
        customSquareStyles={squareStyles}
        autoPromoteToQueen={true}
      />
    </div>
  );
};

export default ChessBoard;