import "../css/toggle.css";
import React from 'react';

const PlayAgainstComputer = ({ toggleMode }) => {
  return (
    <label className="switch-bot">

      <input type="checkbox" className="input-bot" onChange={toggleMode} />
      <span className="slider-bot"></span>
    </label>
  );
};
const BotToggle = ({ chessInstance }) => (
  <>
    <span className="span-bot">Play against computer</span>
    <PlayAgainstComputer toggleMode={() => chessInstance.toggleMode()} />
  </>
);

export default BotToggle;