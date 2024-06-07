const SIZE = 8;
const FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
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

function nextChar(c, iter = 1) {
  return String.fromCharCode(c.charCodeAt(0) + iter);
}

function prevChar(c, iter = 1) {
  return String.fromCharCode(c.charCodeAt(0) - iter);
}

class Chess {
  constructor(fen) {
    this.bitboards = new BigUint64Array(12);
    this.fen = fen ? fen : FEN;
    this.turn = 'w';
    this.castle = "";
    this.enPassant = "";
    this.halfmove = 0;
    this.fullmove = 0;
    this.initializeBoard();
    this.pieces = new Map();
    this.setPieces();
  }

  getPawnMoves(pos) {
    const moves = [];
    const direction = this.turn === 'w' ? 1 : -1;
    const startRank = this.turn === 'w' ? 2 : 7;
    const promotionRank = this.turn === 'w' ? 8 : 1;
    const file = pos.charCodeAt(0);
    const rank = parseInt(pos[1]);

    // Single step forward
    const singleStep = String.fromCharCode(file) + (rank + direction);
    if (!this.pieces.has(singleStep)) moves.push(singleStep);

    // Double step forward from starting position
    if (rank === startRank) {
      const doubleStep = String.fromCharCode(file) + (rank + 2 * direction);
      if (!this.pieces.has(doubleStep)) moves.push(doubleStep);
    }

    // Captures
    const captures = [
      String.fromCharCode(file - 1) + (rank + direction),
      String.fromCharCode(file + 1) + (rank + direction)
    ];
    captures.forEach(target => {
      if (this.pieces.has(target) && this.isEnemyPiece(target)) {
        moves.push(target);
      }
    });

    // Promotion
    if (rank + direction === promotionRank) {
      moves.forEach((move, index) => {
        moves[index] = move + "=Q"; // You can handle different promotion pieces
      });
    }

    return moves;
  }

  getKnightMoves(pos) {
    const moves = [];
    const file = pos.charCodeAt(0);
    const rank = parseInt(pos[1]);
    const possibleMoves = [
      [2, 1], [2, -1], [-2, 1], [-2, -1],
      [1, 2], [1, -2], [-1, 2], [-1, -2]
    ];

    possibleMoves.forEach(([df, dr]) => {
      const targetFile = String.fromCharCode(file + df);
      const targetRank = rank + dr;
      if (targetFile >= 'a' && targetFile <= 'h' && targetRank >= 1 && targetRank <= 8) {
        const target = targetFile + targetRank;
        if (!this.pieces.has(target) || this.isEnemyPiece(target)) {
          moves.push(target);
        }
      }
    });

    return moves;
  }

  getBishopMoves(pos) {
    return this.getSlidingMoves(pos, [[1, 1], [1, -1], [-1, 1], [-1, -1]]);
  }

  getRookMoves(pos) {
    return this.getSlidingMoves(pos, [[1, 0], [-1, 0], [0, 1], [0, -1]]);
  }

  getQueenMoves(pos) {
    return this.getSlidingMoves(pos, [[1, 1], [1, -1], [-1, 1], [-1, -1], [1, 0], [-1, 0], [0, 1], [0, -1]]);
  }

  getKingMoves(pos) {
    const moves = [];
    const file = pos.charCodeAt(0);
    const rank = parseInt(pos[1]);
    const possibleMoves = [
      [1, 0], [1, 1], [1, -1], [-1, 0], [-1, 1], [-1, -1], [0, 1], [0, -1]
    ];

    possibleMoves.forEach(([df, dr]) => {
      const targetFile = String.fromCharCode(file + df);
      const targetRank = rank + dr;
      if (targetFile >= 'a' && targetFile <= 'h' && targetRank >= 1 && targetRank <= 8) {
        const target = targetFile + targetRank;
        if (!this.pieces.has(target) || this.isEnemyPiece(target)) {
          moves.push(target);
        }
      }
    });

    return moves;
  }

  getSlidingMoves(pos, directions) {
    const moves = [];
    const file = pos.charCodeAt(0);
    const rank = parseInt(pos[1]);

    directions.forEach(([df, dr]) => {
      let targetFile = file + df;
      let targetRank = rank + dr;
      while (targetFile >= 'a'.charCodeAt(0) && targetFile <= 'h'.charCodeAt(0) && targetRank >= 1 && targetRank <= 8) {
        const target = String.fromCharCode(targetFile) + targetRank;
        if (this.pieces.has(target)) {
          if (this.isEnemyPiece(target)) moves.push(target);
          break;
        }
        moves.push(target);
        targetFile += df;
        targetRank += dr;
      }
    });

    return moves;
  }

  isEnemyPiece(pos) {
    const piece = this.pieces.get(pos)[0];
    if (!piece) return false;
    return (this.turn === 'w' && piece >= 'a' && piece <= 'z') || (this.turn === 'b' && piece >= 'A' && piece <= 'Z');
  }

  getPossibleMoves() {
    this.pieces.forEach((piece, pos) => {
      let moves;
      switch (piece) {
        case 'P': moves = this.getPawnMoves(pos); break;
        case 'N': moves = this.getKnightMoves(pos); break;
        case 'B': moves = this.getBishopMoves(pos); break;
        case 'R': moves = this.getRookMoves(pos); break;
        case 'Q': moves = this.getQueenMoves(pos); break;
        case 'K': moves = this.getKingMoves(pos); break;
        case 'p': moves = this.getPawnMoves(pos); break;
        case 'n': moves = this.getKnightMoves(pos); break;
        case 'b': moves = this.getBishopMoves(pos); break;
        case 'r': moves = this.getRookMoves(pos); break;
        case 'q': moves = this.getQueenMoves(pos); break;
        case 'k': moves = this.getKingMoves(pos); break;
      }
      console.log(`${piece} at ${pos}: ${moves}`);
    });
  }

  setPieces() {
    const bound = (this.turn === 'w') ? ['A','Z'] : ['a','z'];
    let index = 0;
    let file = 'a';
    let rank = '8';

    for (let i = 0; this.fen[i] !== ' ' && this.fen[i]; i++) {
      if (this.fen[i] >= bound[0] && this.fen[i] <= bound[1])
        this.pieces.set(file + rank, this.fen[i]);
      if (this.fen[i] === '/')
      {
        file = 'a';
        rank = prevChar(rank);
      }
      else
      {
        const num = parseInt(this.fen[i])
        if (num)
          file = nextChar(file, num);
        else
          file = nextChar(file);
      }
    }
  }

  initializeBoard() {
    let index = 0;
    let i = 0

    // Board representation
    for (; this.fen[i] !== ' ' && this.fen[i]; i++) {
      const num = parseInt(this.fen[i])

      if (num)
        index += num;
      else if (this.fen[i] !== '/')
        this.bitboards[PIECE.get(this.fen[i])] |= 1n << BigInt(index++);
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
  }

  // Method to print the board for debugging purposes
  printBoard() {
    const pieceSymbols = [
      'P', 'N', 'B', 'R', 'Q', 'K', // White pieces
      'p', 'n', 'b', 'r', 'q', 'k'  // Black pieces
    ];
  
    const files = 'abcdefgh';
    
    // Print the board with rank and file axes
    for (let rank = 0; rank < 8; rank++) {
      let row = `${8 - rank} `;
      for (let file = 0; file < 8; file++) {
        const squareMask = 1n << BigInt(rank * 8 + file);
        let piece = '.';
  
        for (let i = 0; i < this.bitboards.length; i++) {
          if (this.bitboards[i] & squareMask) {
            piece = pieceSymbols[i];
            break;
          }
        }
        row += piece + ' ';
      }
      console.log(row);
    }
    
    // Print the files at the bottom
    let fileRow = '  ';
    for (let file = 0; file < 8; file++) {
      fileRow += `${files[file]} `;
    }
    console.log(fileRow);

    console.log('\n');
    console.log("turn:", this.turn);
    console.log("Castle:", this.castle);
    console.log("En Passant:", this.enPassant);
    console.log("Halfmove:", this.halfmove);
    console.log("Fullmove:", this.fullmove);
    console.log("\n");
    // this.pieces.forEach((value, key) => {
    //   console.log("key:", key, "value:", value);
    // });
    this.getPossibleMoves();
  }
}

// const chess = new Chess("r1bk3r/p2pBpNp/n4n2/1p1NP2P/6P1/3P4/P1P1K3/q5b1");
const chess = new Chess("rnbqkbnr/ppp1pppp/4P3/3p4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3");
                        //  rnbqkbnr/ppppp1pp/8/5p2/3P4/8/PPP1PPPP/RNBQKBNR w KQkq f6 0 1
chess.printBoard();