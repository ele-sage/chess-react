import { useState, useEffect } from "react";
import { TextField, Button, Stack, Paper, Typography } from "@mui/material";

interface FenEditorProps {
  fen: string;
  onFenChange: (fen: string) => void;
}

const FenEditor = ({ fen, onFenChange }: FenEditorProps) => {
  const [inputFen, setInputFen] = useState(fen);
  const [error, setError] = useState<string | null>(null);
  
  // Update input when prop changes
  useEffect(() => {
    setInputFen(fen);
  }, [fen]);

  const handleFenChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setInputFen(event.target.value);
  };

  const handleApply = () => {
    try {
      onFenChange(inputFen.trim());
      setError(null);
    } catch (err) {
      setError("Invalid FEN string");
    }
  };

  return (
    <Paper sx={{ p: 2, mt: 2 }}>
      <Typography variant="h6" gutterBottom>
        FEN String
      </Typography>
      <Stack spacing={1}>
        <TextField
          fullWidth
          value={inputFen}
          onChange={handleFenChange}
          error={!!error}
          helperText={error}
          size="small"
          variant="outlined"
        />
        <Stack direction="row" spacing={1}>
          <Button variant="contained" onClick={handleApply}>
            Apply
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};

export default FenEditor;