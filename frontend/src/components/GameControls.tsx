import { 
  Button, Stack, ButtonGroup, ToggleButtonGroup, 
  ToggleButton, Slider, Typography, Box 
} from "@mui/material";
import { GameMode, BotColor } from "../types/chess";

interface GameControlsProps {
  gameMode: GameMode;
  botColor: BotColor;
  onNewGame: () => void;
  onFlipBoard: () => void;
  onModeChange: (mode: GameMode) => void;
  onBotColorChange: (color: BotColor) => void;
  onMakeBotMove: () => void;
  // New props
  isAutoPlaying?: boolean;
  autoPlaySpeed?: number;
  onAutoPlayToggle?: () => void;
  onSpeedChange?: (speed: number) => void;
}

const GameControls = ({
  gameMode,
  botColor,
  onNewGame,
  onFlipBoard,
  onModeChange,
  onBotColorChange,
  onMakeBotMove,
  isAutoPlaying = false,
  autoPlaySpeed = 1000,
  onAutoPlayToggle = () => {},
  onSpeedChange = () => {}
}: GameControlsProps) => {
  return (
    <Stack spacing={2} direction="column" sx={{ my: 2 }}>
      <ButtonGroup variant="contained" aria-label="game mode selection">
        <Button 
          onClick={() => onModeChange('pvp')} 
          color={gameMode === 'pvp' ? "primary" : "inherit"}
        >
          Player vs Player
        </Button>
        <Button 
          onClick={() => onModeChange('pvbot')} 
          color={gameMode === 'pvbot' ? "primary" : "inherit"}
        >
          Player vs Bot
        </Button>
        <Button 
          onClick={() => onModeChange('botvbot')} 
          color={gameMode === 'botvbot' ? "primary" : "inherit"}
        >
          Bot vs Bot
        </Button>
      </ButtonGroup>
      
      {gameMode === 'pvbot' && (
        <Stack direction="row" spacing={1} alignItems="center">
          <ToggleButtonGroup
            value={botColor}
            exclusive
            onChange={(_, newColor) => {
              if (newColor !== null) {
                onBotColorChange(newColor);
              }
            }}
            size="small"
          >
            <ToggleButton value="black">Bot plays Black</ToggleButton>
            <ToggleButton value="white">Bot plays White</ToggleButton>
          </ToggleButtonGroup>
        </Stack>
      )}
      
      {gameMode === 'botvbot' && (
        <Stack spacing={2}>
          <Box sx={{ width: '100%', display: 'flex', alignItems: 'center' }}>
            <Button
              variant="contained"
              color={isAutoPlaying ? "error" : "success"}
              onClick={onAutoPlayToggle}
              sx={{ mr: 2 }}
            >
              {isAutoPlaying ? "Stop" : "Start"} Auto Play
            </Button>
            <Button
              variant="outlined"
              onClick={onMakeBotMove}
              disabled={isAutoPlaying}
            >
              Next Move
            </Button>
          </Box>
          <Box sx={{ width: '100%' }}>
            <Typography gutterBottom>Move Speed: {autoPlaySpeed}ms</Typography>
            <Slider
              value={autoPlaySpeed}
              onChange={(_, value) => onSpeedChange(value as number)}
              disabled={isAutoPlaying}
              min={100}
              max={3000}
              step={100}
              marks={[
                { value: 100, label: 'Fast' },
                { value: 1000, label: 'Medium' },
                { value: 3000, label: 'Slow' }
              ]}
              valueLabelDisplay="auto"
            />
          </Box>
        </Stack>
      )}
      
      <ButtonGroup variant="outlined" aria-label="game controls">
        <Button onClick={onNewGame}>New Game</Button>
        <Button onClick={onFlipBoard}>Flip Board</Button>
        {gameMode === 'pvbot' && (
          <Button 
            onClick={onMakeBotMove}
          >
            Force Bot Move
          </Button>
        )}
      </ButtonGroup>
    </Stack>
  );
};

export default GameControls;