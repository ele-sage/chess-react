import React from 'react';

import bb from "../assets/pieces/bb.png";
import bk from "../assets/pieces/bk.png";
import bn from "../assets/pieces/bn.png";
import bp from "../assets/pieces/bp.png";
import bq from "../assets/pieces/bq.png";
import br from "../assets/pieces/br.png";
import wb from "../assets/pieces/wb.png";
import wk from "../assets/pieces/wk.png";
import wn from "../assets/pieces/wn.png";
import wp from "../assets/pieces/wp.png";
import wq from "../assets/pieces/wq.png";
import wr from "../assets/pieces/wr.png";

const Piece = ({piece, isLegalMove, isTarget}) => {

  let image = null;
  switch (piece) {
    case 'b': image = bb; break;
    case 'k': image = bk; break;
    case 'n': image = bn; break;
    case 'p': image = bp; break;
    case 'q': image = bq; break;
    case 'r': image = br; break;
    case 'B': image = wb; break;
    case 'K': image = wk; break;
    case 'N': image = wn; break;
    case 'P': image = wp; break;
    case 'Q': image = wq; break;
    case 'R': image = wr; break;
    default: break;
  }

  return image ? <div className={isTarget}><img src={image} /></div> : <div className={isLegalMove ? 'legal-move' : ''}/>;
};

export default Piece;
