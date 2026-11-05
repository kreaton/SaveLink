import { AbsoluteFill, Sequence } from "remotion";
import { b, useLayout } from "../constants";
import { DesktopCard } from "../components/DesktopCard";
import { Kinetic, Word } from "../components/Kinetic";
import { ScreenCard } from "../components/ScreenCard";

type Cut = { word: string; src?: number; cx?: number; cy?: number; z?: number; desktop?: boolean };

const CUTS: Cut[] = [
  { word: "DROP.", src: 13.6, cx: 360, cy: 110, z: 2.6 },
  { word: "CLICK.", src: 16.6, cx: 364, cy: 411, z: 3.2 },
  { word: "REAL ICONS.", desktop: true },
  { word: "APP MODE.", src: 41.0, cx: 400, cy: 300, z: 1.3 },
];
const BEATS_PER_CUT = 4;

// Beats 124-140, the drop: one shot per bar.
export const Montage: React.FC<{ beat0: number }> = ({ beat0 }) => {
  const { tall } = useLayout();
  return (
    <AbsoluteFill>
      {CUTS.map((c, i) => {
        const from = b(beat0 + i * BEATS_PER_CUT) - b(beat0);
        const dur = b(beat0 + (i + 1) * BEATS_PER_CUT) - b(beat0 + i * BEATS_PER_CUT);
        return (
          <Sequence key={c.word} from={from} durationInFrames={dur}>
            {c.desktop ? (
              <AbsoluteFill style={{ alignItems: "center", justifyContent: "center" }}>
                <DesktopCard size={tall ? 900 : 760} wipeAt={10} />
              </AbsoluteFill>
            ) : (
              <ScreenCard fullBleed drift={0.18} srcStart={c.src!} keys={[{ t: c.src!, cx: c.cx!, cy: c.cy!, z: c.z! }]} />
            )}
            <Kinetic style={{ justifyContent: "flex-end", paddingBottom: c.desktop ? (tall ? 180 : 40) : tall ? 300 : 70 }}>
              <Word at={0} size={c.word.length > 8 ? 210 : 260} style={{ fontStyle: "italic", WebkitTextStroke: "3px rgba(255,255,255,0.9)", textShadow: "0 0 60px rgba(34,102,221,0.9), 0 12px 40px rgba(0,0,0,0.6)" }}>
                {c.word}
              </Word>
            </Kinetic>
          </Sequence>
        );
      })}
    </AbsoluteFill>
  );
};
