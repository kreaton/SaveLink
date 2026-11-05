import { AbsoluteFill, interpolate, Sequence, useCurrentFrame } from "remotion";
import { b, clamp, useLayout } from "../constants";
import { DesktopCard } from "../components/DesktopCard";
import { KineticScene, rel } from "./Text";

// Beats 0-24: the problem (identical Edge icons), the fix, and the name.
export const Intro: React.FC = () => {
  const { tall } = useLayout();
  const f = useCurrentFrame();
  const side: React.CSSProperties = tall ? { height: "40%", top: 60 } : { width: "56%" };
  const cardSize = tall ? 760 : 700;
  const cardPos: React.CSSProperties = tall ? { left: (1080 - cardSize) / 2, top: 900 } : { left: 1060, top: 190 };
  const cardOut = interpolate(f, [b(20), b(20) + 8], [0, 1], clamp);

  const seg = (from: number, to: number, el: React.ReactNode) => (
    <Sequence from={b(from)} durationInFrames={b(to) - b(from)} layout="none">
      {el}
    </Sequence>
  );

  return (
    <AbsoluteFill>
      {seg(0, 4, <KineticScene beat0={0} lines={[[{ t: "Your", beat: 0, size: 170 }, { t: "desktop", beat: 1, size: 170 }], [{ t: "shortcuts.", beat: 2, size: 190, g: true }]]} />)}
      {seg(4, 8, <KineticScene beat0={4} style={side} lines={[[{ t: "They all", beat: 4, size: 130 }], [{ t: "look the same.", beat: 5, size: 130, g: true }]]} />)}
      {seg(8, 12, <KineticScene beat0={8} style={side} lines={[[{ t: "Which one", beat: 8, size: 130 }], [{ t: "is Gmail?", beat: 9, size: 150, g: true }]]} />)}
      {seg(12, 16, <KineticScene beat0={12} style={side} lines={[[{ t: "Fix it", beat: 12, size: 150 }], [{ t: "in", beat: 13, size: 150 }, { t: "seconds.", beat: 14, size: 150, g: true }]]} />)}
      {seg(16, 20, <KineticScene beat0={16} style={side} lines={[[{ t: "Real icons.", beat: 16, size: 150, g: true }], [{ t: "For every site.", beat: 18, size: 110, v: "rise" }]]} />)}
      {seg(20, 24, <KineticScene beat0={20} lines={[[{ t: "Meet", beat: 20, size: 110, v: "rise" }], [{ t: "SaveLink", beat: 21, size: 260, g: true }], [{ t: "web links → desktop shortcuts", beat: 22, size: 60, v: "rise", color: "#cfe3ff" }]]} />)}

      <Sequence from={b(4)} durationInFrames={b(20) + 10 - b(4)} layout="none">
        <div style={{ position: "absolute", ...cardPos, opacity: 1 - cardOut, transform: `scale(${1 - cardOut * 0.3}) translateY(${cardOut * -80}px)` }}>
          <DesktopCard
            size={cardSize}
            questionsAt={[rel(4, 6), rel(4, 6.5), rel(4, 7), rel(4, 7.5)]}
            shakeAt={rel(4, 10)}
            wipeAt={rel(4, 16)}
          />
        </div>
      </Sequence>
    </AbsoluteFill>
  );
};
