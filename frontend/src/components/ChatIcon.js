import React from 'react';
import Svg, { Path, Circle } from 'react-native-svg';

export default function ChatIcon({ size = 22, color = '#fff' }) {
  return (
    <Svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color}
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <Path
        d="M19 8a2 2 0 0 1 2 2v10.05a.65.65 0 0 1-1.3.45l-1.7-1.828A2 2 0 0 0 16.5 18H9a2 2 0 0 1-2-2v-2"
        strokeOpacity={0.5}
      />
      <Path d="M15 10a2 2 0 0 1-2 2H5.5a2 2 0 0 0-1.5.672L2.3 14.5A.65.65 0 0 1 1 14.05V4a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z" />
      <Circle cx="5.5" cy="7" r="0.8" fill={color} stroke="none" />
      <Circle cx="8" cy="7" r="0.8" fill={color} stroke="none" />
      <Circle cx="10.5" cy="7" r="0.8" fill={color} stroke="none" />
    </Svg>
  );
}