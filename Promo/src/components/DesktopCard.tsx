import { Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, COLORS, FONT } from "../constants";

// The four shortcut icons in the 300x300 desktop screenshots.
const ICONS = [
  { x: 37, y: 135 },
  { x: 113, y: 135 },
  { x: 37, y: 235 },
  { x: 113, y: 235 },
];

// Before/after desktop: every shortcut shows the Edge icon until the wipe at
// `wipeAt` reveals the real site icons. Local frames throughout.
export const DesktopCard: React.FC<{
  size: number;
  enterAt?: number;
  questionsAt?: number[];
  shakeAt?: number;
  wipeAt?: number;
  style?: React.CSSProperties;
}> = ({ size, enterAt = 0, questionsAt = [], shakeAt, wipeAt, style }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const k = size / 300;
  const enter = spring({ frame: f - enterAt, fps, config: { damping: 14, stiffness: 150 } });
  const shakeT = shakeAt === undefined ? -1 : f - shakeAt;
  const shake = shakeT >= 0 && shakeT < 14 ? Math.sin(shakeT * 2.2) * 14 * (1 - shakeT / 14) : 0;
  const wipe = wipeAt === undefined ? 0 : interpolate(f - wipeAt, [0, 9], [0, 100], clamp);

  return (
    <div
      style={{
        position: "relative",
        width: size,
        height: size,
        borderRadius: 32,
        overflow: "hidden",
        boxShadow: `0 40px 100px rgba(0,0,0,0.55), 0 0 0 3px rgba(255,255,255,0.18), 0 0 80px ${COLORS.blue}66`,
        transform: `perspective(1400px) rotateY(${(1 - enter) * -35}deg) scale(${enter}) translateX(${shake}px)`,
        opacity: f < enterAt ? 0 : 1,
        ...style,
      }}
    >
      <Img src={staticFile("desktop1.jpg")} style={{ position: "absolute", width: size, height: size }} />
      <Img src={staticFile("desktop2.jpg")} style={{ position: "absolute", width: size, height: size, clipPath: `inset(0 ${100 - wipe}% 0 0)` }} />
      {wipe > 0 && wipe < 100 && (
        <div style={{ position: "absolute", top: 0, bottom: 0, left: `${wipe}%`, width: 8, marginLeft: -4, background: "#fff", boxShadow: `0 0 30px 10px ${COLORS.cyan}` }} />
      )}
      {questionsAt.map((at, i) => {
        const t = f - at;
        const fadeOut = wipeAt === undefined ? 1 : interpolate(f - wipeAt, [0, 6], [1, 0], clamp);
        if (t < 0 || fadeOut <= 0) return null;
        const s = spring({ frame: t, fps, config: { damping: 8, stiffness: 220, mass: 0.5 } });
        const icon = ICONS[i % ICONS.length];
        return (
          <div key={i} style={{ position: "absolute", left: (icon.x + 14) * k, top: (icon.y - 44) * k, width: 30 * k, height: 30 * k, borderRadius: "50%", background: COLORS.accent, color: COLORS.ink, fontFamily: FONT, fontWeight: 900, fontSize: 22 * k, display: "flex", alignItems: "center", justifyContent: "center", transform: `scale(${s})`, opacity: fadeOut, boxShadow: "0 6px 20px rgba(0,0,0,0.4)" }}>
            ?
          </div>
        );
      })}
      {wipeAt !== undefined &&
        ICONS.map((icon, i) => {
          const t = f - wipeAt - 2 - i;
          if (t < 0 || t > 16) return null;
          const r = interpolate(t, [0, 16], [10, 45], clamp) * k;
          return <div key={`s${i}`} style={{ position: "absolute", left: icon.x * k - r, top: (icon.y - 10) * k - r, width: r * 2, height: r * 2, borderRadius: "50%", border: `${3 * k}px solid ${COLORS.accent}`, opacity: 1 - t / 16 }} />;
        })}
    </div>
  );
};
