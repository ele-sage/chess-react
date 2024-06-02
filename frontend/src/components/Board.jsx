import React, { useState } from "react";
import "../css/squares"

const SIZE = 8;

const Square = React.memo(({value}) => {
  return <button className="square">{value}</button>;
});

const Board = () => {
  const [squares, setSquares] = useState([...Array(SIZE * SIZE).keys()]);
  const tiles = squares.map((value, index) => (
    <Square key={index} value={value}/>
  ));
  console.log("rendering");
  return <div className="board">{tiles}</div>;
}

export default Board;