import React from 'react';
import Svg, { Path, Circle } from 'react-native-svg';

export default function GroupIcon({ size = 22, color = '#fff' }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 100 100"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      {/* Left person — head */}
      <circle cx="36" cy="42" r="14" fill="#6366f1" fillOpacity="0.45" />
      {/* Left person — shoulders */}
      <ellipse cx="36" cy="74" rx="22" ry="14" fill="#6366f1" fillOpacity="0.45" />

      {/* Right person — head */}
      <circle cx="62" cy="42" r="14" fill="#6366f1" />
      {/* Right person — shoulders */}
      <ellipse cx="62" cy="74" rx="22" ry="14" fill="#6366f1" />
    </svg>
  );
}