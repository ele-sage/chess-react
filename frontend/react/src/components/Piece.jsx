import React from 'react';
import { useDrag } from 'react-dnd';

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

const ItemTypes = {
  PIECE: 'piece',
};

const Piece = ({ piece, position }) => {
  const [{ isDragging }, drag] = useDrag(() => ({
    type: ItemTypes.PIECE,
    item: { piece, from: position },
    collect: (monitor) => ({
      isDragging: !!monitor.isDragging(),
    }),
  }), [piece, position]);

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

  const component = image ? <img ref={drag} src={image} alt="piece" style={{ opacity: isDragging ? 0.5 : 1 }} /> : null;
  return component;
};

export default Piece;
