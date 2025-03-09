import { useState, useEffect } from "react";
import { Container, Typography, Box, Alert, CircularProgress, Slider } from "@mui/material";
import ChessBoard from "./components/ChessBoard";
import GameControls from "./components/GameControls";
import MoveHistory from "./components/MoveHistory";
import FenEditor from "./components/FenEditor";
import { chessApi } from "./services/api";
import { GameMode, GameState, BotColor } from "./types/chess";

const INITIAL_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

const App = () => {
  const [gameState, setGameState] = useState<GameState>({
    fen: INITIAL_FEN,
    legalMoves: [],
    history: [],
    currentMove: -1,
    gameOver: false,
    result: null
  });
  
  const [gameMode, setGameMode] = useState<GameMode>("pvp");
  const [boardOrientation, setBoardOrientation] = useState<"white" | "black">("white");
  const [botPlaying, setBotPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [botColor, setBotColor] = useState<BotColor>("black");
  const [autoPlaySpeed, setAutoPlaySpeed] = useState<number>(1000);
  const [searchTimeout, setSearchTimeout] = useState<number>(3000);
  const [isAutoPlaying, setIsAutoPlaying] = useState<boolean>(false);


  useEffect(() => {
    fetchLegalMoves(INITIAL_FEN);
  }, []);


  useEffect(() => {
    const isBotTurn = 
      gameMode === 'pvbot' && 
      !gameState.gameOver &&
      !botPlaying &&
      ((botColor === 'white' && gameState.fen.includes(' w ')) ||
      (botColor === 'black' && gameState.fen.includes(' b ')));

    if (isBotTurn) {
      // Small delay before bot moves for better UX
      const timer = setTimeout(() => {
        makeBotMove();
      }, 500);
      
      return () => clearTimeout(timer);
    }
  }, [gameState.fen, gameMode, botPlaying, gameState.gameOver, botColor]);


  useEffect(() => {
    if (gameMode === 'botvbot' && isAutoPlaying && !gameState.gameOver && !botPlaying) {
      const timer = setTimeout(() => {
        makeBotMove();
      }, autoPlaySpeed);
      
      return () => clearTimeout(timer);
    }
  }, [gameState.fen, gameMode, isAutoPlaying, botPlaying, gameState.gameOver, autoPlaySpeed]);

  const fetchLegalMoves = async (fen: string) => {
    try {
      const response = await chessApi.getLegalMoves(fen);
      
      setGameState(prev => ({
        ...prev,
        legalMoves: response.legalMoves,
        gameOver: response.checkmate || response.stalemate,
        result: response.checkmate ? 
          (prev.fen.includes(' w ') ? "Black wins by checkmate" : "White wins by checkmate") : 
          response.stalemate ? "Draw by stalemate" : null
      }));
    } catch (error) {
      console.error("Error fetching legal moves:", error);
      setError("Failed to get legal moves");
    }
  };
  

  const handleMove = async (move: string) => {
    try {
      const response = await chessApi.makeMove(gameState.fen, move);
      
      setGameState(prev => {
        const newHistory = prev.currentMove < prev.history.length - 1 
          ? [...prev.history.slice(0, prev.currentMove + 1), move]
          : [...prev.history, move];
        
        return {
          fen: response.fen,
          legalMoves: response.legalMoves,
          history: newHistory,
          currentMove: newHistory.length - 1,
          gameOver: response.checkmate || response.stalemate,
          result: response.checkmate ? 
            (response.fen.includes(' w ') ? "Black wins by checkmate" : "White wins by checkmate") :
            response.stalemate ? "Draw by stalemate" : null
        };
      });
    } catch (err) {
      console.error("Move error:", err);
      setError("Invalid move");
    }
  };

  const makeBotMove = async () => {
    try {
      setBotPlaying(true);
      const response = await chessApi.getBotMove(gameState.fen, searchTimeout);

      setGameState(prev => ({
        fen: response.fen,
        legalMoves: response.legalMoves,
        history: [...prev.history, response.move],
        currentMove: prev.history.length,
        gameOver: response.checkmate || response.stalemate,
        result: response.checkmate ? 
          (response.fen.includes(' w ') ? "Black wins by checkmate" : "White wins by checkmate") :
          response.stalemate ? "Draw by stalemate" : null
      }));
      
      setBotPlaying(false);
    } catch (err) {
      console.error("Bot move error:", err);
      setError("Bot failed to make a move");
      setBotPlaying(false);
    }
  };
  

  const handleNewGame = () => {
    setGameState({
      fen: INITIAL_FEN,
      legalMoves: [],
      history: [],
      currentMove: -1,
      gameOver: false,
      result: null
    });
    fetchLegalMoves(INITIAL_FEN);
    setError(null);
  };


  const handleFlipBoard = () => {
    setBoardOrientation(prev => prev === "white" ? "black" : "white");
  };
  

  const navigateHistory = async (moveIndex: number) => {
    if (moveIndex < -1 || moveIndex >= gameState.history.length) return;
    
    try {
      if (moveIndex === -1) {
        const response = await chessApi.getLegalMoves(INITIAL_FEN);
        
        setGameState(prev => ({
          ...prev,
          fen: INITIAL_FEN,
          legalMoves: response.legalMoves,
          currentMove: -1,
          gameOver: false,
          result: null
        }));
      } else {
        let currentFen = INITIAL_FEN;
        
        for (let i = 0; i <= moveIndex; i++) {
          const response = await chessApi.makeMove(currentFen, gameState.history[i]);
          currentFen = response.fen;
          
          if (i === moveIndex) {
            setGameState(prev => ({
              ...prev,
              fen: response.fen,
              legalMoves: response.legalMoves,
              currentMove: moveIndex,
              gameOver: response.checkmate || response.stalemate,
              result: response.checkmate ?
                (response.fen.includes(' w ') ? "Black wins by checkmate" : "White wins by checkmate") :
                response.stalemate ? "Draw by stalemate" : null
            }));
          }
        }
      }
    } catch (err) {
      console.error("Navigation error:", err);
      setError("Failed to navigate to selected move");
    }
  };
  
  // Update the board with a new FEN string
  const handleFenChange = async (newFen: string) => {
    try {
      const response = await chessApi.getLegalMoves(newFen);
      
      // Reset game state with new FEN
      setGameState({
        fen: response.fen,
        legalMoves: response.legalMoves,
        history: [],
        currentMove: -1,
        gameOver: response.checkmate || response.stalemate,
        result: response.checkmate ?
          (response.fen.includes(' w ') ? "Black wins by checkmate" : "White wins by checkmate") :
          response.stalemate ? "Draw by stalemate" : null
      });
      
      setError(null);
    } catch (err) {
      console.error("FEN error:", err);
      setError("Invalid FEN string");
    }
  };
  
  return (
    <Container maxWidth="lg">      
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}
      
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 2 }}>
        <Box sx={{ flex: "1 1 700px" }}>

          
          <ChessBoard
            fen={gameState.fen}
            legalMoves={gameState.legalMoves}
            onMove={handleMove}
            orientation={boardOrientation}
            disabled={botPlaying || 
              gameState.gameOver || 
              gameMode === 'botvbot' ||  // Disable board in botvsbot mode
              (gameMode === 'pvbot' && 
              ((botColor === 'white' && gameState.fen.includes(' w ')) ||
              (botColor === 'black' && gameState.fen.includes(' b ')))
            )}
          />
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
            <Typography variant="h6">
              {gameState.fen.includes(' w ') ? 'White' : 'Black'}'s Turn
              {(gameMode === 'pvbot' && 
                ((botColor === 'white' && gameState.fen.includes(' w ')) ||
                (botColor === 'black' && gameState.fen.includes(' b ')))) && 
                ' (Bot)'}
              {gameMode === 'botvbot' && ' (Bot)'}
            </Typography>
          </Box>
          {gameState.gameOver && gameState.result && (
            <Alert severity="info" sx={{ mt: 2 }}>
              Game Over: {gameState.result}
            </Alert>
          )}
        </Box>
        
        <Box sx={{ flex: "1 1 300px" }}>
          <MoveHistory
            history={gameState.history}
            currentMove={gameState.currentMove}
            onSelectMove={navigateHistory}
          />
          <FenEditor fen={gameState.fen} onFenChange={handleFenChange} />
          <Box sx={{ width: '100%' }}>
            <Typography gutterBottom>Engine search time</Typography>
            <Slider
              value={searchTimeout}
              onChange={(_, value) => setSearchTimeout(value as number)}
              min={1000}
              max={10000}
              step={500}
              marks={[
                { value: 1000, label: 'Fast' },
                { value: 3000, label: 'Medium' },
                { value: 10000, label: 'Slow' }
              ]}
              valueLabelDisplay="auto"
            />
          </Box>
          <GameControls
            gameMode={gameMode}
            botColor={botColor}
            onNewGame={handleNewGame}
            onFlipBoard={handleFlipBoard}
            onModeChange={setGameMode}
            onBotColorChange={setBotColor}
            onMakeBotMove={makeBotMove}
            isAutoPlaying={isAutoPlaying}
            autoPlaySpeed={autoPlaySpeed}
            onAutoPlayToggle={() => setIsAutoPlaying(prev => !prev)}
            onSpeedChange={setAutoPlaySpeed}
          />
          {botPlaying && (
            <Box>
              <Box sx={{ display: "flex", justifyContent: "center", mt: 2 }}>
                <Typography variant="h6" gutterBottom>
                  Bot is thinking...
                </Typography>
              </Box>
              <Box sx={{ display: "flex", justifyContent: "center", mt: 2 }}>
                <CircularProgress size={80} />
              </Box>
            </Box>
          )}
        </Box>
      </Box>
    </Container>
  );
};

export default App;