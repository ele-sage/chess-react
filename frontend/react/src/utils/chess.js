const FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
const FEN_PATTERN = /^\s*^(((?:[rnbqkpRNBQKP1-8]+\/){7})[rnbqkpRNBQKP1-8]+)\s([b|w])\s([K|Q|k|q]{1,4}|-)\s([a-h][3|6]|-)\s(\d+)\s(\d+)\s*$/;


class Chess {
  constructor(fen = FEN, mode = false) {
    this.board = new Map([["a8", 'r'], ["b8", 'n'], ["c8", 'b'], ["d8", 'q'], ["e8", 'k'], ["f8", 'b'], ["g8", 'n'], ["h8", 'r'],
                          ["a7", 'p'], ["b7", 'p'], ["c7", 'p'], ["d7", 'p'], ["e7", 'p'], ["f7", 'p'], ["g7", 'p'], ["h7", 'p'],
                          ["a6", ' '], ["b6", ' '], ["c6", ' '], ["d6", ' '], ["e6", ' '], ["f6", ' '], ["g6", ' '], ["h6", ' '],
                          ["a5", ' '], ["b5", ' '], ["c5", ' '], ["d5", ' '], ["e5", ' '], ["f5", ' '], ["g5", ' '], ["h5", ' '],
                          ["a4", ' '], ["b4", ' '], ["c4", ' '], ["d4", ' '], ["e4", ' '], ["f4", ' '], ["g4", ' '], ["h4", ' '],
                          ["a3", ' '], ["b3", ' '], ["c3", ' '], ["d3", ' '], ["e3", ' '], ["f3", ' '], ["g3", ' '], ["h3", ' '],
                          ["a2", 'P'], ["b2", 'P'], ["c2", 'P'], ["d2", 'P'], ["e2", 'P'], ["f2", 'P'], ["g2", 'P'], ["h2", 'P'],
                          ["a1", 'R'], ["b1", 'N'], ["c1", 'B'], ["d1", 'Q'], ["e1", 'K'], ["f1", 'B'], ["g1", 'N'], ["h1", 'R'],]);
    try {
      this.isFenValid(fen);
    } catch (error) {
      console.error(error.message);
      this.fen = FEN;
    }
    this.fen = fen;
    this.turn = 'w';
    this.castle = "KQkq";
    this.enPassant = "-";
    this.halfmove = 0;
    this.fullmove = 1;
    this.legalMoves = new Map();
    this.isCheckmate = false;
    this.isStalemate = false;
    this.checkBy = [];
    if (this.fen !== FEN) this.initializeBoard();
    this.againstComputer = mode
    // this.setLegalMoves();
  }

  isFenValid(fen) {
    if (!FEN_PATTERN.test(fen)) {
      throw new Error("FEN does not match the standard format.");
    }

    let i = 0, lineLength = 0;
    let kings = [0, 0];
    while (i < fen.length && fen[i] !== ' ') {
      if (fen[i] === 'k') kings[0]++;
      else if (fen[i] === 'K') kings[1]++;
      if (fen[i] === '/') {
        if (lineLength !== 8) {
          throw new Error("Each rank must have exactly 8 squares.");
        }
        lineLength = 0;
      } else if (fen[i] >= '1' && fen[i] <= '8') {
        lineLength += parseInt(fen[i]);
      } else if (fen[i].match(/[a-zA-Z]/)) {
        lineLength++;
      } else {
        throw new Error("Invalid character in FEN string.");
      }
      i++;
    }
    if (kings[0] !== 1 || kings[1] !== 1) {
      throw new Error("Each player must have exactly one king.");
    }
  }
  
  getBoard() {
    return this.board;
  }

  async setLegalMoves(mode = "legalmoves") {
    const legalMoves = new Map();
    let alertMessage = null;

    try {
      const response = await fetch(`http://localhost:5000/api/Chess/${mode}?fen=${this.fen}`);
      const data = await response.json();
      this.isStalemate = data.stalemate;
      this.isCheckmate = data.checkmate;
      this.checkBy = data.checkBy;

      data.legalMoves.forEach(move => {
        let [from, to, piece] = move.split(' ');
        if (!legalMoves.has(from)) {
          legalMoves.set(from, []);
        }
        legalMoves.get(from).push(to);
      });
      if (this.againstComputer) {
        this.fen = data.fen;
        this.initializeBoard();
      }
      if (this.isCheckmate) {
        alertMessage = 'Checkmate!';
      } else if (this.isStalemate) {
        alertMessage = 'Stalemate!';
      } else if (this.checkBy.length > 0) {
        alertMessage = `Check by ${this.checkBy}`;
      }
    } catch (error) {
      console.error('Error:', error);
    }

    this.legalMoves = legalMoves;
    return alertMessage;
  }


  initializeBoard() {
    const fenParts = this.fen.split(' ');
    const [position, turn, castling, enPassant, halfmove, fullmove] = fenParts;

    this.board.clear();
    const rows = position.split('/');
    const files = 'abcdefgh';
    for (let row = 0; row < rows.length; row++) {
      let col = 0;
      for (const char of rows[row]) {
        if (char >= '1' && char <= '8') {
          const emptySquares = parseInt(char, 10);
          for (let i = 0; i < emptySquares; i++) {
            const square = files[col] + (8 - row);
            this.board.set(square, ' ');
            col++;
          }
        } else {
          const square = files[col] + (8 - row);
          this.board.set(square, char);
          col++;
        }
      }
    }

    this.turn = turn;
    this.castle = castling;
    this.enPassant = enPassant;
    this.halfmove = parseInt(halfmove, 10);
    this.fullmove = parseInt(fullmove, 10);
  }

  updateFen() {
    const rows = [];
    for (let rank = 8; rank >= 1; rank--) {
      let emptyCount = 0;
      let row = '';
      for (let file = 0; file < 8; file++) {
        const square = String.fromCharCode(97 + file) + rank;
        const piece = this.board.get(square) || ' ';
        if (piece === ' ') {
          emptyCount++;
        } else {
          if (emptyCount > 0) {
            row += emptyCount;
            emptyCount = 0;
          }
          row += piece;
        }
      }
      if (emptyCount > 0) {
        row += emptyCount;
      }
      rows.push(row);
    }
    const fenPosition = rows.join('/');
    this.fen = `${fenPosition} ${this.turn} ${this.castle} ${this.enPassant} ${this.halfmove} ${this.fullmove}`;
  }

  getFen() {
    return this.fen;
  }

  getPiece(square) {
    return this.board.get(square);
  }

  setPiece(square, piece) {
    this.board.set(square, piece);
  }

  printAllOccupiedSquares() {
    for (let [key, value] of this.board) {
      if (value !== ' ') {
        console.log(key, value);
      }
    }
  }

  printBoard() {
    let row = "";
    for (let [key, value] of this.board) {
      row += value + " ";
      if (key[0] === 'h') {
        console.log(row);
        row = "";
      }
    }
  }

  movePiece(from, to) {
    console.log("Piece:", this.getPiece(from), "From:", from, "To:", to);
    if (from === to) {
      return false;
    }
    if (!this.legalMoves.has(from) || !this.legalMoves.get(from).includes(to)) {
      throw new Error("Invalid move.");
    }

    if (this.getPiece(from) === ' ') {
      throw new Error("No piece at the given square.");
    }
    const prevEnPassant = this.enPassant;
    this.enPassant = "-";
    this.halfmove++;

    if (this.getPiece(from).toLowerCase() === 'p' && Math.abs(parseInt(from[1]) - parseInt(to[1])) === 2) {
      this.enPassant = from[0] + (this.turn === 'w' ? '3' : '6');
    }
    else if (this.getPiece(from).toLowerCase() === 'k') {
      if (Math.abs(from.charCodeAt(0) - to.charCodeAt(0)) === 2) {
        if (to.charCodeAt(0) > from.charCodeAt(0)) {
          this.setPiece('h' + from[1], ' ');
          this.setPiece('f' + from[1], this.turn === 'w' ? 'R' : 'r');
        } else {
          this.setPiece('a' + from[1], ' ');
          this.setPiece('d' + from[1], this.turn === 'w' ? 'R' : 'r');
        }
      }
      if (this.turn === 'w') {
        this.castle = this.castle.replace(/[KQ]/g, '');
      }
      else {
        this.castle = this.castle.replace(/[kq]/g, '');
      }
      if (this.castle === "") {
        this.castle = "-";
      }
    }
    else if (this.getPiece(from).toLowerCase() === 'r') {
      if (from === 'a1') {
        this.castle = this.castle.replace(/[Q]/g, '');
      }
      else if (from === 'h1') {
        this.castle = this.castle.replace(/[K]/g, '');
      }
      else if (from === 'a8') {
        this.castle = this.castle.replace(/[q]/g, '');
      }
      else if (from === 'h8') {
        this.castle = this.castle.replace(/[k]/g, '');
      }
      if (this.castle === "") {
        this.castle = "-";
      }
    }

    if (this.getPiece(from).toLowerCase() === 'p') {
      if (to === prevEnPassant) {
        this.setPiece(to[0] + (this.turn === 'w' ? '5' : '4'), ' ');
        this.setPiece(to, this.getPiece(from));
      } else if (to[1] === '1' || to[1] === '8') {
        this.setPiece(to, this.turn === 'w' ? 'Q' : 'q');
      } else {
        this.setPiece(to, this.getPiece(from));
      }
    } else {
      this.setPiece(to, this.getPiece(from));
    }
    this.setPiece(from, ' ');

    if (this.getPiece(to) !== ' ' || this.getPiece(from).toLowerCase() === 'p') {
      this.halfmove = 0;
    }
    if (this.turn === 'b') {
      this.fullmove++;
    }
    this.turn = this.turn === 'w' ? 'b' : 'w';
    this.updateFen();
    return true;
  }

  getPieceColor(piece) {
    return piece === piece.toUpperCase() ? 'w' : 'b';
  }

  isSameColor(from, to) {
    const pieceFrom = this.board.get(from);
    const pieceTo = this.board.get(to);

    if (pieceFrom === ' ' ||  pieceTo  === ' ')
      return false;
    else if (pieceFrom.charCodeAt(0) < 91  &&  pieceTo.charCodeAt(0) < 91)
      return true;
    else if (pieceFrom.charCodeAt(0) > 96  &&  pieceTo.charCodeAt(0) > 96)
      return true;
    return false
  }

  isLegalMove(from, to) {
    const legal = this.legalMoves.get(from);
    if (!legal) {
      return false;
    }
    return legal.includes(to);
  }

  getLegalMoves(square) {
    return this.legalMoves.get(square);
  }

  getAllLegalMoves() {
    return this.legalMoves;
  }

  setAllLegalMoves(legalMoves) {
    this.legalMoves = legalMoves;
  }

  gameStateAlert() {
    console.log(this.checkBy.length);
    if (this.isCheckmate) {
      return 'Checkmate!';
    } else if (this.isStalemate) {
      return 'Stalemate!';
    } else if (this.checkBy.length > 0) {
      return `Check by ${this.checkBy}`;
    }

    return null;
  }

  isTurn(squareSelected) {
    return this.getPieceColor(this.board.get(squareSelected)) === this.turn;
  }

  toggleMode() {
    this.againstComputer = !this.againstComputer;
  }

  isAgainstComputer() {
    return this.againstComputer;
  }

  updateGameState(fen) {
    this.isFenValid(fen);
    this.fen = fen;
    this.initializeBoard();
  }
}

export default Chess;