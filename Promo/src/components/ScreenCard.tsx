import { AbsoluteFill, Easing, interpolate, OffthreadVideo, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { clamp, COLORS, FONT, GRADIENT, SRC_H, SRC_W, useLayout } from "../constants";

// All times below are seconds in the source recording.
export type CamKey = {
  t: number;
  cx: number;
  cy: number;
  z: number; // 1 = whole recording width fits the card
  dur?: number; // frames the move takes
  // Overrides for the vertical frame (default: same centre, tighter zoom).
  tcx?: number;
  tcy?: number;
  tz?: number;
};
export type Callout = { from: number; to: number; rect: [number, number, number, number]; label: string };
export type Click = { t: number; x: number; y: number };
export type Tag = { t: number; num: string; text: string };

type Box = { x: number; y: number; w: number; h: number };

export const useCardBox = (fullBleed?: boolean): Box => {
  const { tall, W, H } = useLayout();
  if (fullBleed) return { x: 0, y: 0, w: W, h: H };
  return tall ? { x: 40, y: 500, w: 1000, h: 1120 } : { x: 220, y: 160, w: 1480, h: 880 };
};

const ease = Easing.bezier(0.45, 0, 0.25, 1);

const cameraAt = (lf: number, keys: CamKey[], frameOf: (t: number) => number, tall: boolean, card: Box) => {
  const cover = Math.max(1, (card.h / SRC_H) * (SRC_W / card.w));
  const resolve = (k: CamKey) => ({
    cx: tall ? k.tcx ?? k.cx : k.cx,
    cy: tall ? k.tcy ?? k.cy : k.cy,
    z: tall ? k.tz ?? Math.max(k.z * 1.2, cover) : Math.max(k.z, cover),
  });
  let cur = resolve(keys[0]);
  for (const k of keys.slice(1)) {
    const s = frameOf(k.t);
    // Unhurried moves: quick snaps read as jitter.
    const p = interpolate(lf, [s, s + Math.max(24, (k.dur ?? 16) * 1.6)], [0, 1], { ...clamp, easing: ease });
    if (p <= 0) break;
    const n = resolve(k);
    // Interpolate zoom in log space so zooms feel even.
    const z = Math.exp(Math.log(cur.z) + (Math.log(n.z) - Math.log(cur.z)) * p);
    cur = { cx: cur.cx + (n.cx - cur.cx) * p, cy: cur.cy + (n.cy - cur.cy) * p, z };
  }
  const scale = (cur.z * card.w) / SRC_W;
  const vw = SRC_W * scale;
  const vh = SRC_H * scale;
  let tx = card.w / 2 - cur.cx * scale;
  let ty = card.h / 2 - cur.cy * scale;
  tx = vw >= card.w ? Math.min(0, Math.max(card.w - vw, tx)) : (card.w - vw) / 2;
  ty = vh >= card.h ? Math.min(0, Math.max(card.h - vh, ty)) : (card.h - vh) / 2;
  return { scale, tx, ty };
};

export const ScreenCard: React.FC<{
  srcStart: number;
  rate?: number;
  keys: CamKey[];
  callouts?: Callout[];
  clicks?: Click[];
  tags?: Tag[];
  fullBleed?: boolean;
  drift?: number; // extra slow push-in over the clip (montage)
  children?: React.ReactNode;
}> = ({ srcStart, rate = 1, keys, callouts = [], clicks = [], tags = [], fullBleed, drift = 0, children }) => {
  const lf = useCurrentFrame();
  const { fps, durationInFrames } = useVideoConfig();
  const { tall, W, u } = useLayout();
  const card = useCardBox(fullBleed);
  const frameOf = (t: number) => ((t - srcStart) * fps) / rate;

  const cam = cameraAt(lf, keys, frameOf, tall, card);
  const prev = cameraAt(lf - 1, keys, frameOf, tall, card);
  const speed = Math.hypot(cam.tx - prev.tx, cam.ty - prev.ty) + Math.abs(cam.scale - prev.scale) * 300;
  // Blur in screen pixels; the filter sits under the zoom transform, so divide by scale.
  const motionBlur = Math.min(4, speed / 40) / cam.scale;

  const d = 1 + drift * (lf / durationInFrames);
  const map = (x: number, y: number) => ({
    X: card.x + card.w / 2 + (cam.tx + x * cam.scale - card.w / 2) * d,
    Y: card.y + card.h / 2 + (cam.ty + y * cam.scale - card.h / 2) * d,
  });

  const enter = spring({ frame: lf, fps, config: { damping: 18, stiffness: 160 } });

  const active = callouts.filter((c) => lf >= frameOf(c.from) && lf < frameOf(c.to));

  return (
    <AbsoluteFill>
      <div
        style={{
          position: "absolute",
          left: card.x,
          top: card.y,
          width: card.w,
          height: card.h,
          overflow: "hidden",
          borderRadius: fullBleed ? 0 : 28,
          boxShadow: fullBleed ? undefined : `0 40px 120px rgba(0,0,0,0.6), 0 0 0 2px rgba(255,255,255,0.14), 0 0 80px ${COLORS.blue}55`,
          background: "#0b1530",
          transform: fullBleed ? undefined : `perspective(1800px) rotateX(${(1 - enter) * 18}deg) scale(${0.82 + 0.18 * enter})`,
          opacity: fullBleed ? 1 : Math.min(1, enter * 2),
        }}
      >
        <div style={{ position: "absolute", inset: 0, transform: `scale(${d})`, transformOrigin: "50% 50%" }}>
          <OffthreadVideo
            src={staticFile("recording.mp4")}
            startFrom={Math.round(srcStart * fps)}
            playbackRate={rate}
            muted
            style={{
              position: "absolute",
              left: 0,
              top: 0,
              width: SRC_W,
              height: SRC_H,
              maxWidth: "none",
              transformOrigin: "0 0",
              transform: `translate(${cam.tx}px, ${cam.ty}px) scale(${cam.scale})`,
              filter: motionBlur * cam.scale > 0.5 ? `blur(${motionBlur}px)` : undefined,
            }}
          />
        </div>
        {fullBleed && <AbsoluteFill style={{ background: "radial-gradient(ellipse at center, transparent 35%, rgba(3,11,31,0.85) 100%)" }} />}
      </div>

      {active.map((c) => (
        <CalloutView key={c.label} c={c} lf={lf} frameOf={frameOf} map={map} card={card} W={W} u={u} tall={tall} />
      ))}

      {clicks.map((c, i) => {
        const t = lf - frameOf(c.t);
        if (t < 0 || t > 20) return null;
        const { X, Y } = map(c.x, c.y);
        return [0, 5].map((delay) => {
          const tt = t - delay;
          if (tt < 0) return null;
          const r = interpolate(tt, [0, 15], [10, 90], { ...clamp, easing: Easing.out(Easing.cubic) });
          return (
            <div key={`${i}-${delay}`} style={{ position: "absolute", left: X - r, top: Y - r, width: r * 2, height: r * 2, borderRadius: "50%", border: `6px solid ${COLORS.accent}`, opacity: interpolate(tt, [0, 15], [1, 0], clamp), boxShadow: `0 0 30px ${COLORS.accent}` }} />
          );
        });
      })}

      {tags.map((tag, i) => {
        const from = frameOf(tag.t);
        const to = i + 1 < tags.length ? frameOf(tags[i + 1].t) : Infinity;
        if (lf < from || lf >= to) return null;
        return <TagView key={tag.num} tag={tag} lf={lf - from} card={card} tall={tall} W={W} u={u} />;
      })}
      {children}
    </AbsoluteFill>
  );
};

const CalloutView: React.FC<{
  c: Callout;
  lf: number;
  frameOf: (t: number) => number;
  map: (x: number, y: number) => { X: number; Y: number };
  card: Box;
  W: number;
  u: number;
  tall: boolean;
}> = ({ c, lf, frameOf, map, card, W, u, tall }) => {
  const { fps } = useVideoConfig();
  const t = lf - frameOf(c.from);
  const left = frameOf(c.to) - lf;
  const pad = 10;
  const a = map(c.rect[0], c.rect[1]);
  const z = map(c.rect[2], c.rect[3]);
  const x1 = Math.max(card.x + 6, a.X - pad);
  const y1 = Math.max(card.y + 6, a.Y - pad);
  const x2 = Math.min(card.x + card.w - 6, z.X + pad);
  const y2 = Math.min(card.y + card.h - 6, z.Y + pad);
  const w = Math.max(0, x2 - x1);
  const h = Math.max(0, y2 - y1);
  const perim = 2 * (w + h);
  const draw = interpolate(t, [0, 10], [0, 1], { ...clamp, easing: Easing.out(Easing.cubic) });
  const fade = interpolate(left, [0, 5], [0, 1], clamp);
  const pop = spring({ frame: t - 4, fps, config: { damping: 11, stiffness: 200, mass: 0.6 } });

  const fontSize = (tall ? 46 : 40) * (tall ? 1 : u);
  const estW = c.label.length * fontSize * 0.56 + 80;
  const { height: H } = useVideoConfig();
  const pillH = fontSize * 1.9;
  const below = (y1 + y2) / 2 < card.y + card.h * 0.55;
  const cx = Math.min(Math.max((x1 + x2) / 2, 30 + estW / 2), W - 30 - estW / 2);
  const gapY = 34;
  let pillY = below ? y2 + gapY : y1 - gapY;
  // No room outside the box (it fills the card): put the label just inside it.
  if (below && pillY + pillH > H - 20) pillY = y2 - 30 - pillH;
  if (!below && pillY - pillH < 20) pillY = y1 + 30 + pillH;

  return (
    <AbsoluteFill style={{ opacity: fade }}>
      {/* Spotlight: dim the card outside the highlighted element */}
      <svg width="100%" height="100%" style={{ position: "absolute", inset: 0 }}>
        <path
          fillRule="evenodd"
          d={`M${card.x},${card.y}h${card.w}v${card.h}h${-card.w}Z M${x1},${y1}v${h}h${w}v${-h}Z`}
          fill={`rgba(2,8,26,${0.6 * draw})`}
        />
        <rect x={x1} y={y1} width={w} height={h} rx={12} fill="none" stroke={COLORS.accent} strokeWidth={6} strokeDasharray={perim} strokeDashoffset={perim * (1 - draw)} style={{ filter: `drop-shadow(0 0 14px ${COLORS.accent})` }} />
        <line x1={(x1 + x2) / 2} y1={below ? y2 : y1} x2={cx} y2={pillY} stroke={COLORS.accent} strokeWidth={4} opacity={pop} />
      </svg>
      <div
        style={{
          position: "absolute",
          left: cx,
          top: pillY,
          transform: `translate(-50%, ${below ? "0%" : "-100%"}) scale(${pop})`,
          transformOrigin: below ? "50% 0%" : "50% 100%",
          background: "#fff",
          color: COLORS.ink,
          fontFamily: FONT,
          fontWeight: 800,
          fontSize,
          padding: `${fontSize * 0.28}px ${fontSize * 0.7}px`,
          borderRadius: 999,
          whiteSpace: "nowrap",
          boxShadow: `0 16px 40px rgba(0,0,0,0.45), 0 0 0 4px ${COLORS.accent}`,
          display: "flex",
          alignItems: "center",
          gap: fontSize * 0.4,
        }}
      >
        <span style={{ width: fontSize * 0.42, height: fontSize * 0.42, borderRadius: "50%", background: GRADIENT, display: "inline-block" }} />
        {c.label}
      </div>
    </AbsoluteFill>
  );
};

const TagView: React.FC<{ tag: Tag; lf: number; card: Box; tall: boolean; W: number; u: number }> = ({ tag, lf, card, tall, W }) => {
  const { fps } = useVideoConfig();
  const s = spring({ frame: lf, fps, config: { damping: 15, stiffness: 200 } });
  const numSize = tall ? 110 : 64;
  const textSize = tall ? 72 : 60;
  const style: React.CSSProperties = tall
    ? { left: 0, width: W, top: 150, flexDirection: "column", alignItems: "center", gap: 18 }
    : { left: card.x, top: 40, flexDirection: "row", alignItems: "center", gap: 24 };
  return (
    <div style={{ position: "absolute", display: "flex", ...style }}>
      <div style={{ fontFamily: FONT, fontWeight: 900, fontSize: numSize * 0.62, color: "#fff", background: GRADIENT, borderRadius: 18, padding: "4px 18px", boxShadow: `0 0 30px ${COLORS.blue}`, transform: `scale(${s})` }}>{tag.num}</div>
      <div style={{ fontFamily: FONT, fontWeight: 900, fontSize: textSize, color: "#fff", letterSpacing: "-0.02em", textShadow: "0 8px 30px rgba(0,0,0,0.5)", clipPath: `inset(-20% ${100 - s * 100}% -20% 0)`, textAlign: "center" }}>{tag.text}</div>
    </div>
  );
};
