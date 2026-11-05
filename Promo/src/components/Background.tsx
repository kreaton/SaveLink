import { AbsoluteFill, random, useCurrentFrame } from "remotion";
import { COLORS, useLayout } from "../constants";

// Global backdrop: deep navy with drifting brand-coloured light, a perspective grid
// and particles. `pulse` (0-1) briefly brightens it, e.g. on a change of subject.
export const Background: React.FC<{ pulse: number }> = ({ pulse }) => {
  const f = useCurrentFrame();
  const { W, H } = useLayout();

  const blob = (i: number) => {
    const t = f / 90 + i * 2.1;
    return {
      x: W * (0.5 + 0.38 * Math.sin(t * 0.7 + i)),
      y: H * (0.5 + 0.32 * Math.cos(t * 0.5 + i * 1.7)),
    };
  };
  const b1 = blob(0);
  const b2 = blob(1);
  const size = Math.max(W, H);

  return (
    <AbsoluteFill style={{ background: `radial-gradient(ellipse at 50% 40%, #0B2A63 0%, ${COLORS.navy} 55%, ${COLORS.ink} 100%)`, overflow: "hidden" }}>
      <div style={{ position: "absolute", left: b1.x - size * 0.4, top: b1.y - size * 0.4, width: size * 0.8, height: size * 0.8, borderRadius: "50%", background: `radial-gradient(circle, ${COLORS.blue}aa 0%, transparent 65%)`, opacity: 0.55 + pulse * 0.35 }} />
      <div style={{ position: "absolute", left: b2.x - size * 0.35, top: b2.y - size * 0.35, width: size * 0.7, height: size * 0.7, borderRadius: "50%", background: `radial-gradient(circle, ${COLORS.teal}99 0%, transparent 65%)`, opacity: 0.45 + pulse * 0.35 }} />
      {/* Perspective grid floor */}
      <div style={{ position: "absolute", left: -W * 0.5, width: W * 2, top: H * 0.55, height: H, transform: "perspective(600px) rotateX(62deg)", transformOrigin: "50% 0%", backgroundImage: `linear-gradient(${COLORS.cyan}22 2px, transparent 2px), linear-gradient(90deg, ${COLORS.cyan}22 2px, transparent 2px)`, backgroundSize: "120px 120px", backgroundPosition: `0 ${(f * 4) % 120}px`, opacity: 0.5 + pulse * 0.5, maskImage: "linear-gradient(to bottom, black, transparent 80%)", WebkitMaskImage: "linear-gradient(to bottom, black, transparent 80%)" }} />
      {Array.from({ length: 40 }, (_, i) => {
        const x = random(`px${i}`) * W;
        const speed = 0.4 + random(`ps${i}`) * 1.2;
        const y = (random(`py${i}`) * H - f * speed * 2 + H * 10) % H;
        const s = 2 + random(`pz${i}`) * 4;
        return <div key={i} style={{ position: "absolute", left: x, top: y, width: s, height: s, borderRadius: "50%", background: "#fff", opacity: 0.15 + 0.35 * random(`po${i}`) + pulse * 0.2 }} />;
      })}
      <AbsoluteFill style={{ background: `radial-gradient(circle at 50% 50%, ${COLORS.cyan}${Math.round(pulse * 40).toString(16).padStart(2, "0")} 0%, transparent 60%)` }} />
    </AbsoluteFill>
  );
};
