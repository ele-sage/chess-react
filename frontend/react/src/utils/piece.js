class PieceEnum {
  constructor() {
    this.pieceMap = new Map([
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
  }

  // Get value by key
  get(key) {
    return this.pieceMap.get(key);
  }

  // Get key by value
  getKey(value) {
    for (const [key, val] of this.pieceMap.entries()) {
      if (val === value) {
        return key;
      }
    }
    return null;
  }

  // Iterate over entries
  forEach(callback) {
    this.pieceMap.forEach(callback);
  }
}

export { PieceEnum };