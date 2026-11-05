import { interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, COLORS } from "../constants";

// The SaveLink app icon, redrawn as SVG (from the poster art) so each part can be
// animated. `start` is the local frame the build begins on; badgeAt is when the
// arrow badge lands.
export const Logo: React.FC<{ size: number; start?: number; badgeAt?: number }> = ({ size, start = 0, badgeAt = 14 }) => {
  const f = useCurrentFrame() - start;
  const { fps } = useVideoConfig();

  const tile = spring({ frame: f, fps, config: { damping: 13, stiffness: 140 } });
  const drawn = (delay: number, len = 14) => interpolate(f - delay, [0, len], [0, 1], clamp);
  const badge = spring({ frame: f - badgeAt, fps, config: { damping: 9, stiffness: 170, mass: 0.7 } });
  const arrow = spring({ frame: f - badgeAt - 3, fps, config: { damping: 12, stiffness: 160 } });
  const shine = interpolate(f, [badgeAt + 10, badgeAt + 26], [-40, 140], clamp);

  const R = 270;
  const circ = 2 * Math.PI * R;
  const ellipse = 2 * Math.PI * Math.sqrt((122 * 122 + R * R) / 2);
  const line = (x1: number, y1: number, x2: number, y2: number, delay: number) => {
    const len = Math.hypot(x2 - x1, y2 - y1);
    return <line x1={x1} y1={y1} x2={x2} y2={y2} stroke="#fff" strokeWidth={40} strokeDasharray={len} strokeDashoffset={len * (1 - drawn(delay, 10))} />;
  };

  return (
    <svg width={size} height={size} viewBox="150 256 990 1000" style={{ overflow: "visible", transform: `scale(${tile}) rotate(${(1 - tile) * -25}deg)` }}>
      <defs>
        <linearGradient id="tileGrad" x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#2470E0" />
          <stop offset="1" stopColor="#17ABAE" />
        </linearGradient>
        <mask id="badgeHole">
          <rect x="0" y="0" width="1400" height="1400" fill="#fff" />
          <circle cx="863" cy="970" r={225 * badge} fill="#000" />
        </mask>
        <clipPath id="tileClip">
          <rect x="169" y="276" width="952" height="961" rx="170" />
        </clipPath>
        <linearGradient id="shine" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#fff" stopOpacity="0" />
          <stop offset="0.5" stopColor="#fff" stopOpacity="0.55" />
          <stop offset="1" stopColor="#fff" stopOpacity="0" />
        </linearGradient>
      </defs>
      <rect x="169" y="276" width="952" height="961" rx="170" fill="url(#tileGrad)" style={{ filter: `drop-shadow(0 30px 60px rgba(0,0,0,0.5)) drop-shadow(0 0 60px ${COLORS.blue}aa)` }} />
      <g mask="url(#badgeHole)" fill="none">
        <circle cx="562" cy="690" r={R} stroke="#fff" strokeWidth={40} strokeDasharray={circ} strokeDashoffset={circ * (1 - drawn(2, 16))} transform="rotate(-90 562 690)" />
        <ellipse cx="562" cy="690" rx="122" ry={R} stroke="#fff" strokeWidth={40} strokeDasharray={ellipse} strokeDashoffset={ellipse * (1 - drawn(6, 14))} />
        {line(562, 420, 562, 960, 5)}
        {line(292, 690, 832, 690, 7)}
        {line(344, 531, 780, 531, 9)}
        {line(344, 851, 780, 851, 11)}
      </g>
      <g transform={`translate(863 970) scale(${badge}) translate(-863 -970)`}>
        <circle cx="863" cy="970" r="193" fill="#fff" stroke={COLORS.blue} strokeWidth="20" />
        <g transform={`translate(${(1 - arrow) * -160} ${(1 - arrow) * 160})`} opacity={arrow}>
          <polygon points="750,1052 862,936 813,886 968,870 952,1025 905,979 790,1092" fill={COLORS.blue} />
        </g>
      </g>
      <g clipPath="url(#tileClip)">
        <rect x={169 + (shine / 100) * 952 - 150} y="200" width="300" height="1200" fill="url(#shine)" transform="rotate(20 645 756)" />
      </g>
    </svg>
  );
};
