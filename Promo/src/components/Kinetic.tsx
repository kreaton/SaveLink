import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, FONT, TEXT_GRADIENT, useLayout } from "../constants";

type Variant = "slam" | "rise" | "pop";

export type WordProps = {
  children: React.ReactNode;
  at: number;
  out?: number;
  size: number;
  color?: string;
  gradient?: boolean;
  weight?: number;
  variant?: Variant;
  style?: React.CSSProperties;
};

// One word or phrase that lands on frame `at`. It keeps its layout slot while
// hidden, so the words around it don't shift as it appears.
export const Word: React.FC<WordProps> = ({ children, at, out, size, color = "#fff", gradient, weight = 900, variant = "slam", style }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const { u } = useLayout();
  const t = f - at;
  const s = spring({ frame: t, fps, config: variant === "pop" ? { damping: 9, stiffness: 180, mass: 0.6 } : { damping: 16, stiffness: 260, mass: 0.6 } });

  let transform = "";
  let blur = 0;
  if (variant === "slam") {
    transform = `scale(${interpolate(s, [0, 1], [2.3, 1])})`;
    blur = (1 - s) * 16;
  } else if (variant === "rise") {
    transform = `translateY(${interpolate(s, [0, 1], [0.7, 0])}em)`;
    blur = (1 - s) * 6;
  } else {
    transform = `scale(${interpolate(s, [0, 1], [0.2, 1])})`;
  }
  let opacity = t < 0 ? 0 : interpolate(t, [0, 3], [0, 1], clamp);
  if (out !== undefined && f >= out) {
    const e = interpolate(f - out, [0, 6], [0, 1], clamp);
    opacity *= 1 - e;
    transform += ` scale(${1 + e * 0.25})`;
    blur += e * 10;
  }

  return (
    <span
      style={{
        display: "inline-block",
        fontFamily: FONT,
        fontWeight: weight,
        fontSize: size * u,
        lineHeight: 1.02,
        letterSpacing: "-0.03em",
        color: gradient ? "transparent" : color,
        backgroundImage: gradient ? TEXT_GRADIENT : undefined,
        WebkitBackgroundClip: gradient ? "text" : undefined,
        backgroundClip: gradient ? "text" : undefined,
        textShadow: gradient ? undefined : "0 10px 40px rgba(0,0,0,0.45)",
        transform,
        filter: blur > 0.2 ? `blur(${blur}px)` : undefined,
        opacity,
        whiteSpace: "pre",
        ...style,
      }}
    >
      {children}
    </span>
  );
};

export const Line: React.FC<{ children: React.ReactNode; gap?: number; style?: React.CSSProperties }> = ({ children, gap = 0.28, style }) => (
  <div style={{ display: "flex", flexWrap: "wrap", justifyContent: "center", alignItems: "baseline", columnGap: `${gap}em`, fontSize: 100, ...style }}>{children}</div>
);

// A centred block of kinetic lines.
export const Kinetic: React.FC<{ children: React.ReactNode; style?: React.CSSProperties }> = ({ children, style }) => (
  <AbsoluteFill style={{ display: "flex", flexDirection: "column", justifyContent: "center", alignItems: "center", gap: 10, textAlign: "center", padding: "0 6%", ...style }}>{children}</AbsoluteFill>
);
