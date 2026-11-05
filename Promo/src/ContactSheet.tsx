import { AbsoluteFill, OffthreadVideo, Sequence, staticFile } from "remotion";

// Dev helper: a grid of frames from the recording, to plan the edit.
export const ContactSheet: React.FC<{ start: number; step: number }> = ({ start, step }) => (
  <AbsoluteFill style={{ background: "#000", display: "flex", flexWrap: "wrap", flexDirection: "row" }}>
    {Array.from({ length: 16 }, (_, i) => {
      const t = start + i * step;
      return (
        <div key={i} style={{ width: 480, height: 289, position: "relative", overflow: "hidden" }}>
          <Sequence from={-Math.round(t * 30)} layout="none">
            <OffthreadVideo src={staticFile("recording.mp4")} muted style={{ width: 480 }} />
          </Sequence>
          <div style={{ position: "absolute", top: 4, left: 4, background: "red", color: "#fff", fontSize: 24, padding: "0 6px" }}>
            {t.toFixed(1)}s
          </div>
        </div>
      );
    })}
  </AbsoluteFill>
);
