import { AbsoluteFill, OffthreadVideo, Sequence, staticFile } from "remotion";

// Dev helper: one source frame with a 50px coordinate grid, to place zooms and callouts.
export const GridFrame: React.FC<{ t: number }> = ({ t }) => (
  <AbsoluteFill>
    <Sequence from={-Math.round(t * 30)} layout="none">
      <OffthreadVideo src={staticFile("recording.mp4")} muted />
    </Sequence>
    {Array.from({ length: 24 }, (_, i) => (
      <div key={"v" + i} style={{ position: "absolute", left: i * 50, top: 0, bottom: 0, borderLeft: `1px solid ${i % 2 ? "rgba(255,0,0,.35)" : "red"}`, color: "red", fontSize: 11 }}>{i % 2 ? "" : i * 50}</div>
    ))}
    {Array.from({ length: 15 }, (_, i) => (
      <div key={"h" + i} style={{ position: "absolute", top: i * 50, left: 0, right: 0, borderTop: `1px solid ${i % 2 ? "rgba(255,0,0,.35)" : "red"}`, color: "red", fontSize: 11 }}>{i % 2 ? "" : i * 50}</div>
    ))}
  </AbsoluteFill>
);
