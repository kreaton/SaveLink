import { AbsoluteFill, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, COLORS, FONT, useLayout } from "../constants";
import { Logo } from "../components/Logo";
import { Word } from "../components/Kinetic";
import { rel } from "./Text";

// Beats 140 to the end: logo build, wordmark, tagline, Store badge.
export const LogoOutro: React.FC<{ beat0: number }> = ({ beat0 }) => {
  const f = useCurrentFrame();
  const { fps } = useVideoConfig();
  const { tall } = useLayout();
  const badgeAt = rel(beat0, beat0 + 2);
  const wordAt = rel(beat0, beat0 + 4);
  const tagAt = rel(beat0, beat0 + 6);
  const storeAt = rel(beat0, beat0 + 8);

  const rays = interpolate(f - badgeAt, [0, 6, 60], [0, 1, 0.35], clamp);
  const float = Math.sin(f / 20) * 6;
  const store = spring({ frame: f - storeAt, fps, config: { damping: 14, stiffness: 150 } });
  const logoSize = tall ? 520 : 380;
  const letters = "SaveLink".split("");

  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", flexDirection: "column" }}>
      <AbsoluteFill
        style={{
          background: `repeating-conic-gradient(from ${f * 0.6}deg at 50% ${tall ? 36 : 34}%, ${COLORS.cyan}22 0deg 6deg, transparent 6deg 18deg)`,
          opacity: rays,
          maskImage: "radial-gradient(circle at 50% 35%, black 10%, transparent 60%)",
          WebkitMaskImage: "radial-gradient(circle at 50% 35%, black 10%, transparent 60%)",
        }}
      />
      <div style={{ transform: `translateY(${float}px)`, marginTop: tall ? -160 : -40 }}>
        <Logo size={logoSize} badgeAt={badgeAt} />
      </div>
      <div style={{ display: "flex", marginTop: tall ? 40 : 10 }}>
        {letters.map((l, i) => (
          <Word key={i} at={wordAt + i * 2} size={tall ? 250 : 190} weight={700} style={{ letterSpacing: "-0.01em" }}>
            {l}
          </Word>
        ))}
      </div>
      <div style={{ marginTop: tall ? 30 : 6, maxWidth: tall ? 960 : 1500, textAlign: "center" }}>
        <Word at={tagAt} size={tall ? 72 : 52} weight={500} variant="rise" color="#d7e7ff">
          {tall ? "Web links as desktop shortcuts\nwith the site's own icon" : "Web links as desktop shortcuts with the site's own icon"}
        </Word>
      </div>
      <div
        style={{
          marginTop: tall ? 80 : 44,
          display: "flex",
          alignItems: "center",
          gap: 22,
          padding: tall ? "20px 40px" : "14px 32px",
          borderRadius: 20,
          background: "#000",
          border: "2px solid rgba(255,255,255,0.35)",
          boxShadow: "0 20px 50px rgba(0,0,0,0.5)",
          opacity: f < storeAt ? 0 : store,
          transform: `translateY(${(1 - store) * 40}px)`,
        }}
      >
        <Img src={staticFile("store.png")} style={{ width: tall ? 76 : 58, height: tall ? 76 : 58 }} />
        <div style={{ fontFamily: FONT, color: "#fff", lineHeight: 1.1 }}>
          <div style={{ fontSize: tall ? 26 : 20, fontWeight: 400 }}>Get it from the</div>
          <div style={{ fontSize: tall ? 44 : 34, fontWeight: 700 }}>Microsoft Store</div>
        </div>
      </div>
    </AbsoluteFill>
  );
};
