import { List, ListItem, ListItemButton, Typography, Paper, Box } from "@mui/material";

interface MoveHistoryProps {
  history: string[];
  currentMove: number;
  onSelectMove: (index: number) => void;
}

// Piece symbols mapping
const pieceSymbols: Record<string, string> = {
  'wP': '♙', 'wN': '♘', 'wB': '♗', 'wR': '♖', 'wQ': '♕', 'wK': '♔',
  'bP': '♟', 'bN': '♞', 'bB': '♝', 'bR': '♜', 'bQ': '♛', 'bK': '♚',
};

const MoveHistory = ({ history, currentMove, onSelectMove }: MoveHistoryProps) => {
  if (history.length === 0) {
    return (
      <Paper sx={{ p: 2, mt: 2, maxHeight: "300px", overflow: "auto" }}>
        <Typography variant="h6" gutterBottom>
          Move History
        </Typography>
        <Typography variant="body2" color="text.secondary">
          No moves yet
        </Typography>
      </Paper>
    );
  }

  // Format moves for display: 1. e2e4 e7e5
  const formattedMoves = [];
  for (let i = 0; i < history.length; i += 2) {
    const moveNumber = Math.floor(i / 2) + 1;
    const whiteMove = history[i];
    const blackMove = i + 1 < history.length ? history[i + 1] : "";
    
    formattedMoves.push({
      number: moveNumber,
      white: whiteMove,
      black: blackMove,
      whiteIndex: i,
      blackIndex: i + 1
    });
  }

  return (
    <Paper sx={{ p: 2, mt: 2, maxHeight: "300px", overflow: "auto" }}>
      <Typography variant="h6" gutterBottom>
        Move History
      </Typography>
      <Box sx={{ maxHeight: "250px", overflow: "auto" }}>
        <List dense disablePadding>
          {formattedMoves.map((move) => (
            <ListItem disablePadding key={move.number} sx={{ mb: 0.5 }}>
              <Typography variant="body2" sx={{ width: "30px", color: "text.secondary" }}>
                {`${move.number}.`}
              </Typography>
              <ListItemButton 
                selected={currentMove === move.whiteIndex}
                onClick={() => onSelectMove(move.whiteIndex)}
                sx={{ 
                  py: 0, 
                  minWidth: '80px',
                  borderRadius: 1,
                  bgcolor: currentMove === move.whiteIndex ? 'action.selected' : 'transparent'
                }}
              >
                <Typography variant="body2">
                  {formatMoveWithPiece(move.white, true)}
                </Typography>
              </ListItemButton>
              {move.black ? (
                <ListItemButton 
                  selected={currentMove === move.blackIndex}
                  onClick={() => onSelectMove(move.blackIndex)}
                  sx={{ 
                    py: 0, 
                    minWidth: '80px', 
                    borderRadius: 1,
                    bgcolor: currentMove === move.blackIndex ? 'action.selected' : 'transparent'
                  }}
                >
                  <Typography variant="body2">
                    {formatMoveWithPiece(move.black, false)}
                  </Typography>
                </ListItemButton>
              ) : <ListItemButton sx={{ px: 5, py: 0, minWidth: '80px' }} />}
            </ListItem>
          ))}
        </List>
      </Box>
    </Paper>
  );
};

// Format move with piece symbol
const formatMoveWithPiece = (move: string, isWhite: boolean): React.ReactNode => {
  if (!move || move.length < 4) return move;
  
  // Extract move coordinates
  const from = move.substring(0, 2);
  const to = move.substring(2, 4);
  
  // Derive piece type from coordinate (simplified assumption)
  // In a real implementation, we'd get the actual piece from the position
  const pieceType = getPieceTypeFromMove(from, isWhite);
  const symbol = pieceSymbols[pieceType] || '';
  
  return (
    <span>
      <span style={{ marginRight: '4px' }}>{symbol}</span>
      <span>{`${from}-${to}`}</span>
    </span>
  );
};

// Simple helper to guess piece type based on starting square
const getPieceTypeFromMove = (fromSquare: string, isWhite: boolean): string => {
  // This is a simplified approach - in reality you'd want to get the actual piece
  // from the position data or from metadata stored with the move
  const color = isWhite ? 'w' : 'b';
  
  // For pawns on their starting ranks
  if ((isWhite && fromSquare[1] === '2') || (!isWhite && fromSquare[1] === '7')) {
    return `${color}P`;
  }
  
  // For pieces on back rank
  if ((isWhite && fromSquare[1] === '1') || (!isWhite && fromSquare[1] === '8')) {
    const file = fromSquare[0];
    
    switch (file) {
      case 'a':
      case 'h':
        return `${color}R`; // Rook
      case 'b':
      case 'g':
        return `${color}N`; // Knight
      case 'c':
      case 'f':
        return `${color}B`; // Bishop
      case 'd':
        return `${color}Q`; // Queen
      case 'e':
        return `${color}K`; // King
      default:
        return `${color}P`; // Default to pawn
    }
  }
  
  // Default to pawn for other squares
  return `${color}P`;
};

export default MoveHistory;