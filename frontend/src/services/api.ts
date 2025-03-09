import axios from 'axios';

const API_URL = 'http://localhost:5000/api';

export interface GameResponse {
  fen: string;
  legalMoves: string[];
  checkmate: boolean;
  stalemate: boolean;
  checkBy: string[];
}

export interface BotResponse extends GameResponse {
  move: string;
}


export const chessApi = {
  getLegalMoves: async (fen: string = ''): Promise<GameResponse> => {
    const response = await axios.get(`${API_URL}/chess/legalmoves`, {
      params: { fen }
    });
    return response.data;
  },
  
  getBotMove: async (fen: string = '', searchTime: number): Promise<BotResponse> => {
    const response = await axios.get(`${API_URL}/chess/bot`, {
      params: { fen, searchTime }
    });
    return response.data;
  },

  makeMove: async (fen: string, move: string): Promise<GameResponse> => {
    const response = await axios.get(`${API_URL}/chess/move`, {
      params: { fen, move }
    });
    return response.data;
  },
};