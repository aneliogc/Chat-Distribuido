import React from 'react';
import Svg, { Path, Circle, Ellipse } from 'react-native-svg';

export default function GroupIcon({ size = 22, color = '#fff' }) {
  return (
    <Svg
      width={size}
      height={size}
      viewBox="0 0 100 100"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      {/* Left person — head */}
      <Circle cx="36" cy="42" r="14" fill="#6366f1" fillOpacity="0.45" />
      {/* Left person — shoulders */}
      <Ellipse cx="36" cy="74" rx="22" ry="14" fill="#6366f1" fillOpacity="0.45" />

      {/* Right person — head */}
      <Circle cx="62" cy="42" r="14" fill="#6366f1" />
      {/* Right person — shoulders */}
      <Ellipse cx="62" cy="74" rx="22" ry="14" fill="#6366f1" />
    </Svg>
  );
}