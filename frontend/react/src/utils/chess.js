const SIZE = 8;
const FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
const FEN_PATTERN = /^\s*^(((?:[rnbqkpRNBQKP1-8]+\/){7})[rnbqkpRNBQKP1-8]+)\s([b|w])\s([K|Q|k|q]{1,4}|-)\s([a-h][3|6]|-)\s(\d+)\s(\d+)\s*$/;
const PIECE = new Map([
  ['P', 0],
  ['N', 1],
  ['B', 2],
  ['R', 3],
  ['Q', 4],
  ['K', 5],
  ['p', 6],
  ['n', 7],
  ['b', 8],
  ['r', 9],
  ['q', 10],
  ['k', 11],
]);

class Chess {
  constructor(fen = FEN) {
    this.board = new Map([["a1", 'R'], ["b1", 'N'], ["c1", 'B'], ["d1", 'Q'], ["e1", 'K'], ["f1", 'B'], ["g1", 'N'], ["h1", 'R'],
                          ["a2", 'P'], ["b2", 'P'], ["c2", 'P'], ["d2", 'P'], ["e2", 'P'], ["f2", 'P'], ["g2", 'P'], ["h2", 'P'],
                          ["a3", ' '], ["b3", ' '], ["c3", ' '], ["d3", ' '], ["e3", ' '], ["f3", ' '], ["g3", ' '], ["h3", ' '],
                          ["a4", ' '], ["b4", ' '], ["c4", ' '], ["d4", ' '], ["e4", ' '], ["f4", ' '], ["g4", ' '], ["h4", ' '],
                          ["a5", ' '], ["b5", ' '], ["c5", ' '], ["d5", ' '], ["e5", ' '], ["f5", ' '], ["g5", ' '], ["h5", ' '],
                          ["a6", ' '], ["b6", ' '], ["c6", ' '], ["d6", ' '], ["e6", ' '], ["f6", ' '], ["g6", ' '], ["h6", ' '],
                          ["a7", 'p'], ["b7", 'p'], ["c7", 'p'], ["d7", 'p'], ["e7", 'p'], ["f7", 'p'], ["g7", 'p'], ["h7", 'p'],
                          ["a8", 'r'], ["b8", 'n'], ["c8", 'b'], ["d8", 'q'], ["e8", 'k'], ["f8", 'b'], ["g8", 'n'], ["h8", 'r']]);
    this.fen = fen;
    this.turn = 'w';
    this.castle = "KQkq";
    this.enPassant = "-";
    this.halfmove = 0;
    this.fullmove = 1;
    if (fen !== FEN) this.initializeBoard();
  }

  isFenValid() {
    if (!FEN_PATTERN.test(this.fen)) {
      throw new Error("FEN does not match the standard format.");
    }

    let i = 0, lineLength = 0;
    let kings = [0, 0];
    while (i < this.fen.length && this.fen[i] !== ' ') {
      if (this.fen[i] === 'k') kings[0]++;
      else if (this.fen[i] === 'K') kings[1]++;
      if (this.fen[i] === '/') {
        if (lineLength !== 8) {
          throw new Error("Each rank must have exactly 8 squares.");
        }
        lineLength = 0;
      } else if (this.fen[i] >= '1' && this.fen[i] <= '8') {
        lineLength += parseInt(this.fen[i]);
      } else if (this.fen[i].match(/[a-zA-Z]/)) {
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

  // make a http request to the server to get the legal moves
  // https://0.0.0.0:5000/api/Chess?fen=this.fen
  // Response body: {
  //   "fen": "4k2r/r3bppp/p1np4/1p1NpP2/2p1P3/6N1/PPKR2PP/4QB1R w k - 4 23",
  //   "legalMoves": [
  //     "f5 f6 P",
  //     "a2 a4 P",
  //     "a2 a3 P",
  //     "b2 b4 P",
  //     "b2 b3 P",
  //     "h2 h4 P",
  //     "h2 h3 P",
  //     "d5 c7 N",
  //     "d5 e7 N",
  //     "d5 b6 N",
  //     "d5 f6 N",
  //     "d5 b4 N",
  //     "d5 f4 N",
  //     "d5 c3 N",
  //     "d5 e3 N",
  //     "g3 h5 N",
  //     "g3 e2 N",
  //     "f1 c4 B",
  //     "f1 d3 B",
  //     "f1 e2 B",
  //     "d2 d4 R",
  //     "d2 d3 R",
  //     "d2 e2 R",
  //     "d2 f2 R",
  //     "d2 d1 R",
  //     "h1 g1 R",
  //     "e1 e3 Q",
  //     "e1 e2 Q",
  //     "e1 f2 Q",
  //     "e1 a1 Q",
  //     "e1 b1 Q",
  //     "e1 c1 Q",
  //     "e1 d1 Q",
  //     "c2 c3 K",
  //     "c2 b1 K",
  //     "c2 c1 K",
  //     "c2 d1 K"
  //   ]
  // }
  //  OR
  // Response body: {
  //   "fen": "4k2r/r3bppp/p1np4/1p1NpP2/2p1P3/6N1/PPKR2PP/4QB1R w k - 4 23",
  //   "legalMoves": ["Stalemate"] or ["Checkmate"]
  // }


  initializeBoard() {
    let index = 0;
    let i = 0

    this.isFenValid();

    // Board representation
    for (let row = 8; row >= 1; row--) {
      for (let col = 'a'; col <= 'h'; col++) {
        if (this.fen[i] === '/') {
          i++;
        }
        if (this.fen[i] >= '1' && this.fen[i] <= '8') {
          index += parseInt(this.fen[i]);
          i++;
        }
        this.board.set(col + row, this.fen[i]);
        index++;
        i++;
      }
    }
    
    // Active Color
    this.turn = this.fen[++i];
  
    // Castling Rights
    for (i+=2; this.fen[i] !== ' ' && this.fen[i]; i++)
      this.castle += this.fen[i];
  
    // Possible En Passant Targets
    for (++i; this.fen[i] !== ' ' && this.fen[i]; i++)
      this.enPassant += this.fen[i];
  
    // Halfmove Clock
    let halfmove = "";
    for (++i; this.fen[i] !== ' ' && this.fen[i]; i++)
      halfmove += this.fen[i];
    this.halfmove = parseInt(halfmove)
  
    // Fullmove Number
    let fullmove = "";
    for (++i; this.fen[i] !== ' ' && this.fen[i]; i++)
      fullmove += this.fen[i];
    this.fullmove = parseInt(fullmove);

    // Create a map of legal moves for each piece of the active color
    // Key: piece location, Value: list of legal moves
    // this.setLegalMoves();

  }

  updateFen() {
    let fen = "";
    let empty = 0;
    for (let row = 8; row >= 1; row--) {
      for (let col = 'a'; col <= 'h'; col++) {
        let piece = this.board.get(col + row);
        if (piece === ' ') {
          empty++;
        } else {
          if (empty > 0) {
            fen += empty;
            empty = 0;
          }
          fen += piece;
        }
      }
      if (empty > 0) {
        fen += empty;
        empty = 0;
      }
      if (row > 1) {
        fen += "/";
      }
    }
    fen += " " + this.turn + " " + this.castle + " " + this.enPassant + " " + this.halfmove + " " + this.fullmove;
    this.fen = fen;
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

  movePiece(from, to) {
    if (!this.legalMoves.has(from) || !this.legalMoves.get(from).includes(to)) {
      throw new Error("Invalid move.");
    }

    if (this.getPiece(from) === ' ') {
      throw new Error("No piece at the given square.");
    }

    this.enPassant = "-";
    this.halfmove++;

    if (this.getPiece(from).toLowerCase() === 'p' && Math.abs(parseInt(from[1]) - parseInt(to[1])) === 2) {
      this.enPassant = from[0] + (parseInt(from[1]) + 1);
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
      if (to === this.enPassant) {
        this.setPiece(to[0] + (this.turn === 'w' ? '5' : '4'), ' ');
      } else if (to[1] === '1' || to[1] === '8') {
        this.setPiece(to, this.turn === 'w' ? 'Q' : 'q');
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
  }

  getPieceColor(piece) {
    return piece === piece.toUpperCase() ? 'w' : 'b';
  }

  getLegalMoves(square) {
    return this.legalMoves.get(square);
  }

  getLegalMovesMap() {
    return this.legalMoves;
  }
}

// const chess = new Chess("r1bk3r/p2pBpNp/n4n2/1p1NP2P/6P1/3P4/P1P1K3/q5b1");
// const chess = new Chess("rnbqkbnr/ppp1pppp/4P3/3p4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3");
                        //  rnbqkbnr/ppppp1pp/8/5p2/3P4/8/PPP1PPPP/RNBQKBNR w KQkq f6 0 1

export default Chess;