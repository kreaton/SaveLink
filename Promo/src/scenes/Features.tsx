import { AbsoluteFill, interpolate, spring, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, COLORS, FONT, GRADIENT, useLayout } from "../constants";
import { Word } from "../components/Kinetic";
import { rel } from "./Text";

const s = { fill: "none", stroke: "#fff", strokeWidth: 6, strokeLinecap: "round", strokeLinejoin: "round" } as const;
const ICONS: Record<string, React.ReactNode> = {
  image: (
    <>
      <rect x="10" y="16" width="60" height="48" rx="8" {...s} />
      <path d="M14 58 L34 38 L48 52 L56 44 L68 56" {...s} />
      <circle cx="54" cy="30" r="5" fill="#fff" />
    </>
  ),
  letter: (
    <>
      <rect x="12" y="12" width="56" height="56" rx="14" {...s} />
      <path d="M28 56 L40 24 L52 56 M32 46 H48" {...s} />
    </>
  ),
  ico: (
    <>
      <path d="M20 10 H48 L62 24 V70 H20 Z M48 10 V24 H62" {...s} />
      <path d="M30 42 H52 M30 54 H46" {...s} />
    </>
  ),
  drop: (
    <>
      <path d="M40 10 V46 M26 32 L40 46 L54 32" {...s} />
      <path d="M12 50 V66 H68 V50" {...s} />
    </>
  ),
  browser: (
    <>
      <rect x="8" y="14" width="64" height="52" rx="8" {...s} />
      <path d="M8 28 H72" {...s} />
      <circle cx="18" cy="21" r="2.5" fill="#fff" />
      <circle cx="27" cy="21" r="2.5" fill="#fff" />
    </>
  ),
};

const ITEMS = [
  { icon: "image", text: "Use your own image" },
  { icon: "letter", text: "Generate a letter tile" },
  { icon: "ico", text: "Save the icon as .ico" },
  { icon: "drop", text: "Drop links, text or .url files" },
  { icon: "browser", text: "Pick any installed browser" },
];

// Beats 104-116, the breakdown: what else SaveLink does.
export const Features: React.FC<{ beat0: number }> = ({ beat0 }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const { tall, u } = useLayout();
  const rowH = tall ? 150 : 112;
  const width = tall ? 940 : 1100;
  const drift = interpolate(f, [0, 160], [0, -30], clamp);

  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", flexDirection: "column", gap: tall ? 26 : 18, transform: `translateY(${drift}px)` }}>
      <div style={{ marginBottom: tall ? 40 : 24 }}>
        <Word at={0} size={110} variant="rise">
          And there's more.
        </Word>
      </div>
      {ITEMS.map((it, i) => {
        const at = rel(beat0, beat0 + 2 + i * 2);
        const sp = spring({ frame: f - at, fps, config: { damping: 16, stiffness: 170 } });
        return (
          <div
            key={it.text}
            style={{
              width,
              height: rowH,
              display: "flex",
              alignItems: "center",
              gap: 34,
              padding: "0 34px",
              borderRadius: 28,
              background: "rgba(255,255,255,0.08)",
              border: "2px solid rgba(255,255,255,0.16)",
              boxShadow: "0 20px 50px rgba(0,0,0,0.35)",
              backdropFilter: "blur(8px)",
              opacity: f < at ? 0 : sp,
              transform: `translateX(${(1 - sp) * 260}px) rotateY(${(1 - sp) * -30}deg)`,
            }}
          >
            <div style={{ width: rowH * 0.72, height: rowH * 0.72, borderRadius: 20, background: GRADIENT, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0, boxShadow: `0 0 30px ${COLORS.blue}aa` }}>
              <svg viewBox="0 0 80 80" width={rowH * 0.5} height={rowH * 0.5}>
                {ICONS[it.icon]}
              </svg>
            </div>
            <div style={{ fontFamily: FONT, fontWeight: 700, fontSize: (tall ? 58 : 54) * (tall ? 1 : u), color: "#fff" }}>{it.text}</div>
          </div>
        );
      })}
    </AbsoluteFill>
  );
};
